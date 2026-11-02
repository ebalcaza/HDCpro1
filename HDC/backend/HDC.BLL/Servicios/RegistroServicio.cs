using HDC.BLL.Comun;
using HDC.DAL;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;


namespace HDC.BLL.Servicios;

/// <summary>
/// Registros de emisiones. Los puede cargar el titular del domicilio, o un colaborador con una
/// colaboración aceptada que cubra el período y solo para los tipos de emisión habilitados.
/// kg CO2 = consumo × factor de emisión (el factor se congela en el detalle al momento de la carga).
/// </summary>
public class RegistroServicio(
    RegistroRepositorio registros,
    DomicilioRepositorio domicilios,
    FuenteEmisionRepositorio fuentes,
    ColaboracionRepositorio colaboraciones,
     HdcDbContext context)
{
    public async Task<List<RegistroListaDto>> ListarAsync(long idUsuario, long? idDomicilio)
    {
        var lista = await registros.ListarVisiblesAsync(idUsuario, idDomicilio);
        return lista.Select(r => new RegistroListaDto(
            r.IdRegistro, r.IdDomicilio, Mapeos.DescripcionCompleta(r.Domicilio), r.Periodo, r.TotalKgCo2,
            r.Detalles.Count, r.IdUsuarioCarga, Mapeos.NombreMostrar(r.UsuarioCarga),
            r.IdUsuarioCarga != r.Domicilio.IdUsuario,
            PuedeEditar(r, idUsuario),
            r.Detalles.Select(d => d.FuenteEmision.TipoEmision.Alcance.Nombre).Distinct().OrderBy(a => a).ToList(),
            r.FechaCreacion, r.FechaUltimaModificacion)).ToList();
    }

    public async Task<RegistroDetalleDto> ObtenerAsync(long idUsuario, long idRegistro)
    {
        var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
        if (r.Domicilio.IdUsuario != idUsuario && r.IdUsuarioCarga != idUsuario)
            throw new ProhibidoException("No tenés acceso a este registro");
        return ADetalle(r, idUsuario);
    }


public async Task<RegistroDetalleDto> CrearAsync(long idUsuario, RegistroRequest req)
{
    var strategy = context.Database.CreateExecutionStrategy();

    // IMPORTANTE: Asegúrate de que tenga 'async () =>'
    return await strategy.ExecuteAsync(async () =>
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var periodo = Mapeos.InicioDeMes(req.Periodo);
            var domicilio = await ValidarAccesoCargaAsync(idUsuario, req.IdDomicilio, periodo, req.Detalles);

            bool existeRegistro = await registros.ExisteParaDomicilioYPeriodoAsync(domicilio.IdDomicilio, periodo);
            if (existeRegistro)
            {
                throw new NegocioException("Ya existe un registro cargado para este domicilio en el período seleccionado.");
            }

            var ahora = DateTime.Now;
            var r = new Registro
            {
                IdDomicilio = domicilio.IdDomicilio,
                IdUsuarioCarga = idUsuario,
                Periodo = periodo,
                Observaciones = Mapeos.Limpiar(req.Observaciones),
                FechaCreacion = ahora,
                FechaUltimaModificacion = ahora
            };
            
            await AplicarDetallesAsync(r, req.Detalles);
            registros.Agregar(r);
            
            await registros.GuardarAsync();
            await RecalcularDomicilioAsync(domicilio);

            await transaction.CommitAsync();

            return ADetalle((await registros.ObtenerCompletoAsync(r.IdRegistro))!, idUsuario);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    });
}
public async Task<RegistroDetalleDto> ActualizarAsync(long idUsuario, long idRegistro, RegistroRequest req)
{
    var strategy = context.Database.CreateExecutionStrategy();

    // IMPORTANTE: Asegúrate de que tenga 'async () =>'
    return await strategy.ExecuteAsync(async () =>
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
            if (!PuedeEditar(r, idUsuario)) throw new ProhibidoException("No podés editar este registro");
            if (req.IdDomicilio != r.IdDomicilio) throw new NegocioException("No se puede cambiar el domicilio de un registro");

            var periodo = Mapeos.InicioDeMes(req.Periodo);
            await ValidarAccesoCargaAsync(idUsuario, r.IdDomicilio, periodo, req.Detalles);

            var domicilio = r.Domicilio; // Aseguramos tener la referencia al domicilio

            r.Periodo = periodo;
            r.Observaciones = Mapeos.Limpiar(req.Observaciones);
            r.FechaUltimaModificacion = DateTime.Now;
            await AplicarDetallesAsync(r, req.Detalles);
            
            await registros.GuardarAsync();
            await RecalcularDomicilioAsync(domicilio);

            await transaction.CommitAsync();

            return ADetalle((await registros.ObtenerCompletoAsync(idRegistro))!, idUsuario);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    });
}

public async Task EliminarAsync(long idUsuario, long idRegistro)
{
    var strategy = context.Database.CreateExecutionStrategy();

    // IMPORTANTE: Asegúrate de que tenga 'async () =>'
    await strategy.ExecuteAsync(async () =>
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
            if (!PuedeEditar(r, idUsuario)) throw new ProhibidoException("No podés eliminar este registro");
            
            var domicilio = r.Domicilio;
            
            registros.Eliminar(r);
            await registros.GuardarAsync();
            await RecalcularDomicilioAsync(domicilio);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    });
}
    private static bool PuedeEditar(Registro r, long idUsuario) =>
        r.Domicilio.IdUsuario == idUsuario || r.IdUsuarioCarga == idUsuario;

private async Task<Domicilio> ValidarAccesoCargaAsync(long idUsuarioAccion, long idDomicilio, DateOnly periodo, List<DetalleRegistroRequest> detalles)
{
    if (periodo > Mapeos.MesActual()) throw new NegocioException("No se pueden cargar consumos de períodos futuros");

    var domicilio = await domicilios.ObtenerCompletoAsync(idDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
    if (domicilio.FechaBaja != null) throw new NegocioException("El domicilio está dado de baja");

    // 1. SI EL USUARIO ACTUAL ES EL TITULAR, TIENE ACCESO TOTAL
    // (Esto soluciona que el titular pueda editar registros de ex-colaboradores)
    if (domicilio.IdUsuario == idUsuarioAccion) return domicilio;

    // 2. Si no es el titular, validamos que tenga una colaboración vigente para ese período
    var vigentes = await colaboraciones.ObtenerVigentesAsync(idUsuarioAccion, idDomicilio, periodo);
    if (vigentes.Count == 0)
        throw new ProhibidoException("No tenés una colaboración aceptada en este domicilio para ese período");

    var tiposPermitidos = vigentes.SelectMany(c => c.TiposEmision.Select(t => t.IdTipoEmision)).ToHashSet();
    var usadas = await fuentes.ObtenerPorIdsAsync(detalles.Select(d => d.IdFuenteEmision).Distinct());
    var noPermitida = usadas.FirstOrDefault(f => !tiposPermitidos.Contains(f.IdTipoEmision));
    if (noPermitida != null)
        throw new ProhibidoException($"La colaboración no habilita cargar \"{noPermitida.TipoEmision.Nombre}\" ({noPermitida.Nombre})");

    return domicilio;
}

private async Task AplicarDetallesAsync(Registro r, List<DetalleRegistroRequest> detalles)
{
    var repetida = detalles.GroupBy(d => d.IdFuenteEmision).FirstOrDefault(g => g.Count() > 1);
    if (repetida != null) throw new NegocioException("Hay fuentes de emisión repetidas; sumá los consumos en una sola fila");

    var ids = detalles.Select(d => d.IdFuenteEmision).ToList();
    var mapa = (await fuentes.ObtenerPorIdsAsync(ids)).ToDictionary(f => f.IdFuenteEmision);
    if (mapa.Count != ids.Count) throw new NegocioException("Alguna de las fuentes de emisión no existe");

    var existentes = r.Detalles.ToDictionary(d => d.IdFuenteEmision);
    var quitar = r.Detalles.Where(d => !ids.Contains(d.IdFuenteEmision)).ToList();
    registros.QuitarDetalles(quitar);
    foreach (var q in quitar) r.Detalles.Remove(q);

    // Límite seguro para columnas DECIMAL(18,6) (12 dígitos enteros)
    const decimal MAX_VALOR_PERMITIDO = 999999999999.99m;

    foreach (var req in detalles)
    {
        var fuente = mapa[req.IdFuenteEmision];
        
        // Determinamos el factor de emisión a utilizar
        var factorEmision = existentes.TryGetValue(req.IdFuenteEmision, out var detExistente) 
            ? detExistente.ValorFactorEmision 
            : fuente.ValorFactorEmision;

        var kgCo2Calculado = Math.Round(req.Consumo * factorEmision, 6);

        // --- VALIDACIÓN DE AUDITORÍA PARA EVITAR DESBORDAMIENTO ---
        if (kgCo2Calculado > MAX_VALOR_PERMITIDO)
        {
            throw new NegocioException($"El valor calculado de CO2 para la fuente \"{fuente.Nombre}\" supera el límite máximo permitido por el sistema.");
        }
        // ---------------------------------------------------------

        if (existentes.TryGetValue(req.IdFuenteEmision, out var det))
        {
            // Se conserva el factor histórico con el que se cargó originalmente
            det.Consumo = req.Consumo;
            det.KgCo2 = kgCo2Calculado;
        }
        else
        {
            if (fuente.FechaBaja != null) throw new NegocioException($"La fuente \"{fuente.Nombre}\" fue dada de baja");
            r.Detalles.Add(new DetalleRegistro
            {
                IdFuenteEmision = fuente.IdFuenteEmision,
                Consumo = req.Consumo,
                ValorFactorEmision = fuente.ValorFactorEmision,
                KgCo2 = kgCo2Calculado
            });
        }
    }

    r.TotalKgCo2 = r.Detalles.Sum(d => d.KgCo2);

    // Opcional: Validar también que la suma total del registro no desborde la columna general
    if (r.TotalKgCo2 > MAX_VALOR_PERMITIDO)
    {
        throw new NegocioException("El total acumulado de emisiones del registro supera la capacidad máxima permitida.");
    }
}
    private async Task RecalcularDomicilioAsync(Domicilio domicilio)
    {
        var tracked = await domicilios.ObtenerPorIdAsync(domicilio.IdDomicilio);
        if (tracked is null) return;
        tracked.TotalKgCo2 = await registros.SumarPorDomicilioAsync(domicilio.IdDomicilio);
        await domicilios.GuardarAsync();
    }

    private static RegistroDetalleDto ADetalle(Registro r, long idUsuario) => new(
        r.IdRegistro, r.IdDomicilio, Mapeos.DescripcionCorta(r.Domicilio),
        r.Domicilio.Ciudad.Nombre, r.Domicilio.Ciudad.Distrito.Provincia.Nombre,
        Mapeos.NombreMostrar(r.Domicilio.Usuario),
        r.Periodo, r.Observaciones, r.TotalKgCo2,
        r.IdUsuarioCarga, Mapeos.NombreMostrar(r.UsuarioCarga),
        r.IdUsuarioCarga != r.Domicilio.IdUsuario,
        PuedeEditar(r, idUsuario),
        r.FechaCreacion, r.FechaUltimaModificacion,
        r.Detalles.OrderByDescending(d => d.KgCo2).Select(d => new DetalleRegistroDto(
            d.IdDetalle, d.IdFuenteEmision, d.FuenteEmision.Nombre,
            d.FuenteEmision.IdTipoEmision, d.FuenteEmision.TipoEmision.Nombre,
            d.FuenteEmision.TipoEmision.IdAlcance, d.FuenteEmision.TipoEmision.Alcance.Nombre,
            d.FuenteEmision.UnidadMedida.Nombre, d.Consumo, d.ValorFactorEmision, d.KgCo2)).ToList());
}
