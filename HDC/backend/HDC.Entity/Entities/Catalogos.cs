namespace HDC.Entity.Entities;

public class UnidadMedida
{
    public long IdUnidadMedida { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class Alcance
{
    public long IdAlcance { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<TipoEmision> TiposEmision { get; set; } = new List<TipoEmision>();
}

public class TipoEmision
{
    public long IdTipoEmision { get; set; }
    public long IdAlcance { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Alcance Alcance { get; set; } = null!;
}

public class FuenteEmision
{
    public long IdFuenteEmision { get; set; }
    public long IdUnidadMedida { get; set; }
    public long IdTipoEmision { get; set; }
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Texto de ayuda para el usuario: qué es la fuente y dónde encontrar el dato del consumo.</summary>
    public string? Descripcion { get; set; }
    public decimal ValorFactorEmision { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public UnidadMedida UnidadMedida { get; set; } = null!;
    public TipoEmision TipoEmision { get; set; } = null!;
}
