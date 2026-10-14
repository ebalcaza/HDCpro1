using System.ComponentModel.DataAnnotations;
using HDC.Entity.Validaciones;

namespace HDC.Entity.Dtos;

public class IndividuoDto
{
    [Required, NombrePersona(200)]
    public string Nombres { get; set; } = string.Empty;

    [Required, NombrePersona(250)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    public DateOnly? FechaNacimiento { get; set; }

    [MaxLength(30)]
    public string? Genero { get; set; }
}

public class OrganizacionDto
{
    [Required, MaxLength(250)]
    public string RazonSocial { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{2}-?\d{8}-?\d$", ErrorMessage = "CUIT inválido (formato 20-12345678-9)")]
    public string Cuit { get; set; } = string.Empty;

    [Required, MaxLength(100), AreaOrganizacion]
    public string Area { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [Range(0, 1_000_000)]
    public int? CantidadMiembros { get; set; }
}

public record PerfilDto(
    long IdUsuario,
    string Email,
    string? Telefono,
    long IdRol,
    string Rol,
    string NombreMostrar,
    string? TipoPerfil,
    DateTime FechaAlta,
    DateTime? FechaBaja,
    IndividuoDto? Individuo,
    OrganizacionDto? Organizacion,
    int CantidadDomicilios,
    decimal TotalKgCo2);

public class ActualizarPerfilRequest
{
    [MaxLength(30), TelefonoArgentino]
    public string? Telefono { get; set; }

    public IndividuoDto? Individuo { get; set; }
    public OrganizacionDto? Organizacion { get; set; }
}

public class CambiarContrasenaRequest
{
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres"), MaxLength(100), Contrasena]
    public string ContrasenaNueva { get; set; } = string.Empty;
}
