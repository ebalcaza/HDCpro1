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
    public const string Pendiente = "PENDIENTE";
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

    public Domicilio Domicilio { get; set; } = null!;
    public Usuario Colaborador { get; set; } = null!;
    public ICollection<ColaboracionTipoEmision> TiposEmision { get; set; } = new List<ColaboracionTipoEmision>();
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
