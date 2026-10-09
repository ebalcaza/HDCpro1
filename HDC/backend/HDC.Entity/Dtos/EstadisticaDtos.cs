using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public record SerieItemDto(long Id, string Nombre, decimal KgCo2, decimal Porcentaje, string? Grupo = null, decimal? Consumo = null, string? Unidad = null);

public record PuntoEvolucionDto(string Periodo, decimal KgCo2, IReadOnlyDictionary<string, decimal> PorAlcance);

public record PeriodoValorDto(string Periodo, decimal KgCo2);

public record LecturaRapidaDto(
    decimal PromedioMensualKgCo2,
    PeriodoValorDto? MesMaximo,
    PeriodoValorDto? MesMinimo,
    int CantidadRegistros,
    int CantidadPeriodos,
    int CantidadDomicilios,
    int CantidadColaboradores);

public record DashboardDto(
    decimal TotalKgCo2,
    decimal MesActualKgCo2,
    decimal MesAnteriorKgCo2,
    decimal? VariacionMensualPct,
    SerieItemDto? AlcancePrincipal,
    IReadOnlyList<SerieItemDto> PorAlcance,
    IReadOnlyList<SerieItemDto> PorTipoEmision,
    IReadOnlyList<SerieItemDto> TopFuentes,
    IReadOnlyList<SerieItemDto> PorDomicilio,
    IReadOnlyList<SerieItemDto> PorColaborador,
    IReadOnlyList<PuntoEvolucionDto> Evolucion,
    LecturaRapidaDto LecturaRapida);

public record ReporteResumenDto(
    decimal TotalKgCo2,
    int CantidadRegistros,
    int CantidadDetalles,
    int CantidadDomicilios,
    int CantidadUsuarios,
    string? PeriodoDesde,
    string? PeriodoHasta,
    decimal PromedioMensualKgCo2);

public record ReporteFilaDto(
    long IdRegistro,
    string Periodo,
    string Domicilio,
    string Titular,
    string Rol,
    string Provincia,
    string Distrito,
    string Ciudad,
    string Alcance,
    string TipoEmision,
    string Fuente,
    decimal Consumo,
    string UnidadMedida,
    decimal ValorFactorEmision,
    decimal KgCo2);

public record ReporteResultadoDto(
    DateTime FechaGeneracion,
    bool EsGlobal,
    IReadOnlyList<string> FiltrosAplicados,
    ReporteResumenDto Resumen,
    IReadOnlyList<PeriodoValorDto> PorPeriodo,
    IReadOnlyList<SerieItemDto> PorAlcance,
    IReadOnlyList<SerieItemDto> PorTipoEmision,
    IReadOnlyList<SerieItemDto> PorFuente,
    IReadOnlyList<SerieItemDto> PorDomicilio,
    IReadOnlyList<SerieItemDto> PorRol,
    IReadOnlyList<SerieItemDto> PorProvincia,
    IReadOnlyList<SerieItemDto> PorDistrito,
    IReadOnlyList<SerieItemDto> PorUsuario,
    IReadOnlyList<ReporteFilaDto> Filas);

public class GuardarReporteRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public bool EsGlobal { get; set; }

    [Required]
    public FiltroEmisiones Filtros { get; set; } = new();
}

public record ReporteGuardadoDto(long IdReporte, string Nombre, string? Descripcion, bool EsGlobal, FiltroEmisiones Filtros, DateTime FechaCreacion);

public record UsuarioListaDto(
    long IdUsuario,
    string Email,
    long IdRol,
    string Rol,
    string NombreMostrar,
    string? TipoPerfil,
    string? Telefono,
    DateTime FechaAlta,
    DateTime? FechaBaja,
    bool Activo,
    int CantidadDomicilios,
    decimal TotalKgCo2);

public record UsuarioDetalleDto(PerfilDto Perfil, IReadOnlyList<DomicilioDto> Domicilios, int CantidadRegistros);

public class CambiarRolRequest
{
    [Range(1, long.MaxValue)]
    public long IdRol { get; set; }
}

public class CambiarEstadoRequest
{
    public bool Activo { get; set; }
}

public record MensajeDto(string Mensaje);
