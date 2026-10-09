using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public static class TipoPerfil
{
    public const string Individuo = "INDIVIDUO";
    public const string Organizacion = "ORGANIZACION";
}

public class LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio"), EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Contrasena { get; set; } = string.Empty;
}

public abstract class RegistroBaseRequest
{
    [Required(ErrorMessage = "El email es obligatorio"), EmailAddress(ErrorMessage = "Email inválido"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria"), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres"), MaxLength(100)]
    public string Contrasena { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El domicilio es obligatorio")]
    public DomicilioRequest Domicilio { get; set; } = new();
}

public class RegistroIndividuoRequest : RegistroBaseRequest
{
    [Required, MaxLength(200)]
    public string Nombres { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    public DateOnly? FechaNacimiento { get; set; }

    [MaxLength(30)]
    public string? Genero { get; set; }

    public VehiculoRequest? Vehiculo { get; set; }
}

public class RegistroOrganizacionRequest : RegistroBaseRequest
{
    [Required, MaxLength(250)]
    public string RazonSocial { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{2}-?\d{8}-?\d$", ErrorMessage = "CUIT inválido (formato 20-12345678-9)")]
    public string Cuit { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Area { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [Range(0, 1_000_000)]
    public int? CantidadMiembros { get; set; }
}

public record UsuarioSesionDto(
    long IdUsuario,
    string Email,
    long IdRol,
    string Rol,
    string NombreMostrar,
    string? TipoPerfil,
    IReadOnlyList<string> Permisos);

public record AuthResponse(string Token, DateTime ExpiraEn, UsuarioSesionDto Usuario);
