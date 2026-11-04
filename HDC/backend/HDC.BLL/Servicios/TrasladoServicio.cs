using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

/// <summary>
/// Traslados (v2.6, ampliado en v2.8): cómo viaja cada persona hasta el domicilio del titular.
///
/// Una carga tiene uno o más medios de transporte, cada uno con su distancia de ida y vuelta y sus días por semana.
/// Un medio aparece una sola vez: quien toma 3 colectivos declara el total de km de los tres en una fila (v2.12):
///     km del mes = distancia × días por semana × (semanas ÷ meses del período) × personas
///     kg CO2e del mes = km del mes × factor del medio
///
/// Quién define el período:
///   - En una CONVOCATORIA lo define el organizador (período y semanas). Quien colabora solo elige cargar
///     todo el período de una vez o un mes por vez.
///   - El titular de un domicilio y quien fue invitado por email eligen mensual, semestral o anual.
///
/// Se guarda un registro por cada mes, con un detalle por medio, para que los gráficos y filtros mensuales muestren
/// una evolución pareja. Nunca se generan meses futuros: los meses que todavía no empezaron no se cargan.
/// Los registros de una carga se editan y eliminan juntos, desde este servicio.
/// </summary>
public class TrasladoServicio(
    CargaTrasladoRepositorio cargas,
    RegistroRepositorio registros,
    FuenteEmisionRepositorio fuentes,
    RegistroServicio registroServicio)
{
    /// <summary>Lo que necesita el formulario según el domicilio: convocatorias, roles y meses que se pueden cargar.</summary>
    public async Task<OpcionesTrasladoDto> OpcionesAsync(long idUsuario, long idDomicilio)
    {
        var titular = await cargas.TitularDelDomicilioAsync(idDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        var esTitular = titular == idUsuario;
        var roles = await cargas.RolesDisponiblesAsync(idDomicilio, esTitular ? null : idUsuario);
        var mesActual = Mapeos.MesActual();

        if (esTitular)
            return new OpcionesTrasladoDto(true, roles, null, mesActual, [], []);

        var colabs = await cargas.ColaboracionesAceptadasAsync(idUsuario, idDomicilio);
        if (colabs.Count == 0) throw new ProhibidoException("No tenés una colaboración aceptada en este domicilio");

        var convocatorias = new List<ConvocatoriaTrasladoDto>();
        foreach (var c in await cargas.ConvocatoriasDisponiblesAsync(idDomicilio, idUsuario))
        {
            convocatorias.Add(new ConvocatoriaTrasladoDto(
                c.IdConvocatoria, c.Nombre, c.Descripcion, c.PeriodoDesde, c.PeriodoHasta!.Value, c.SemanasTraslado!.Value,
                Periodicidad.MesesEntre(c.PeriodoDesde, c.PeriodoHasta.Value),
                c.Roles.OrderBy(r => r.Nombre).Select(r => new RolOpcionDto(r.IdConvocatoriaRol, r.Nombre, c.Nombre)).ToList(),
                await cargas.MesesCargadosAsync(idUsuario, c.IdConvocatoria),
                colabs.FirstOrDefault(x => x.IdConvocatoria == c.IdConvocatoria)?.IdConvocatoriaRol));
        }

        // Sin fecha de fin en alguna colaboración, el límite es solo el mes en curso
        var hasta = colabs.Any(c => c.PeriodoHasta == null) ? mesActual : colabs.Max(c => c.PeriodoHasta!.Value);
        return new OpcionesTrasladoDto(false, roles, colabs.Min(c => c.PeriodoDesde), hasta > mesActual ? mesActual : hasta, convocatorias,
            await cargas.CargasPropiasAsync(idUsuario, idDomicilio));
    }

    public async Task<TrasladoDto> ObtenerAsync(long idUsuario, long id)
    {
        var c = await ObtenerConAccesoAsync(idUsuario, id);
        return ADto(c, idUsuario);
    }

    public async Task<TrasladoDto> CrearAsync(long idUsuario, TrasladoRequest req)
    {
        var c = new CargaTraslado { IdDomicilio = req.IdDomicilio, IdUsuarioCarga = idUsuario, FechaCreacion = DateTime.Now };
        var domicilio = await AplicarAsync(c, req);
        cargas.Agregar(c);
        await cargas.GuardarAsync();
        await registroServicio.RecalcularDomicilioAsync(domicilio);
        return await ObtenerAsync(idUsuario, c.IdCargaTraslado);
    }

    /// <summary>Editar reemplaza todos los registros de la carga. Los medios que siguen conservan su factor original.</summary>
    public async Task<TrasladoDto> ActualizarAsync(long idUsuario, long id, TrasladoRequest req)
    {
        var c = await ObtenerConAccesoAsync(idUsuario, id);
        if (req.IdDomicilio != c.IdDomicilio) throw new NegocioException("No se puede cambiar el domicilio de una carga");
        registros.EliminarRango(c.Registros.ToList());
        var domicilio = await AplicarAsync(c, req);
        await cargas.GuardarAsync();
        await registroServicio.RecalcularDomicilioAsync(domicilio);
        return await ObtenerAsync(idUsuario, id);
    }

    public async Task EliminarAsync(long idUsuario, long id)
    {
        var c = await ObtenerConAccesoAsync(idUsuario, id);
        var domicilio = c.Domicilio;
        registros.EliminarRango(c.Registros.ToList());
        cargas.Eliminar(c);
        await cargas.GuardarAsync();
        await registroServicio.RecalcularDomicilioAsync(domicilio);
    }

    private async Task<CargaTraslado> ObtenerConAccesoAsync(long idUsuario, long id)
    {
        var c = await cargas.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Carga de traslado no encontrada");
        // Igual que en un registro: la gestionan quien la cargó y el titular del domicilio
        if (c.IdUsuarioCarga != idUsuario && c.Domicilio.IdUsuario != idUsuario)
            throw new ProhibidoException("No tenés acceso a esta carga");
        return c;
    }

    /// <summary>Período ya resuelto: quién lo definió no importa para calcular.</summary>
    private record PeriodoCarga(string Periodicidad, DateOnly Desde, List<DateOnly> Meses, int MesesPeriodo, decimal Semanas);

    /// <summary>Valida el pedido, copia los datos a la carga y genera un registro por cada mes.</summary>
    private async Task<Domicilio> AplicarAsync(CargaTraslado c, TrasladoRequest req)
    {
        var titular = await cargas.TitularDelDomicilioAsync(c.IdDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        var esColaborador = titular != c.IdUsuarioCarga;

        var convocatoria = await ResolverConvocatoriaAsync(c, req, esColaborador);
        var periodo = convocatoria != null ? PeriodoDeConvocatoria(convocatoria, req) : PeriodoElegido(req);

        var medios = await ValidarMediosAsync(c, req);
        var idsFuentes = medios.Select(m => m.Fuente.IdFuenteEmision).ToList();

        // Cada mes pasa por la misma validación de acceso que un registro común
        Domicilio domicilio = null!;
        foreach (var mes in periodo.Meses)
            domicilio = await registroServicio.ValidarAccesoCargaAsync(c.IdUsuarioCarga, c.IdDomicilio, mes, idsFuentes);

        var hasta = periodo.Meses[^1];
        // Quien colabora no puede volver a grabar un mes ya cargado: solo editar esa carga (v2.11).
        // Una edición que no suma meses queda exceptuada, para no trabar a quien ya tenía dos cargas con distinto medio en el mismo mes.
        var sumaMeses = c.IdCargaTraslado == 0 || periodo.Desde < c.PeriodoDesde || hasta > c.PeriodoHasta;
        if (esColaborador && sumaMeses
            && await cargas.PeriodoCargadoAsync(c.IdUsuarioCarga, c.IdDomicilio, convocatoria?.IdConvocatoria, periodo.Desde, hasta, c.IdCargaTraslado))
            throw new ConflictoException("Ya cargaste tu traslado para alguno de esos meses. Para cambiarlo, editá esa carga desde Mis emisiones.");

        // El titular puede declarar varios grupos en un mismo mes con medios distintos, pero no repetir un medio
        var repetido = await cargas.MedioSolapadoAsync(c.IdUsuarioCarga, c.IdDomicilio, convocatoria?.IdConvocatoria, idsFuentes, periodo.Desde, hasta, c.IdCargaTraslado);
        if (repetido != null)
            throw new ConflictoException($"Ya cargaste \"{repetido}\" para alguno de esos meses. Editá esa carga o elegí otro período.");

        await ValidarRolAsync(c, req, esColaborador, convocatoria);
        // El rol usado queda como el de la persona en la convocatoria, para no volver a pedírselo
        if (convocatoria != null && req.IdConvocatoriaRol is long rolUsado)
            await cargas.RecordarRolAsync(c.IdUsuarioCarga, convocatoria.IdConvocatoria, rolUsado);

        // Quien colabora carga solo su propio viaje; el titular puede declarar un grupo (ej.: datos de una encuesta)
        var personas = esColaborador ? 1 : req.CantidadPersonas;

        c.IdConvocatoria = convocatoria?.IdConvocatoria;
        c.IdConvocatoriaRol = req.IdConvocatoriaRol;
        c.Periodicidad = periodo.Periodicidad;
        c.PeriodoDesde = periodo.Desde;
        c.PeriodoHasta = hasta;
        c.MesesPeriodo = periodo.MesesPeriodo;
        c.Semanas = periodo.Semanas;
        c.CantidadPersonas = personas;
        AplicarMedios(c, medios);

        // Cada medio aporta sus km al mismo registro mensual
        var detallesMes = medios.Select(m =>
        {
            var km = Math.Round(m.Pedido.DistanciaKm * m.Pedido.DiasSemana * (periodo.Semanas / periodo.MesesPeriodo) * personas, 4);
            if (km <= 0) throw new NegocioException("Los datos ingresados no alcanzan para calcular la distancia del mes");
            return (m.Fuente.IdFuenteEmision, Km: km, m.Factor, Kg: Math.Round(km * m.Factor, 6));
        }).ToList();

        var ahora = DateTime.Now;
        foreach (var mes in periodo.Meses)
        {
            // Cada mes queda como aporte de su convocatoria. El formulario libre la deduce del mes y de los medios
            var idConvocatoria = convocatoria?.IdConvocatoria
                ?? (esColaborador ? await registroServicio.ConvocatoriaDeCargaAsync(c.IdUsuarioCarga, domicilio, mes, idsFuentes, null) : null);
            var r = new Registro
            {
                IdDomicilio = c.IdDomicilio,
                IdUsuarioCarga = c.IdUsuarioCarga,
                IdConvocatoria = idConvocatoria,
                Periodo = mes,
                TotalKgCo2 = detallesMes.Sum(d => d.Kg),
                FechaCreacion = c.FechaCreacion,
                FechaUltimaModificacion = ahora
            };
            foreach (var d in detallesMes)
                r.Detalles.Add(new DetalleRegistro { IdFuenteEmision = d.IdFuenteEmision, Consumo = d.Km, ValorFactorEmision = d.Factor, KgCo2 = d.Kg });
            c.Registros.Add(r);
        }
        return domicilio;
    }

    /// <summary>
    /// Quien colabora en una convocatoria configurada debe cargar dentro de ella. El titular y quien fue invitado por
    /// email no eligen convocatoria y definen su propio período.
    /// </summary>
    private async Task<Convocatoria?> ResolverConvocatoriaAsync(CargaTraslado c, TrasladoRequest req, bool esColaborador)
    {
        if (!esColaborador) return null;
        var disponibles = await cargas.ConvocatoriasDisponiblesAsync(c.IdDomicilio, c.IdUsuarioCarga);
        if (disponibles.Count == 0) return null;
        if (req.IdConvocatoria is not long id) throw new NegocioException("Elegí la convocatoria en la que cargás");
        return disponibles.FirstOrDefault(x => x.IdConvocatoria == id) ?? throw new NegocioException("No participás en esa convocatoria");
    }

    /// <summary>La convocatoria fija el período y las semanas: solo se elige si es todo el período o un mes.</summary>
    private static PeriodoCarga PeriodoDeConvocatoria(Convocatoria conv, TrasladoRequest req)
    {
        var desde = conv.PeriodoDesde;
        var fin = conv.PeriodoHasta!.Value;
        var mesesPeriodo = Periodicidad.MesesEntre(desde, fin);
        var mesActual = Mapeos.MesActual();
        if (desde > mesActual) throw new NegocioException("El período de la convocatoria todavía no empezó");

        if (req.Periodicidad == Periodicidad.PeriodoCompleto)
        {
            var meses = Enumerable.Range(0, mesesPeriodo).Select(desde.AddMonths).Where(m => m <= mesActual).ToList();
            return new PeriodoCarga(Periodicidad.PeriodoCompleto, desde, meses, mesesPeriodo, conv.SemanasTraslado!.Value);
        }
        if (req.Periodicidad != Periodicidad.Mensual) throw new NegocioException("Elegí cargar todo el período o un mes");

        var mes = Mapeos.InicioDeMes(req.PeriodoDesde);
        if (mes < desde || mes > fin) throw new NegocioException("El mes elegido está fuera del período de la convocatoria");
        if (mes > mesActual) throw new NegocioException("No se pueden cargar traslados de períodos futuros");
        return new PeriodoCarga(Periodicidad.Mensual, mes, [mes], mesesPeriodo, conv.SemanasTraslado!.Value);
    }

    /// <summary>Titular o invitación por email: cada uno declara el período y las semanas.</summary>
    private static PeriodoCarga PeriodoElegido(TrasladoRequest req)
    {
        if (req.Periodicidad == Periodicidad.PeriodoCompleto) throw new NegocioException("Elegí un período mensual, semestral o anual");
        var desde = Mapeos.InicioDeMes(req.PeriodoDesde);
        var mesActual = Mapeos.MesActual();
        if (desde > mesActual) throw new NegocioException("No se pueden cargar traslados de períodos futuros");

        if (req.Semanas > Periodicidad.SemanasMaximas(req.Periodicidad))
            throw new NegocioException($"Un período {req.Periodicidad.ToLowerInvariant()} tiene como máximo {Periodicidad.SemanasMaximas(req.Periodicidad)} semanas");

        var mesesPeriodo = Periodicidad.Meses(req.Periodicidad);
        var meses = Enumerable.Range(0, mesesPeriodo).Select(desde.AddMonths).Where(m => m <= mesActual).ToList();
        return new PeriodoCarga(req.Periodicidad, desde, meses, mesesPeriodo, req.Semanas);
    }

    private record MedioValidado(MedioTrasladoRequest Pedido, FuenteEmision Fuente, decimal Factor);

    private async Task<List<MedioValidado>> ValidarMediosAsync(CargaTraslado c, TrasladoRequest req)
    {
        if (req.Medios.GroupBy(m => m.IdFuenteEmision).Any(g => g.Count() > 1))
            throw new NegocioException("Hay medios de transporte repetidos: sumá los km de ese medio en una sola fila");

        var encontrados = (await fuentes.ObtenerPorIdsAsync(req.Medios.Select(m => m.IdFuenteEmision))).ToDictionary(f => f.IdFuenteEmision);
        var lista = new List<MedioValidado>();
        foreach (var m in req.Medios)
        {
            if (!encontrados.TryGetValue(m.IdFuenteEmision, out var fuente)) throw new NegocioException("Alguno de los medios de transporte no existe");
            if (fuente.TipoEmision.ModoCarga != ModoCarga.Traslado) throw new NegocioException($"\"{fuente.Nombre}\" no es un medio de transporte");

            // Un medio que ya estaba en la carga conserva su factor, aunque después haya cambiado o se haya dado de baja
            var existente = c.Medios.FirstOrDefault(x => x.IdFuenteEmision == fuente.IdFuenteEmision);
            if (existente is null && fuente.FechaBaja != null) throw new NegocioException($"El medio \"{fuente.Nombre}\" fue dado de baja");
            lista.Add(new MedioValidado(m, fuente, existente?.ValorFactorEmision ?? fuente.ValorFactorEmision));
        }
        return lista;
    }

    /// <summary>Actualiza en el lugar los medios que siguen: así no choca con la restricción de medio único por carga.</summary>
    private void AplicarMedios(CargaTraslado c, List<MedioValidado> medios)
    {
        foreach (var quitado in c.Medios.Where(x => medios.All(m => m.Fuente.IdFuenteEmision != x.IdFuenteEmision)).ToList())
        {
            cargas.QuitarMedio(quitado);
            c.Medios.Remove(quitado);
        }
        foreach (var m in medios)
        {
            var existente = c.Medios.FirstOrDefault(x => x.IdFuenteEmision == m.Fuente.IdFuenteEmision);
            if (existente is null)
            {
                existente = new CargaTrasladoMedio { IdFuenteEmision = m.Fuente.IdFuenteEmision, FuenteEmision = m.Fuente };
                c.Medios.Add(existente);
            }
            existente.DistanciaKm = m.Pedido.DistanciaKm;
            existente.DiasSemana = m.Pedido.DiasSemana;
            existente.ValorFactorEmision = m.Factor;
        }
    }

    /// <summary>El rol debe estar disponible para quien cargó. Si hay roles, el colaborador tiene que elegir uno.</summary>
    private async Task ValidarRolAsync(CargaTraslado c, TrasladoRequest req, bool esColaborador, Convocatoria? convocatoria)
    {
        var disponibles = convocatoria != null
            ? convocatoria.Roles.Select(r => r.IdConvocatoriaRol).ToList()
            : (await cargas.RolesDisponiblesAsync(c.IdDomicilio, esColaborador ? c.IdUsuarioCarga : null)).Select(r => r.IdConvocatoriaRol).ToList();

        if (req.IdConvocatoriaRol is long idRol)
        {
            if (!disponibles.Contains(idRol)) throw new NegocioException("El rol elegido no corresponde a esta convocatoria");
        }
        else if (esColaborador && disponibles.Count > 0)
        {
            throw new NegocioException("Elegí tu rol");
        }
    }

    private static TrasladoDto ADto(CargaTraslado c, long idUsuario)
    {
        var lista = c.Registros.OrderBy(r => r.Periodo).ToList();
        var detalles = lista.SelectMany(r => r.Detalles).ToList();
        return new TrasladoDto(
            c.IdCargaTraslado, c.IdDomicilio, Mapeos.DescripcionCorta(c.Domicilio),
            c.IdConvocatoria, c.Convocatoria?.Nombre, c.IdConvocatoriaRol, c.ConvocatoriaRol?.Nombre,
            c.Periodicidad, c.PeriodoDesde, c.PeriodoHasta, lista.Count, c.MesesPeriodo,
            c.Semanas, c.CantidadPersonas,
            c.Medios.OrderBy(m => m.FuenteEmision.Nombre).Select(m => new MedioTrasladoDto(
                m.IdFuenteEmision, m.FuenteEmision.Nombre, m.DistanciaKm, m.DiasSemana, m.ValorFactorEmision,
                detalles.Where(d => d.IdFuenteEmision == m.IdFuenteEmision).Sum(d => d.Consumo),
                detalles.Where(d => d.IdFuenteEmision == m.IdFuenteEmision).Sum(d => d.KgCo2))).ToList(),
            detalles.Sum(d => d.Consumo), lista.Sum(r => r.TotalKgCo2),
            lista[0].IdRegistro, c.Domicilio.IdUsuario == idUsuario);
    }
}
