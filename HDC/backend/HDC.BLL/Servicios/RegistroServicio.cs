using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

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
    ColaboracionRepositorio colaboraciones)
{
    /// <summary>RFR05 paginado: la base filtra, ordena y devuelve solo la página pedida.</summary>
    public async Task<PaginaRegistrosDto> ListarAsync(long idUsuario, FiltroRegistros filtro)
    {
        // Los períodos siempre se guardan como el día 1 del mes
        if (filtro.Desde is DateOnly d) filtro.Desde = Mapeos.InicioDeMes(d);
        if (filtro.Hasta is DateOnly h) filtro.Hasta = Mapeos.InicioDeMes(h);

        var (pagina, total, totalKg) = await registros.PaginarVisiblesAsync(idUsuario, filtro);
        var items = pagina.Select(r => new RegistroListaDto(
            r.IdRegistro, r.IdDomicilio, Mapeos.DescripcionCompleta(r.Domicilio), r.Periodo, r.TotalKgCo2,
            r.Detalles.Count, r.IdUsuarioCarga, Mapeos.NombreMostrar(r.UsuarioCarga),
            r.IdUsuarioCarga != r.Domicilio.IdUsuario,
            PuedeEditar(r, idUsuario),
            r.Detalles.Select(d => d.FuenteEmision.TipoEmision.Alcance.Nombre).Distinct().OrderBy(a => a).ToList(),
            r.FechaCreacion, r.FechaUltimaModificacion, r.IdCargaTraslado)).ToList();
        return new PaginaRegistrosDto(items, total, totalKg);
    }

    /// <summary>Opciones del filtro "Domicilio" de Mis emisiones.</summary>
    public async Task<List<OpcionDomicilioDto>> DomiciliosConRegistrosAsync(long idUsuario) =>
        (await registros.DomiciliosVisiblesAsync(idUsuario))
            .Select(d => new OpcionDomicilioDto(d.IdDomicilio, Mapeos.DescripcionCompleta(d))).ToList();

    public async Task<RegistroDetalleDto> ObtenerAsync(long idUsuario, long idRegistro)
    {
        var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
        if (r.Domicilio.IdUsuario != idUsuario && r.IdUsuarioCarga != idUsuario)
            throw new ProhibidoException("No tenés acceso a este registro");
        return ADetalle(r, idUsuario);
    }

    public async Task<RegistroDetalleDto> CrearAsync(long idUsuario, RegistroRequest req)
    {
        var periodo = Mapeos.InicioDeMes(req.Periodo);
        var domicilio = await ValidarAccesoCargaAsync(idUsuario, req.IdDomicilio, periodo, req.Detalles.Select(d => d.IdFuenteEmision));
        var idConvocatoria = await ConvocatoriaDeCargaAsync(idUsuario, domicilio, periodo, req.Detalles.Select(d => d.IdFuenteEmision), req.IdConvocatoria);
        if (req.IdConvocatoria != null && idConvocatoria != req.IdConvocatoria)
            throw new NegocioException("Esa convocatoria no te permite cargar estos consumos en ese período");
        // Quien colabora tiene un solo registro por mes; el titular conserva su forma de cargar
        if (domicilio.IdUsuario != idUsuario) await ValidarMesLibreAsync(idUsuario, domicilio.IdDomicilio, idConvocatoria, periodo);

        var ahora = DateTime.Now;
        var r = new Registro
        {
            IdDomicilio = domicilio.IdDomicilio,
            IdUsuarioCarga = idUsuario,
            IdConvocatoria = idConvocatoria,
            Periodo = periodo,
            Observaciones = Mapeos.Limpiar(req.Observaciones),
            FechaCreacion = ahora,
            FechaUltimaModificacion = ahora
        };
        await AplicarDetallesAsync(r, req.Detalles);
        registros.Agregar(r);
        await registros.GuardarAsync();
        await RecalcularDomicilioAsync(domicilio);

        return ADetalle((await registros.ObtenerCompletoAsync(r.IdRegistro))!, idUsuario);
    }

    public async Task<RegistroDetalleDto> ActualizarAsync(long idUsuario, long idRegistro, RegistroRequest req)
    {
        var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
        if (!PuedeEditar(r, idUsuario)) throw new ProhibidoException("No podés editar este registro");
        if (r.IdCargaTraslado != null) throw new NegocioException("Este registro es parte de una carga de traslado: editá la carga completa");
        if (req.IdDomicilio != r.IdDomicilio) throw new NegocioException("No se puede cambiar el domicilio de un registro");

        var periodo = Mapeos.InicioDeMes(req.Periodo);
        // El titular puede editar registros de sus colaboradores; las restricciones de tipo aplican a quien cargó.
        await ValidarAccesoCargaAsync(r.IdUsuarioCarga, r.IdDomicilio, periodo, req.Detalles.Select(d => d.IdFuenteEmision));

        // Sigue en su convocatoria, salvo que el nuevo mes o las nuevas fuentes ya no correspondan a ella
        var idConvocatoria = await ConvocatoriaDeCargaAsync(r.IdUsuarioCarga, r.Domicilio, periodo, req.Detalles.Select(d => d.IdFuenteEmision), r.IdConvocatoria);
        // Solo si cambia de mes o de colaboración: así se puede corregir un registro aunque ya hubiera dos en ese mes
        if (r.IdUsuarioCarga != r.Domicilio.IdUsuario && (periodo != r.Periodo || idConvocatoria != r.IdConvocatoria))
            await ValidarMesLibreAsync(r.IdUsuarioCarga, r.IdDomicilio, idConvocatoria, periodo, r.IdRegistro);
        r.IdConvocatoria = idConvocatoria;
        r.Periodo = periodo;
        r.Observaciones = Mapeos.Limpiar(req.Observaciones);
        r.FechaUltimaModificacion = DateTime.Now;
        await AplicarDetallesAsync(r, req.Detalles);
        await registros.GuardarAsync();
        await RecalcularDomicilioAsync(r.Domicilio);

        return ADetalle((await registros.ObtenerCompletoAsync(idRegistro))!, idUsuario);
    }

    public async Task EliminarAsync(long idUsuario, long idRegistro)
    {
        var r = await registros.ObtenerCompletoAsync(idRegistro) ?? throw new NoEncontradoException("Registro no encontrado");
        if (!PuedeEditar(r, idUsuario)) throw new ProhibidoException("No podés eliminar este registro");
        if (r.IdCargaTraslado != null) throw new NegocioException("Este registro es parte de una carga de traslado: eliminá la carga completa");
        var domicilio = r.Domicilio;
        registros.Eliminar(r);
        await registros.GuardarAsync();
        await RecalcularDomicilioAsync(domicilio);
    }

    private async Task ValidarMesLibreAsync(long idUsuario, long idDomicilio, long? idConvocatoria, DateOnly periodo, long excluirId = 0)
    {
        if (await registros.ExisteConsumoDelMesAsync(idUsuario, idDomicilio, idConvocatoria, periodo, excluirId))
            throw new ConflictoException("Ya guardaste un registro de ese mes en esta colaboración. Para agregar o cambiar consumos, editalo desde Mis emisiones.");
    }

    private static bool PuedeEditar(Registro r, long idUsuario) =>
        r.Domicilio.IdUsuario == idUsuario || r.IdUsuarioCarga == idUsuario;

    /// <summary>Valida el período y el acceso a cargar esas fuentes. También la usa TrasladoServicio para cada mes.</summary>
    internal async Task<Domicilio> ValidarAccesoCargaAsync(long idUsuarioCarga, long idDomicilio, DateOnly periodo, IEnumerable<long> idsFuentes)
    {
        if (periodo > Mapeos.MesActual()) throw new NegocioException("No se pueden cargar consumos de períodos futuros");

        var domicilio = await domicilios.ObtenerCompletoAsync(idDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        if (domicilio.FechaBaja != null) throw new NegocioException("El domicilio está dado de baja");

        if (domicilio.IdUsuario == idUsuarioCarga) return domicilio;

        var vigentes = await colaboraciones.ObtenerVigentesAsync(idUsuarioCarga, idDomicilio, periodo);
        if (vigentes.Count == 0)
            throw new ProhibidoException("No tenés una colaboración aceptada en este domicilio para ese período");

        // Tipos habilitados de cada colaboración vigente (los de su convocatoria, si nació de una)
        var tiposPermitidos = vigentes.SelectMany(c => c.IdsTiposHabilitados()).ToHashSet();
        var usadas = await fuentes.ObtenerPorIdsAsync(idsFuentes.Distinct());
        var noPermitida = usadas.FirstOrDefault(f => !tiposPermitidos.Contains(f.IdTipoEmision));
        if (noPermitida != null)
            throw new ProhibidoException($"La colaboración no habilita cargar \"{noPermitida.TipoEmision.Nombre}\" ({noPermitida.Nombre})");

        return domicilio;
    }

    /// <summary>
    /// Convocatoria a la que corresponde un aporte: la preferida, si cubre ese mes y esos tipos; si no, la más reciente
    /// que los cubra. Null para el titular y para lo que solo cubre una invitación por email. También la usa TrasladoServicio.
    /// </summary>
    internal async Task<long?> ConvocatoriaDeCargaAsync(long idUsuarioCarga, Domicilio domicilio, DateOnly periodo, IEnumerable<long> idsFuentes, long? preferida)
    {
        if (domicilio.IdUsuario == idUsuarioCarga) return null;
        var tiposUsados = (await fuentes.ObtenerPorIdsAsync(idsFuentes.Distinct())).Select(f => f.IdTipoEmision).ToList();
        var candidatas = (await colaboraciones.ObtenerVigentesAsync(idUsuarioCarga, domicilio.IdDomicilio, periodo))
            .Where(c => c.IdConvocatoria != null && tiposUsados.All(t => c.IdsTiposHabilitados().Contains(t)))
            .OrderByDescending(c => c.FechaInvitacion)
            .Select(c => c.IdConvocatoria!.Value)
            .ToList();
        if (preferida is long id && candidatas.Contains(id)) return id;
        return candidatas.Count > 0 ? candidatas[0] : null;
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

        foreach (var req in detalles)
        {
            var fuente = mapa[req.IdFuenteEmision];
            if (existentes.TryGetValue(req.IdFuenteEmision, out var det))
            {
                // Se conserva el factor histórico con el que se cargó originalmente
                det.Consumo = req.Consumo;
                det.KgCo2 = Math.Round(req.Consumo * det.ValorFactorEmision, 6);
                AplicarDatosAdicionales(det, req, fuente, r.Periodo);
            }
            else
            {
                if (fuente.FechaBaja != null) throw new NegocioException($"La fuente \"{fuente.Nombre}\" fue dada de baja");
                // Los traslados necesitan distancia, días y período: se cargan con su propio formulario
                if (fuente.TipoEmision.ModoCarga == ModoCarga.Traslado)
                    throw new NegocioException($"\"{fuente.Nombre}\" se carga desde el formulario de traslados");
                var nuevo = new DetalleRegistro
                {
                    IdFuenteEmision = fuente.IdFuenteEmision,
                    Consumo = req.Consumo,
                    ValorFactorEmision = fuente.ValorFactorEmision,
                    KgCo2 = Math.Round(req.Consumo * fuente.ValorFactorEmision, 6)
                };
                AplicarDatosAdicionales(nuevo, req, fuente, r.Periodo);
                r.Detalles.Add(nuevo);
            }
        }
        r.TotalKgCo2 = r.Detalles.Sum(d => d.KgCo2);
    }

    /// <summary>Datos que dependen del tipo de la fuente: km opcionales en vehículos y fecha en viajes institucionales.</summary>
    private static void AplicarDatosAdicionales(DetalleRegistro d, DetalleRegistroRequest req, FuenteEmision fuente, DateOnly periodo)
    {
        var modo = fuente.TipoEmision.ModoCarga;
        d.Kilometros = modo == ModoCarga.Vehiculo ? req.Kilometros : null;
        d.FechaViaje = null;
        if (modo != ModoCarga.Viaje) return;

        var fecha = req.FechaViaje ?? throw new NegocioException($"Indicá la fecha de \"{fuente.Nombre}\"");
        if (fecha.Year != periodo.Year || fecha.Month != periodo.Month)
            throw new NegocioException($"La fecha de \"{fuente.Nombre}\" debe estar dentro del período del registro");
        if (fecha > DateOnly.FromDateTime(DateTime.Now))
            throw new NegocioException("La fecha del viaje no puede ser futura");
        d.FechaViaje = fecha;
    }

    internal async Task RecalcularDomicilioAsync(Domicilio domicilio)
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
            d.FuenteEmision.UnidadMedida.Nombre, d.Consumo, d.ValorFactorEmision, d.KgCo2,
            d.FuenteEmision.TipoEmision.ModoCarga, d.Kilometros, d.FechaViaje)).ToList(),
        r.CargaTraslado is { } c
            ? new TrasladoResumenDto(c.IdCargaTraslado, c.Periodicidad, c.PeriodoDesde, c.PeriodoHasta, c.MesesPeriodo, c.Semanas, c.CantidadPersonas,
                c.ConvocatoriaRol?.Nombre, c.Convocatoria?.Nombre,
                c.Medios.OrderBy(m => m.FuenteEmision.Nombre).Select(m => new MedioResumenDto(m.FuenteEmision.Nombre, m.DistanciaKm, m.DiasSemana)).ToList())
            : null);
}
