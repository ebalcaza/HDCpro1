using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class DomicilioRequest
{
    [Required(ErrorMessage = "La calle es obligatoria"), MaxLength(200)]
    public string Calle { get; set; } = string.Empty;

    [Range(0, 999_999, ErrorMessage = "La altura debe ser un número válido")]
    public int Numero { get; set; }

    [MaxLength(20)] public string? Departamento { get; set; }
    [MaxLength(20)] public string? Piso { get; set; }
    [MaxLength(20)] public string? Block { get; set; }
    [MaxLength(20)] public string? Manzana { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "La ciudad es obligatoria")]
    public long IdCiudad { get; set; }

    [Required(ErrorMessage = "El código postal es obligatorio"), MaxLength(15)]
    public string CodigoPostal { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    public List<long> IdsFuentes { get; set; } = new();
}

public record FuenteResumenDto(long IdFuenteEmision, string Nombre, string UnidadMedida, long IdTipoEmision, string TipoEmision, long IdAlcance, string Alcance);

public record DomicilioDto(
    long IdDomicilio,
    string Calle,
    int Numero,
    string? Departamento,
    string? Piso,
    string? Block,
    string? Manzana,
    string CodigoPostal,
    string? Telefono,
    long IdCiudad,
    string Ciudad,
    long IdDistrito,
    string Distrito,
    long IdProvincia,
    string Provincia,
    DateTime FechaAlta,
    DateTime? FechaBaja,
    decimal TotalKgCo2,
    string Descripcion,
    IReadOnlyList<FuenteResumenDto> Fuentes,
    int CantidadRegistros,
    int CantidadColaboradores);
