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

/// <summary>
/// Pedido de recuperación de contraseña ("Olvidé mi contraseña").
/// Solo se guarda el hash SHA-256 del token que viaja en el link del email.
/// </summary>
public class RecuperacionContrasena
{
    public long IdRecuperacion { get; set; }
    public long IdUsuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    /// <summary>Cuándo se usó el link; null = todavía no se usó. Cada link sirve una sola vez.</summary>
    public DateTime? FechaUso { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
