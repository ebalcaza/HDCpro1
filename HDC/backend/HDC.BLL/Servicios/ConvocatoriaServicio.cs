using System.Security.Cryptography;
using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

/// <summary>
/// Convocatorias (v2.2): colaboración masiva por código, para cuando el titular necesita sumar a muchas
/// personas (por ejemplo, los 3.000 empleados de una organización) sin invitarlas una por una.
///
/// Flujo:
///   1. El titular crea la convocatoria para uno de sus domicilios: nombre, períodos, tipos de emisión que se
///      pueden cargar y si la aprobación es automática. El sistema genera un código (ej. ECSR-2K26).
///   2. El titular comparte el código. Cada persona lo escribe en "Sumarme con un código", ve a qué se suma
///      y confirma. Queda ACEPTADA (aprobación automática) o SOLICITADA (espera al titular).
///   3. El titular gestiona los miembros: aceptar, rechazar, dar de baja o reactivar, de a uno o en grupo
///      (seleccionados o todos los que cumplen un filtro).
///   4. Editar los períodos o los tipos de la convocatoria vale para todos sus miembros a la vez.
///
/// Reglas importantes:
///   - Si el titular rechaza o da de baja a alguien, esa persona NO puede volver a entrar con el código
///     (solo el titular puede reactivarla). Si la persona se fue por su cuenta, sí puede volver a sumarse.
///   - Estados de la convocatoria: ABIERTA (entra gente), CERRADA (no entra nadie nuevo, los miembros siguen
///     cargando) y FINALIZADA (nadie carga más; no se reabre).
/// </summary>
public class ConvocatoriaServicio(
    ConvocatoriaRepositorio convocatorias,
    DomicilioRepositorio domicilios,
    TipoEmisionRepositorio tipos,
    ColaboracionServicio colaboracionServicio)
{
    /// <summary>Caracteres del código: sin 0/O ni 1/I, que se confunden al dictarlos o copiarlos.</summary>
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int LargoCodigo = 8;

    // ======================= Titular =======================

    /// <summary>Convocatorias de todos los domicilios del titular, con conteos de miembros y aportes.</summary>
    public async Task<List<ConvocatoriaDto>> ListarAsync(long idTitular) =>
        await ADtosAsync(await convocatorias.ListarPorTitularAsync(idTitular));

    /// <summary>Una convocatoria del titular (403 si es de otro usuario, 404 si no existe).</summary>
    public async Task<ConvocatoriaDto> ObtenerAsync(long idTitular, long id) =>
        (await ADtosAsync([await ObtenerPropiaAsync(idTitular, id)]))[0];

    /// <summary>
    /// Crea una convocatoria ABIERTA en un domicilio propio y activo, con un código nuevo.
    /// Los datos editables (nombre, períodos, tipos, aprobación) los completa AplicarAsync.
    /// </summary>
    public async Task<ConvocatoriaDto> CrearAsync(long idTitular, CrearConvocatoriaRequest req)
    {
        var domicilio = await domicilios.ObtenerPorIdAsync(req.IdDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        if (domicilio.IdUsuario != idTitular) throw new ProhibidoException("El domicilio no te pertenece");
        if (domicilio.FechaBaja != null) throw new NegocioException("El domicilio está dado de baja");

        var c = new Convocatoria
        {
            IdDomicilio = domicilio.IdDomicilio,
            Codigo = await GenerarCodigoUnicoAsync(),
            Estado = EstadoConvocatoria.Abierta,
            FechaCreacion = DateTime.Now
        };
        await AplicarAsync(c, req);
        convocatorias.Agregar(c);
        await convocatorias.GuardarAsync();
        return await ObtenerAsync(idTitular, c.IdConvocatoria);
    }

    /// <summary>
    /// Edita una convocatoria no finalizada. Los tipos se leen siempre de la convocatoria, así que valen
    /// para todos los miembros al instante; los períodos se copian a cada miembro con SincronizarPeriodosAsync.
    /// </summary>
    public async Task<ConvocatoriaDto> ActualizarAsync(long idTitular, long id, ConvocatoriaRequest req)
    {
        var c = await ObtenerPropiaAsync(idTitular, id);
        if (c.Estado == EstadoConvocatoria.Finalizada) throw new NegocioException("La convocatoria está finalizada y no se puede modificar");
        await AplicarAsync(c, req);
        c.FechaModificacion = DateTime.Now;
        await convocatorias.GuardarAsync();
        // Los períodos se copian a cada miembro (los usa la validación de carga); los tipos se leen siempre de la convocatoria
        await convocatorias.SincronizarPeriodosAsync(c.IdConvocatoria, c.PeriodoDesde, c.PeriodoHasta);
        return await ObtenerAsync(idTitular, id);
    }

    /// <summary>Abrir, cerrar o finalizar. Al finalizar, todos los miembros activos quedan dados de baja.</summary>
    public async Task<ConvocatoriaDto> CambiarEstadoAsync(long idTitular, long id, string estado)
    {
        var c = await ObtenerPropiaAsync(idTitular, id);
        if (c.Estado == EstadoConvocatoria.Finalizada) throw new NegocioException("La convocatoria ya está finalizada y no se puede reabrir");
        if (estado == EstadoConvocatoria.Abierta && c.Domicilio.FechaBaja != null)
            throw new NegocioException("El domicilio está dado de baja: no se puede abrir la convocatoria");

        var ahora = DateTime.Now;
        c.Estado = estado;
        c.FechaModificacion = ahora;
        await convocatorias.GuardarAsync();

        if (estado == EstadoConvocatoria.Finalizada)
        {
            await convocatorias.CambiarEstadoAsync(convocatorias.MiembrosFiltrados(id, EstadoColaboracion.Aceptada, null), EstadoColaboracion.Revocada, idTitular, ahora);
            await convocatorias.CambiarEstadoAsync(convocatorias.MiembrosFiltrados(id, EstadoColaboracion.Solicitada, null), EstadoColaboracion.Rechazada, idTitular, ahora);
        }
        return await ObtenerAsync(idTitular, id);
    }

    /// <summary>
    /// Elimina la convocatoria por completo, en cualquier estado (v2.11): también sus miembros y todo lo que cargaron en ella,
    /// que deja de sumar a la huella del domicilio. Para conservar lo cargado, hay que finalizarla. No se puede deshacer.
    /// </summary>
    public async Task EliminarAsync(long idTitular, long id)
    {
        var c = await ObtenerPropiaAsync(idTitular, id);
        await convocatorias.EliminarConAportesAsync(c);
    }

    /// <summary>Página de miembros de la convocatoria (filtro por estado, búsqueda y orden se resuelven en SQL).</summary>
    public async Task<PaginaDto<MiembroConvocatoriaDto>> ListarMiembrosAsync(long idTitular, long id, FiltroMiembros filtro)
    {
        await ObtenerPropiaAsync(idTitular, id); // solo valida que sea del titular
        return await convocatorias.PaginarMiembrosAsync(id, idTitular, filtro);
    }

    /// <summary>Detalle de lo que cargaron los colaboradores, todos o una persona (v2.8). Solo lo ve el titular.</summary>
    public async Task<PaginaDto<AporteConvocatoriaDto>> ListarAportesAsync(long idTitular, long id, FiltroAportes filtro)
    {
        var c = await ObtenerPropiaAsync(idTitular, id);
        return await convocatorias.PaginarAportesAsync(c, filtro);
    }

    /// <summary>
    /// Acción sobre varios miembros: los Ids indicados o TODOS los que cumplen el filtro.
    /// Cada acción solo toca a los miembros cuyo estado actual lo permite.
    /// </summary>
    public async Task<ResultadoAccionDto> AplicarAccionAsync(long idTitular, long id, AccionMiembrosRequest req)
    {
        var c = await ObtenerPropiaAsync(idTitular, id);
        if (!req.Todos && (req.Ids is null || req.Ids.Count == 0)) throw new NegocioException("No seleccionaste colaboradores");
        if (c.Estado == EstadoConvocatoria.Finalizada && req.Accion is AccionMiembros.Aceptar or AccionMiembros.Reactivar)
            throw new NegocioException("La convocatoria está finalizada");

        var q = req.Todos
            ? convocatorias.MiembrosFiltrados(id, req.Estado, req.Busqueda)
            : convocatorias.MiembrosFiltrados(id, null, null).Where(m => req.Ids!.Contains(m.IdColaboracion));

        var ahora = DateTime.Now;
        var (afectados, singular, plural) = req.Accion switch
        {
            AccionMiembros.Aceptar => (await convocatorias.CambiarEstadoAsync(
                q.Where(m => m.Estado == EstadoColaboracion.Solicitada), EstadoColaboracion.Aceptada, null, ahora), "aceptado", "aceptados"),
            AccionMiembros.Rechazar => (await convocatorias.CambiarEstadoAsync(
                q.Where(m => m.Estado == EstadoColaboracion.Solicitada), EstadoColaboracion.Rechazada, idTitular, ahora), "rechazado", "rechazados"),
            AccionMiembros.Baja => (await convocatorias.CambiarEstadoAsync(
                q.Where(m => m.Estado == EstadoColaboracion.Aceptada), EstadoColaboracion.Revocada, idTitular, ahora), "dado de baja", "dados de baja"),
            // Solo se reactiva a quien rechazó o dio de baja el TITULAR: a quien se fue por su cuenta no se lo vuelve a sumar a la fuerza
            AccionMiembros.Reactivar => (await convocatorias.CambiarEstadoAsync(
                q.Where(m => (m.Estado == EstadoColaboracion.Rechazada || m.Estado == EstadoColaboracion.Revocada) && m.IdUsuarioBaja == idTitular),
                EstadoColaboracion.Aceptada, null, ahora), "reactivado", "reactivados"),
            _ => throw new NegocioException("Acción inválida")
        };

        var mensaje = afectados switch
        {
            0 => "Ningún colaborador estaba en un estado que permita esa acción",
            1 => $"1 colaborador {singular}",
            _ => $"{afectados} colaboradores {plural}"
        };
        return new ResultadoAccionDto(afectados, mensaje);
    }

    // ======================= Colaborador =======================

    /// <summary>Lo que ve el usuario al escribir el código, antes de confirmar.</summary>
    public async Task<VistaPreviaConvocatoriaDto> VistaPreviaAsync(long idUsuario, string codigoIngresado)
    {
        var c = await BuscarPorCodigoAsync(codigoIngresado);
        if (c.Domicilio.IdUsuario == idUsuario) throw new NegocioException("Es una convocatoria tuya: no podés sumarte como colaborador");
        var miembro = await convocatorias.ObtenerMiembroAsync(c.IdConvocatoria, idUsuario);
        return new VistaPreviaConvocatoriaDto(
            FormatearCodigo(c.Codigo), c.Nombre, c.Descripcion,
            Mapeos.NombreMostrar(c.Domicilio.Usuario), $"{Mapeos.DescripcionCorta(c.Domicilio)} - {c.Domicilio.Ciudad.Nombre}",
            c.PeriodoDesde, c.PeriodoHasta, c.AprobacionAutomatica, Tipos(c), miembro?.Estado,
            c.Roles.OrderBy(r => r.Nombre).Select(r => new RolOpcionDto(r.IdConvocatoriaRol, r.Nombre, c.Nombre)).ToList(),
            miembro?.IdConvocatoriaRol);
    }

    /// <summary>Sumarse a una convocatoria con su código.</summary>
    public async Task<ColaboracionDto> UnirseAsync(long idUsuario, string codigoIngresado, long? idRol)
    {
        var c = await BuscarPorCodigoAsync(codigoIngresado);
        if (c.Domicilio.IdUsuario == idUsuario) throw new NegocioException("Es una convocatoria tuya: no podés sumarte como colaborador");
        if (c.Estado != EstadoConvocatoria.Abierta) throw new NegocioException("La convocatoria no está recibiendo nuevos colaboradores");
        var rol = ValidarRolAlSumarse(c, idRol);

        var estado = c.AprobacionAutomatica ? EstadoColaboracion.Aceptada : EstadoColaboracion.Solicitada;
        var ahora = DateTime.Now;
        var existente = await convocatorias.ObtenerMiembroAsync(c.IdConvocatoria, idUsuario);

        if (existente != null)
        {
            if (existente.Estado is EstadoColaboracion.Aceptada or EstadoColaboracion.Solicitada)
                throw new ConflictoException("Ya formás parte de esta convocatoria");
            if (existente.IdUsuarioBaja == c.Domicilio.IdUsuario)
                throw new ProhibidoException("El titular de la convocatoria no está recibiendo tus aportes. Si creés que es un error, contactalo.");
            // Se había ido por su cuenta: vuelve a sumarse con la misma fila (se conserva el historial de aportes)
            existente.Estado = estado;
            existente.FechaInvitacion = ahora;
            existente.FechaRespuesta = c.AprobacionAutomatica ? ahora : null;
            existente.IdUsuarioBaja = null;
            existente.IdConvocatoriaRol = rol;
            existente.PeriodoDesde = c.PeriodoDesde;
            existente.PeriodoHasta = c.PeriodoHasta;
            await convocatorias.GuardarAsync();
            return await colaboracionServicio.ObtenerDtoPublicoAsync(existente.IdColaboracion);
        }

        var nueva = new Colaboracion
        {
            IdDomicilio = c.IdDomicilio,
            IdUsuarioColaborador = idUsuario,
            IdConvocatoria = c.IdConvocatoria,
            IdConvocatoriaRol = rol,
            PeriodoDesde = c.PeriodoDesde,
            PeriodoHasta = c.PeriodoHasta,
            Estado = estado,
            FechaInvitacion = ahora,
            FechaRespuesta = c.AprobacionAutomatica ? ahora : null
        };
        convocatorias.AgregarMiembro(nueva);
        await convocatorias.GuardarAsync();
        return await colaboracionServicio.ObtenerDtoPublicoAsync(nueva.IdColaboracion);
    }

    // ======================= Auxiliares =======================

    /// <summary>Si la convocatoria tiene roles, quien se suma elige uno y no se le vuelve a pedir en cada carga (v2.9).</summary>
    private static long? ValidarRolAlSumarse(Convocatoria c, long? idRol)
    {
        if (c.Roles.Count == 0) return null;
        if (idRol is null) throw new NegocioException("Elegí tu rol en la convocatoria");
        if (c.Roles.All(r => r.IdConvocatoriaRol != idRol)) throw new NegocioException("El rol elegido no corresponde a esta convocatoria");
        return idRol;
    }

    private async Task<Convocatoria> ObtenerPropiaAsync(long idTitular, long id)
    {
        var c = await convocatorias.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Convocatoria no encontrada");
        if (c.Domicilio.IdUsuario != idTitular) throw new ProhibidoException();
        return c;
    }

    private async Task<Convocatoria> BuscarPorCodigoAsync(string codigoIngresado)
    {
        var c = await convocatorias.ObtenerPorCodigoAsync(NormalizarCodigo(codigoIngresado));
        // Mismo mensaje si no existe o si ya terminó: no se revela qué códigos existieron
        if (c is null || c.Estado == EstadoConvocatoria.Finalizada)
            throw new NoEncontradoException("El código no existe o la convocatoria ya no está disponible. Revisá que esté bien escrito.");
        return c;
    }

    private async Task AplicarAsync(Convocatoria c, ConvocatoriaRequest req)
    {
        var desde = Mapeos.InicioDeMes(req.PeriodoDesde);
        var hasta = req.PeriodoHasta is DateOnly h ? Mapeos.InicioDeMes(h) : (DateOnly?)null;
        if (hasta < desde) throw new NegocioException("El período hasta debe ser posterior al período desde");

        var ids = req.IdsTiposEmision.Distinct().ToList();
        if ((await tipos.ObtenerPorIdsAsync(ids)).Count != ids.Count) throw new NegocioException("Alguno de los tipos de emisión no existe");

        c.Nombre = req.Nombre.Trim();
        c.Descripcion = Mapeos.Limpiar(req.Descripcion);
        c.PeriodoDesde = desde;
        c.PeriodoHasta = hasta;
        c.AprobacionAutomatica = req.AprobacionAutomatica;

        // Con traslados, el organizador define el período completo y sus semanas: quien colabora ya no los elige (v2.8)
        var tiposElegidos = await tipos.ObtenerPorIdsAsync(ids);
        if (tiposElegidos.Any(t => t.ModoCarga == ModoCarga.Traslado))
        {
            if (hasta is null) throw new NegocioException("Indicá hasta qué mes va el período: los colaboradores cargan sus traslados dentro de él");
            if (req.SemanasTraslado is not decimal semanas) throw new NegocioException("Indicá las semanas de actividad del período");
            var maximo = Periodicidad.MesesEntre(desde, hasta.Value) * 5;
            if (semanas > maximo) throw new NegocioException($"Ese período tiene como máximo {maximo} semanas");
            c.SemanasTraslado = semanas;
        }
        else
        {
            c.SemanasTraslado = null;
        }

        var quitar = c.TiposEmision.Where(t => !ids.Contains(t.IdTipoEmision)).ToList();
        convocatorias.QuitarTipos(quitar);
        foreach (var q in quitar) c.TiposEmision.Remove(q);
        foreach (var id in ids.Where(id => c.TiposEmision.All(t => t.IdTipoEmision != id)))
            c.TiposEmision.Add(new ConvocatoriaTipoEmision { IdTipoEmision = id });

        await AplicarRolesAsync(c, req.Roles);
    }

    /// <summary>Sincroniza los roles por nombre, sin distinguir mayúsculas. Un rol ya elegido en un traslado no se puede quitar.</summary>
    private async Task AplicarRolesAsync(Convocatoria c, List<string> nombres)
    {
        var limpios = nombres.Select(n => n?.Trim() ?? string.Empty).Where(n => n.Length > 0)
            .DistinctBy(n => n.ToLowerInvariant()).ToList();
        if (limpios.Any(n => n.Length > 100)) throw new NegocioException("El nombre de un rol puede tener hasta 100 caracteres");

        foreach (var rol in c.Roles.Where(r => !limpios.Contains(r.Nombre, StringComparer.OrdinalIgnoreCase)).ToList())
        {
            if (await convocatorias.RolEnUsoAsync(rol.IdConvocatoriaRol))
                throw new ConflictoException($"El rol \"{rol.Nombre}\" ya fue elegido en traslados cargados: no se puede quitar");
            convocatorias.QuitarRol(rol);
            c.Roles.Remove(rol);
        }
        foreach (var nombre in limpios.Where(n => !c.Roles.Any(r => string.Equals(r.Nombre, n, StringComparison.OrdinalIgnoreCase))))
            c.Roles.Add(new ConvocatoriaRol { Nombre = nombre });
    }

    /// <summary>Código aleatorio criptográficamente seguro de 8 caracteres (32^8 ≈ 1 billón de combinaciones).</summary>
    private async Task<string> GenerarCodigoUnicoAsync()
    {
        while (true)
        {
            var codigo = new string(Enumerable.Range(0, LargoCodigo).Select(_ => Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]).ToArray());
            if (!await convocatorias.ExisteCodigoAsync(codigo)) return codigo;
        }
    }

    /// <summary>Acepta el código con o sin guion, espacios o minúsculas: "ecsr 2k26" → "ECSR2K26".</summary>
    public static string NormalizarCodigo(string codigo) =>
        new string(codigo.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>"ECSR2K26" → "ECSR-2K26" (más fácil de leer y dictar).</summary>
    public static string FormatearCodigo(string codigo) =>
        codigo.Length == LargoCodigo ? $"{codigo[..4]}-{codigo[4..]}" : codigo;

    private static List<TipoEmisionResumenDto> Tipos(Convocatoria c) => c.TiposEmision
        .Select(t => new TipoEmisionResumenDto(t.IdTipoEmision, t.TipoEmision.Nombre, t.TipoEmision.IdAlcance, t.TipoEmision.Alcance.Nombre))
        .OrderBy(t => t.Alcance).ThenBy(t => t.Nombre).ToList();

    private async Task<List<ConvocatoriaDto>> ADtosAsync(List<Convocatoria> lista)
    {
        var ids = lista.Select(c => c.IdConvocatoria).ToList();
        var conteos = await convocatorias.ContarPorEstadoAsync(ids);
        var aportes = await convocatorias.AportesAsync(ids);
        var porRol = await convocatorias.ResultadosPorRolAsync(lista.SelectMany(c => c.Roles).Select(r => r.IdConvocatoriaRol).ToList());
        int N(long id, string estado) => conteos.GetValueOrDefault((id, estado));

        return lista.Select(c => new ConvocatoriaDto(
            c.IdConvocatoria, c.IdDomicilio, Mapeos.DescripcionCorta(c.Domicilio),
            c.Nombre, c.Descripcion, FormatearCodigo(c.Codigo),
            c.PeriodoDesde, c.PeriodoHasta, c.AprobacionAutomatica, c.Estado, c.FechaCreacion, Tipos(c),
            N(c.IdConvocatoria, EstadoColaboracion.Aceptada),
            N(c.IdConvocatoria, EstadoColaboracion.Solicitada),
            N(c.IdConvocatoria, EstadoColaboracion.Revocada) + N(c.IdConvocatoria, EstadoColaboracion.Rechazada),
            aportes.GetValueOrDefault(c.IdConvocatoria),
            c.Roles.OrderBy(r => r.Nombre).Select(r =>
            {
                var resultado = porRol.GetValueOrDefault(r.IdConvocatoriaRol);
                return new RolConvocatoriaDto(r.IdConvocatoriaRol, r.Nombre, resultado.Colaboradores, resultado.Kg);
            }).ToList(),
            c.SemanasTraslado)).ToList();
    }
}
