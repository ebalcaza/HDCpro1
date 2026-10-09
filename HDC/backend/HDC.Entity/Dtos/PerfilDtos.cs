using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class IndividuoDto
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
}

public class OrganizacionDto
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

public class VehiculoRequest
{
    [Required, RegularExpression("^(Auto|Motocicleta|Camioneta|Camión)$", ErrorMessage = "Tipo de vehículo inválido")]
    public string TipoVehiculo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    [Required, RegularExpression(@"^([A-Za-z]{3}\s?\d{3}|[A-Za-z]{2}\s?\d{3}\s?[A-Za-z]{2}|[A-Za-z]\d{3}[A-Za-z]{3}|\d{3}[A-Za-z]{3})$",
        ErrorMessage = "Patente inválida (ej: ABC123 o AB123CD)")]
    public string Patente { get; set; } = string.Empty;

    [Required, RegularExpression("^(Nafta|Gasoil|GNC)$", ErrorMessage = "Tipo de combustible inválido")]
    public string TipoCombustible { get; set; } = string.Empty;

    [Required, RegularExpression("^(Personal|Institucional)$", ErrorMessage = "Tipo de uso inválido")]
    public string TipoUso { get; set; } = string.Empty;
}

public record VehiculoDto(
    long IdVehiculo,
    string TipoVehiculo,
    string Marca,
    string Modelo,
    string Patente,
    string TipoCombustible,
    string TipoUso);

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
    IReadOnlyList<VehiculoDto> Vehiculos,
    int CantidadDomicilios,
    decimal TotalKgCo2);

public class ActualizarPerfilRequest
{
    [MaxLength(30)]
    public string? Telefono { get; set; }

    public IndividuoDto? Individuo { get; set; }
    public OrganizacionDto? Organizacion { get; set; }
}

public class CambiarContrasenaRequest
{
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres"), MaxLength(100)]
    public string ContrasenaNueva { get; set; } = string.Empty;
}
