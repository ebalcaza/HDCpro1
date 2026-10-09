using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public record ProvinciaDto(long IdProvincia, string Nombre);
public record DistritoDto(long IdDistrito, long IdProvincia, string Nombre);
public record CiudadDto(long IdCiudad, long IdDistrito, string Nombre);

public record AlcanceDto(long IdAlcance, string Nombre, string? Descripcion, int CantidadTipos);

public class AlcanceRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

public record UnidadMedidaDto(long IdUnidadMedida, string Nombre, int CantidadFuentes);

public class UnidadMedidaRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;
}

public record TipoEmisionDto(long IdTipoEmision, long IdAlcance, string Alcance, string Nombre, int CantidadFuentes);

public class TipoEmisionRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El alcance es obligatorio")]
    public long IdAlcance { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
}

public record FuenteEmisionDto(
    long IdFuenteEmision,
    string Nombre,
    decimal ValorFactorEmision,
    long IdUnidadMedida,
    string UnidadMedida,
    long IdTipoEmision,
    string TipoEmision,
    long IdAlcance,
    string Alcance,
    DateTime FechaAlta,
    DateTime? FechaBaja,
    DateTime? FechaModificacion,
    bool Activa);

public class FuenteEmisionRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, 999_999_999, ErrorMessage = "El factor de emisión debe ser mayor o igual a 0")]
    public decimal ValorFactorEmision { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "La unidad de medida es obligatoria")]
    public long IdUnidadMedida { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "El tipo de emisión es obligatorio")]
    public long IdTipoEmision { get; set; }
}

public record PermisoDto(long IdPermiso, string Nombre, string? Descripcion, IReadOnlyList<string> Roles);

public class PermisoRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(250),
     RegularExpression("^[A-Z0-9_]+$", ErrorMessage = "Usá mayúsculas, números y guiones bajos (ej: VER_REPORTES)")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

public record RolDto(
    long IdRol,
    string Nombre,
    string? Descripcion,
    bool EsSistema,
    int CantidadUsuarios,
    IReadOnlyList<PermisoDto> Permisos);

public class RolRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public List<long> IdsPermisos { get; set; } = new();
}
