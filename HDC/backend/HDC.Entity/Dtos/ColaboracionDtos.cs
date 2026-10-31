using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class InvitarColaboradorRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El domicilio es obligatorio")]
    public long IdDomicilio { get; set; }

    [Required(ErrorMessage = "El email del colaborador es obligatorio"), EmailAddress(ErrorMessage = "Email inválido")]
    public string EmailColaborador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El período desde es obligatorio")]
    public DateOnly PeriodoDesde { get; set; }

    public DateOnly? PeriodoHasta { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un tipo de emisión")]
    public List<long> IdsTiposEmision { get; set; } = new();

    [MaxLength(500)]
    public string? Mensaje { get; set; }
}

public class ActualizarColaboracionRequest
{
    [Required]
    public DateOnly PeriodoDesde { get; set; }

    public DateOnly? PeriodoHasta { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un tipo de emisión")]
    public List<long> IdsTiposEmision { get; set; } = new();
}

public record TipoEmisionResumenDto(long IdTipoEmision, string Nombre, long IdAlcance, string Alcance);

public record ColaboracionDto(
    long IdColaboracion,
    long IdDomicilio,
    string Domicilio,
    long IdTitular,
    string Titular,
    string EmailTitular,
    long IdColaborador,
    string Colaborador,
    string EmailColaborador,
    DateOnly PeriodoDesde,
    DateOnly? PeriodoHasta,
    string Estado,
    string? Mensaje,
    DateTime FechaInvitacion,
    DateTime? FechaRespuesta,
    bool Vigente,
    IReadOnlyList<TipoEmisionResumenDto> TiposEmision,
    int CantidadRegistros,
    decimal TotalKgCo2Aportado,
    long? IdConvocatoria,
    string? Convocatoria);

// ======================= Convocatorias (v2.2) =======================

/// <summary>Edición de una convocatoria (el domicilio no se puede cambiar).</summary>
public class ConvocatoriaRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(150, ErrorMessage = "Máximo 150 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El período desde es obligatorio")]
    public DateOnly PeriodoDesde { get; set; }

    public DateOnly? PeriodoHasta { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un tipo de emisión")]
    public List<long> IdsTiposEmision { get; set; } = new();

    /// <summary>true = quien se suma con el código queda aceptado sin aprobación del titular.</summary>
    public bool AprobacionAutomatica { get; set; }
}

/// <summary>Alta de una convocatoria: además de los datos editables, el domicilio al que pertenece.</summary>
public class CrearConvocatoriaRequest : ConvocatoriaRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El domicilio es obligatorio")]
    public long IdDomicilio { get; set; }
}

public class CambiarEstadoConvocatoriaRequest
{
    [Required, RegularExpression("^(ABIERTA|CERRADA|FINALIZADA)$", ErrorMessage = "Estado inválido")]
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Convocatoria vista por su titular, con los conteos de miembros.</summary>
public record ConvocatoriaDto(
    long IdConvocatoria,
    long IdDomicilio,
    string Domicilio,
    string Nombre,
    string? Descripcion,
    string Codigo,
    DateOnly PeriodoDesde,
    DateOnly? PeriodoHasta,
    bool AprobacionAutomatica,
    string Estado,
    DateTime FechaCreacion,
    IReadOnlyList<TipoEmisionResumenDto> TiposEmision,
    int Aceptados,
    int Solicitudes,
    int Bajas,
    decimal TotalKgCo2Aportado);

/// <summary>Filtros del listado de miembros de una convocatoria (además de la paginación y la búsqueda).</summary>
public class FiltroMiembros : ConsultaPagina
{
    /// <summary>SOLICITADA, ACEPTADA, RECHAZADA, REVOCADA o vacío para todos.</summary>
    public string? Estado { get; set; }
}

/// <summary>Una persona que se sumó a la convocatoria.</summary>
public record MiembroConvocatoriaDto(
    long IdColaboracion,
    long IdColaborador,
    string Colaborador,
    string Email,
    string Estado,
    DateTime FechaSolicitud,
    DateTime? FechaRespuesta,
    // true si la baja o el rechazo lo decidió el titular (no puede volver a sumarse con el código)
    bool BajaPorTitular,
    int CantidadRegistros,
    decimal TotalKgCo2Aportado);

public static class AccionMiembros
{
    public const string Aceptar = "aceptar";       // SOLICITADA → ACEPTADA
    public const string Rechazar = "rechazar";     // SOLICITADA → RECHAZADA
    public const string Baja = "baja";             // ACEPTADA → REVOCADA (deja de recibir sus aportes)
    public const string Reactivar = "reactivar";   // RECHAZADA/REVOCADA por el titular → ACEPTADA
}

/// <summary>
/// Acción sobre varios miembros a la vez. Se aplica a los Ids indicados, o (Todos = true) a TODOS los miembros
/// que cumplen el filtro Estado + Busqueda, aunque no estén en la página visible.
/// Cada acción solo afecta a los miembros cuyo estado lo permite (ej. "aceptar" ignora a los ya aceptados).
/// </summary>
public class AccionMiembrosRequest
{
    [Required, RegularExpression("^(aceptar|rechazar|baja|reactivar)$", ErrorMessage = "Acción inválida")]
    public string Accion { get; set; } = string.Empty;

    /// <summary>Miembros elegidos (ids de colaboración). Se ignora si Todos = true.</summary>
    public List<long>? Ids { get; set; }

    /// <summary>true = aplicar a TODOS los que cumplen Estado + Busqueda, aunque no estén en la página visible.</summary>
    public bool Todos { get; set; }

    /// <summary>Filtro de estado (solo con Todos = true); null = todos los estados.</summary>
    public string? Estado { get; set; }

    /// <summary>Texto buscado (solo con Todos = true): nombre, apellido, razón social, DNI o email.</summary>
    public string? Busqueda { get; set; }
}

public record ResultadoAccionDto(int Afectados, string Mensaje);

/// <summary>Lo que ve un usuario al escribir un código, antes de confirmar que se suma.</summary>
public record VistaPreviaConvocatoriaDto(
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Titular,
    string Domicilio,
    DateOnly PeriodoDesde,
    DateOnly? PeriodoHasta,
    bool AprobacionAutomatica,
    IReadOnlyList<TipoEmisionResumenDto> TiposEmision,
    // Estado del usuario en esta convocatoria si ya se había sumado (null = nunca)
    string? EstadoActual);

public class CodigoConvocatoriaRequest
{
    [Required(ErrorMessage = "Ingresá el código"), MaxLength(20)]
    public string Codigo { get; set; } = string.Empty;
}
