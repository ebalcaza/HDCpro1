namespace HDC.Entity.Entities;

public class Provincia
{
    public long IdProvincia { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Distrito> Distritos { get; set; } = new List<Distrito>();
}

public class Distrito
{
    public long IdDistrito { get; set; }
    public long IdProvincia { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Provincia Provincia { get; set; } = null!;
    public ICollection<Ciudad> Ciudades { get; set; } = new List<Ciudad>();
}

public class Ciudad
{
    public long IdCiudad { get; set; }
    public long IdDistrito { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Distrito Distrito { get; set; } = null!;
}
