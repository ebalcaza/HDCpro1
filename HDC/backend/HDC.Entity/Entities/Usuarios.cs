namespace HDC.Entity.Entities;

public class Usuario
{
    public long IdUsuario { get; set; }
    public long IdRol { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }

    public Rol Rol { get; set; } = null!;
    public Individuo? Individuo { get; set; }
    public Organizacion? Organizacion { get; set; }
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    public ICollection<Domicilio> Domicilios { get; set; } = new List<Domicilio>();
}

public class Individuo
{
    public long IdUsuario { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

public class Organizacion
{
    public long IdUsuario { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CantidadMiembros { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

public class Vehiculo
{
    public long IdVehiculo { get; set; }
    public long IdUsuario { get; set; }
    public string TipoVehiculo { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Patente { get; set; } = string.Empty;
    public string TipoCombustible { get; set; } = string.Empty;
    public string TipoUso { get; set; } = string.Empty;

    public Usuario Usuario { get; set; } = null!;
}
