namespace HDC.Entity.Entities;

/// <summary>Cabecera: consumos de un domicilio en un período (primer día del mes).</summary>
public class Registro
{
    public long IdRegistro { get; set; }
    public long IdDomicilio { get; set; }
    public long IdUsuarioCarga { get; set; }
    public DateOnly Periodo { get; set; }
    public string? Observaciones { get; set; }
    public decimal TotalKgCo2 { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaUltimaModificacion { get; set; }
    /// <summary>Carga de traslado que generó este registro (null = registro común).</summary>
    public long? IdCargaTraslado { get; set; }
    /// <summary>
    /// Convocatoria a la que pertenece este aporte (v2.10). Una persona puede estar en varias convocatorias del mismo
    /// domicilio: sin este dato, lo cargado en una aparecía también en las otras. Null = titular o invitación por email.
    /// </summary>
    public long? IdConvocatoria { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario UsuarioCarga { get; set; } = null!;
    public CargaTraslado? CargaTraslado { get; set; }
    public Convocatoria? Convocatoria { get; set; }
    public ICollection<DetalleRegistro> Detalles { get; set; } = new List<DetalleRegistro>();
}

/// <summary>Períodos que se pueden declarar en un traslado (v2.6).</summary>
public static class Periodicidad
{
    public const string Mensual = "MENSUAL";
    public const string Semestral = "SEMESTRAL";
    public const string Anual = "ANUAL";
    /// <summary>Todo el período que definió el organizador de una convocatoria (v2.8).</summary>
    public const string PeriodoCompleto = "PERIODO";

    public static int Meses(string periodicidad) => periodicidad switch { Anual => 12, Semestral => 6, _ => 1 };

    /// <summary>Cantidad de meses de un rango, con los dos extremos incluidos.</summary>
    public static int MesesEntre(DateOnly desde, DateOnly hasta) => (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month + 1;

    public static int SemanasMaximas(string periodicidad) => periodicidad switch { Anual => 53, Semestral => 27, _ => 5 };
}

/// <summary>
/// Traslado declarado de una sola vez (v2.6). Genera un Registro por cada mes cubierto.
/// Conserva los datos ingresados para poder editarlos y mostrarlos.
/// </summary>
public class CargaTraslado
{
    public long IdCargaTraslado { get; set; }
    public long IdDomicilio { get; set; }
    public long IdUsuarioCarga { get; set; }
    /// <summary>Convocatoria que fijó el período (null = lo eligió quien cargó).</summary>
    public long? IdConvocatoria { get; set; }
    public long? IdConvocatoriaRol { get; set; }
    public string Periodicidad { get; set; } = HDC.Entity.Entities.Periodicidad.Mensual;
    public DateOnly PeriodoDesde { get; set; }
    /// <summary>Último mes cargado: nunca es posterior al mes en curso.</summary>
    public DateOnly PeriodoHasta { get; set; }
    /// <summary>Meses del período completo, aunque la carga cubra uno solo.</summary>
    public int MesesPeriodo { get; set; } = 1;
    /// <summary>Semanas con asistencia en el período completo.</summary>
    public decimal Semanas { get; set; }
    public int CantidadPersonas { get; set; } = 1;
    public DateTime FechaCreacion { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario UsuarioCarga { get; set; } = null!;
    public Convocatoria? Convocatoria { get; set; }
    public ConvocatoriaRol? ConvocatoriaRol { get; set; }
    public ICollection<CargaTrasladoMedio> Medios { get; set; } = new List<CargaTrasladoMedio>();
    public ICollection<Registro> Registros { get; set; } = new List<Registro>();
}

/// <summary>Un medio de transporte de una carga, con su distancia, sus días y su factor congelado (v2.8).</summary>
public class CargaTrasladoMedio
{
    public long IdCargaTrasladoMedio { get; set; }
    public long IdCargaTraslado { get; set; }
    public long IdFuenteEmision { get; set; }
    /// <summary>Distancia de ida y vuelta por día de asistencia.</summary>
    public decimal DistanciaKm { get; set; }
    public decimal DiasSemana { get; set; }
    public decimal ValorFactorEmision { get; set; }

    public CargaTraslado CargaTraslado { get; set; } = null!;
    public FuenteEmision FuenteEmision { get; set; } = null!;
}

public class DetalleRegistro
{
    public long IdDetalle { get; set; }
    public long IdRegistro { get; set; }
    public long IdFuenteEmision { get; set; }
    public decimal Consumo { get; set; }
    public decimal ValorFactorEmision { get; set; }
    public decimal KgCo2 { get; set; }
    /// <summary>Opcional, solo en vehículos: kilómetros recorridos. No interviene en el cálculo.</summary>
    public decimal? Kilometros { get; set; }
    /// <summary>Solo en viajes institucionales: fecha del viaje.</summary>
    public DateOnly? FechaViaje { get; set; }

    public Registro Registro { get; set; } = null!;
    public FuenteEmision FuenteEmision { get; set; } = null!;
}

public static class EstadoColaboracion
{
    /// <summary>Invitación por email esperando la respuesta del invitado.</summary>
    public const string Pendiente = "PENDIENTE";
    /// <summary>Se sumó con el código de una convocatoria y espera la aprobación del titular.</summary>
    public const string Solicitada = "SOLICITADA";
    public const string Aceptada = "ACEPTADA";
    public const string Rechazada = "RECHAZADA";
    public const string Revocada = "REVOCADA";
}

public class Colaboracion
{
    public long IdColaboracion { get; set; }
    public long IdDomicilio { get; set; }
    public long IdUsuarioColaborador { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly? PeriodoHasta { get; set; }
    public string Estado { get; set; } = EstadoColaboracion.Pendiente;
    public string? Mensaje { get; set; }
    public DateTime FechaInvitacion { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    /// <summary>Convocatoria de la que nació (null = invitación individual por email).</summary>
    public long? IdConvocatoria { get; set; }
    /// <summary>Quién la rechazó o finalizó: el titular o el propio colaborador (null = sigue activa).</summary>
    public long? IdUsuarioBaja { get; set; }
    /// <summary>Rol elegido en la convocatoria (v2.9): el formulario de traslados lo propone para no pedirlo cada vez.</summary>
    public long? IdConvocatoriaRol { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario Colaborador { get; set; } = null!;
    public Convocatoria? Convocatoria { get; set; }
    /// <summary>Tipos habilitados de una invitación individual. Si viene de una convocatoria, valen los de la convocatoria.</summary>
    public ICollection<ColaboracionTipoEmision> TiposEmision { get; set; } = new List<ColaboracionTipoEmision>();

    /// <summary>Ids de los tipos de emisión que puede cargar: los de la convocatoria, o los propios si es una invitación.</summary>
    public IEnumerable<long> IdsTiposHabilitados() => Convocatoria != null
        ? Convocatoria.TiposEmision.Select(t => t.IdTipoEmision)
        : TiposEmision.Select(t => t.IdTipoEmision);
}

public static class EstadoConvocatoria
{
    /// <summary>Se puede sumar gente con el código.</summary>
    public const string Abierta = "ABIERTA";
    /// <summary>No entra nadie nuevo; los miembros aceptados siguen cargando.</summary>
    public const string Cerrada = "CERRADA";
    /// <summary>Terminó: nadie carga más y no se puede reabrir.</summary>
    public const string Finalizada = "FINALIZADA";
}

/// <summary>
/// Convocatoria: el titular de un domicilio comparte un código para que muchas personas se sumen
/// como colaboradoras. Los períodos y tipos de emisión se definen acá, una vez para todos los miembros.
/// </summary>
public class Convocatoria
{
    public long IdConvocatoria { get; set; }
    public long IdDomicilio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    /// <summary>8 caracteres sin guion (se muestra como XXXX-XXXX).</summary>
    public string Codigo { get; set; } = string.Empty;
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly? PeriodoHasta { get; set; }
    /// <summary>true = quien se suma con el código queda aceptado sin que el titular lo apruebe.</summary>
    public bool AprobacionAutomatica { get; set; }
    /// <summary>Semanas de actividad del período: las define el organizador para los traslados (v2.8).</summary>
    public decimal? SemanasTraslado { get; set; }
    public string Estado { get; set; } = EstadoConvocatoria.Abierta;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public ICollection<ConvocatoriaTipoEmision> TiposEmision { get; set; } = new List<ConvocatoriaTipoEmision>();
    public ICollection<Colaboracion> Miembros { get; set; } = new List<Colaboracion>();
    public ICollection<ConvocatoriaRol> Roles { get; set; } = new List<ConvocatoriaRol>();
}

/// <summary>
/// Rol que define quien crea la convocatoria y que elige cada colaborador al cargar su traslado (v2.6).
/// Ejemplo en una facultad: estudiantes, personal docente y personal no docente.
/// </summary>
public class ConvocatoriaRol
{
    public long IdConvocatoriaRol { get; set; }
    public long IdConvocatoria { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Convocatoria Convocatoria { get; set; } = null!;
}

public class ConvocatoriaTipoEmision
{
    public long IdConvocatoria { get; set; }
    public long IdTipoEmision { get; set; }

    public Convocatoria Convocatoria { get; set; } = null!;
    public TipoEmision TipoEmision { get; set; } = null!;
}

public class ColaboracionTipoEmision
{
    public long IdColaboracion { get; set; }
    public long IdTipoEmision { get; set; }

    public Colaboracion Colaboracion { get; set; } = null!;
    public TipoEmision TipoEmision { get; set; } = null!;
}

public class Reporte
{
    public long IdReporte { get; set; }
    public long IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Filtros { get; set; } = "{}";
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
