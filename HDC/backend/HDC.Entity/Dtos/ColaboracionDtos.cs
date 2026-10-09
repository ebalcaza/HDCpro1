using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class InvitarColaboradorRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El domicilio es obligatorio")]
    public long IdDomicilio { get; set; }

    [Required(ErrorMessage = "El email del colaborador es obligatorio"), EmailAddress(ErrorMessage = "Email inválido")]
    public string EmailColaborador { get; set; } = string.Empty;

    [Required(ErrorMessage = "El período desde es obligatorio")]
    public DateOnly PeriodoDesde { get; set; }

    public DateOnly? PeriodoHasta { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un tipo de emisión")]
    public List<long> IdsTiposEmision { get; set; } = new();

    [MaxLength(500)]
    public string? Mensaje { get; set; }
}

public class ActualizarColaboracionRequest
{
    [Required]
    public DateOnly PeriodoDesde { get; set; }

    public DateOnly? PeriodoHasta { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un tipo de emisión")]
    public List<long> IdsTiposEmision { get; set; } = new();
}

public record TipoEmisionResumenDto(long IdTipoEmision, string Nombre, long IdAlcance, string Alcance);

public record ColaboracionDto(
    long IdColaboracion,
    long IdDomicilio,
    string Domicilio,
    long IdTitular,
    string Titular,
    string EmailTitular,
    long IdColaborador,
    string Colaborador,
    string EmailColaborador,
    DateOnly PeriodoDesde,
    DateOnly? PeriodoHasta,
    string Estado,
    string? Mensaje,
    DateTime FechaInvitacion,
    DateTime? FechaRespuesta,
    bool Vigente,
    IReadOnlyList<TipoEmisionResumenDto> TiposEmision,
    int CantidadRegistros,
    decimal TotalKgCo2Aportado);
