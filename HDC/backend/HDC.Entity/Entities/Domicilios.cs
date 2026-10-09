namespace HDC.Entity.Entities;

public class Domicilio
{
    public long IdDomicilio { get; set; }
    public long IdCiudad { get; set; }
    public long IdUsuario { get; set; }
    public string Calle { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string? Departamento { get; set; }
    public string? Piso { get; set; }
    public string? Block { get; set; }
    public string? Manzana { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
    public decimal TotalKgCo2 { get; set; }

    public Ciudad Ciudad { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<DomicilioFuente> Fuentes { get; set; } = new List<DomicilioFuente>();
}

public class DomicilioFuente
{
    public long IdDomicilio { get; set; }
    public long IdFuenteEmision { get; set; }

    public Domicilio Domicilio { get; set; } = null!;
    public FuenteEmision FuenteEmision { get; set; } = null!;
}
