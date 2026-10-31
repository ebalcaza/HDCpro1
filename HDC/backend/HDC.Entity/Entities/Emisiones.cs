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

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario UsuarioCarga { get; set; } = null!;
    public ICollection<DetalleRegistro> Detalles { get; set; } = new List<DetalleRegistro>();
}

public class DetalleRegistro
{
    public long IdDetalle { get; set; }
    public long IdRegistro { get; set; }
    public long IdFuenteEmision { get; set; }
    public decimal Consumo { get; set; }
    public decimal ValorFactorEmision { get; set; }
    public decimal KgCo2 { get; set; }

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
    public string Estado { get; set; } = EstadoConvocatoria.Abierta;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public ICollection<ConvocatoriaTipoEmision> TiposEmision { get; set; } = new List<ConvocatoriaTipoEmision>();
    public ICollection<Colaboracion> Miembros { get; set; } = new List<Colaboracion>();
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
