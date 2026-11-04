using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class DetalleRegistroRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La fuente de emisión es obligatoria")]
    public long IdFuenteEmision { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999", ErrorMessage = "El consumo debe ser mayor a 0",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Consumo { get; set; }

    /// <summary>Opcional, solo para vehículos: kilómetros recorridos.</summary>
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Los kilómetros deben ser mayores a 0",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Kilometros { get; set; }

    /// <summary>Obligatoria en viajes institucionales.</summary>
    public DateOnly? FechaViaje { get; set; }
}

public class RegistroRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El domicilio es obligatorio")]
    public long IdDomicilio { get; set; }

    /// <summary>Cualquier día del mes; se normaliza al primer día.</summary>
    [Required(ErrorMessage = "El período es obligatorio")]
    public DateOnly Periodo { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Convocatoria desde la que se carga (botón Cargar de "Donde colaboro"). Opcional.</summary>
    public long? IdConvocatoria { get; set; }

    [MinLength(1, ErrorMessage = "Agregá al menos un consumo")]
    public List<DetalleRegistroRequest> Detalles { get; set; } = new();
}

public record RegistroListaDto(
    long IdRegistro,
    long IdDomicilio,
    string Domicilio,
    DateOnly Periodo,
    decimal TotalKgCo2,
    int CantidadDetalles,
    long IdUsuarioCarga,
    string UsuarioCarga,
    bool EsColaboracion,
    bool PuedeEditar,
    IReadOnlyList<string> Alcances,
    DateTime FechaCreacion,
    DateTime FechaUltimaModificacion,
    long? IdCargaTraslado);

public record DetalleRegistroDto(
    long IdDetalle,
    long IdFuenteEmision,
    string Fuente,
    long IdTipoEmision,
    string TipoEmision,
    long IdAlcance,
    string Alcance,
    string UnidadMedida,
    decimal Consumo,
    decimal ValorFactorEmision,
    decimal KgCo2,
    string ModoCarga,
    decimal? Kilometros,
    DateOnly? FechaViaje);

/// <summary>Datos de la carga de traslado de un registro (se muestran en su detalle).</summary>
public record TrasladoResumenDto(
    long IdCargaTraslado,
    string Periodicidad,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    int MesesPeriodo,
    decimal Semanas,
    int CantidadPersonas,
    string? Rol,
    string? Convocatoria,
    IReadOnlyList<MedioResumenDto> Medios);

public record MedioResumenDto(string Fuente, decimal DistanciaKm, decimal DiasSemana);

public record RegistroDetalleDto(
    long IdRegistro,
    long IdDomicilio,
    string Domicilio,
    string Ciudad,
    string Provincia,
    string Titular,
    DateOnly Periodo,
    string? Observaciones,
    decimal TotalKgCo2,
    long IdUsuarioCarga,
    string UsuarioCarga,
    bool EsColaboracion,
    bool PuedeEditar,
    DateTime FechaCreacion,
    DateTime FechaUltimaModificacion,
    IReadOnlyList<DetalleRegistroDto> Detalles,
    TrasladoResumenDto? Traslado);

/// <summary>Traslado declarado una vez, para un mes, un semestre o un año (v2.6).</summary>
public class TrasladoRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "El domicilio es obligatorio")]
    public long IdDomicilio { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Elegí el medio de transporte")]
    public long? IdConvocatoria { get; set; }

    public long? IdConvocatoriaRol { get; set; }

    /// <summary>En convocatorias: PERIODO (todo de una vez) o MENSUAL. Fuera de ellas: MENSUAL, SEMESTRAL o ANUAL.</summary>
    [Required, RegularExpression("^(MENSUAL|SEMESTRAL|ANUAL|PERIODO)$", ErrorMessage = "Período inválido")]
    public string Periodicidad { get; set; } = string.Empty;

    /// <summary>Primer mes del período, o el mes elegido. Con PERIODO se ignora: vale el de la convocatoria.</summary>
    [Required(ErrorMessage = "Elegí desde qué mes")]
    public DateOnly PeriodoDesde { get; set; }

    /// <summary>Semanas del período. En una convocatoria se ignora: las define el organizador.</summary>
    [Range(typeof(decimal), "0.25", "999", ErrorMessage = "Las semanas deben ser mayores a 0",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Semanas { get; set; }

    /// <summary>Solo lo usa el titular, para declarar un grupo. Un colaborador siempre cuenta como 1.</summary>
    [Range(1, 1000000, ErrorMessage = "La cantidad de personas debe ser al menos 1")]
    public int CantidadPersonas { get; set; } = 1;

    [MinLength(1, ErrorMessage = "Agregá al menos un medio de transporte")]
    public List<MedioTrasladoRequest> Medios { get; set; } = new();
}

public class MedioTrasladoRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "Elegí el medio de transporte")]
    public long IdFuenteEmision { get; set; }

    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "La distancia debe ser mayor a 0",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal DistanciaKm { get; set; }

    [Range(typeof(decimal), "1", "7", ErrorMessage = "Los días por semana deben estar entre 1 y 7",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal DiasSemana { get; set; }
}

public record MedioTrasladoDto(long IdFuenteEmision, string Fuente, decimal DistanciaKm, decimal DiasSemana, decimal ValorFactorEmision, decimal KmTotales, decimal KgCo2);

public record TrasladoDto(
    long IdCargaTraslado,
    long IdDomicilio,
    string Domicilio,
    long? IdConvocatoria,
    string? Convocatoria,
    long? IdConvocatoriaRol,
    string? Rol,
    string Periodicidad,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    int MesesCargados,
    int MesesPeriodo,
    decimal Semanas,
    int CantidadPersonas,
    IReadOnlyList<MedioTrasladoDto> Medios,
    decimal KmTotales,
    decimal KgCo2Total,
    long IdPrimerRegistro,
    bool EsTitular);

public record RolOpcionDto(long IdConvocatoriaRol, string Nombre, string Convocatoria);

/// <summary>Convocatoria que fija el período de un traslado, con los meses que la persona ya cargó.</summary>
public record ConvocatoriaTrasladoDto(
    long IdConvocatoria,
    string Nombre,
    string? Descripcion,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    decimal SemanasTraslado,
    int MesesPeriodo,
    IReadOnlyList<RolOpcionDto> Roles,
    IReadOnlyList<DateOnly> MesesCargados,
    // Rol que la persona ya eligió en esta convocatoria: el formulario lo propone (v2.9)
    long? IdRolElegido);

/// <summary>Una carga de traslado que la persona ya guardó: lo que ya tiene cargado se cambia editándola (v2.11).</summary>
public record CargaPropiaDto(long IdCargaTraslado, long? IdConvocatoria, DateOnly PeriodoDesde, DateOnly PeriodoHasta);

/// <summary>Datos para el formulario de traslado. DesdeMinimo null significa sin límite inferior.</summary>
public record OpcionesTrasladoDto(bool EsTitular, IReadOnlyList<RolOpcionDto> Roles, DateOnly? DesdeMinimo, DateOnly HastaMaximo, IReadOnlyList<ConvocatoriaTrasladoDto> Convocatorias, IReadOnlyList<CargaPropiaDto> CargasPropias);

/// <summary>Fila "aplanada" de emisión (un detalle con todo su contexto) usada para estadísticas y reportes.</summary>
public class EmisionPlana
{
    public long IdRegistro { get; set; }
    public DateOnly Periodo { get; set; }
    public long IdDomicilio { get; set; }
    public string Calle { get; set; } = string.Empty;
    public int Numero { get; set; }
    public long IdUsuarioTitular { get; set; }
    public string Titular { get; set; } = string.Empty;
    public long IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
    public long IdUsuarioCarga { get; set; }
    public string UsuarioCarga { get; set; } = string.Empty;
    public long IdProvincia { get; set; }
    public string Provincia { get; set; } = string.Empty;
    public long IdDistrito { get; set; }
    public string Distrito { get; set; } = string.Empty;
    public long IdCiudad { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public long IdAlcance { get; set; }
    public string Alcance { get; set; } = string.Empty;
    public long IdTipoEmision { get; set; }
    public string TipoEmision { get; set; } = string.Empty;
    public long IdFuenteEmision { get; set; }
    public string Fuente { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal Consumo { get; set; }
    public decimal ValorFactorEmision { get; set; }
    public decimal KgCo2 { get; set; }

    public string DomicilioTexto => $"{Calle} {Numero}";
}

/// <summary>Criterios de filtrado sobre las emisiones. Las listas vacías no filtran.</summary>
/// <summary>kg CO₂ sumados por la base para un mes y un alcance.</summary>
public record SumaPeriodoAlcance(DateOnly Periodo, long IdAlcance, string Alcance, decimal KgCo2);

/// <summary>kg CO₂ (y consumo, si aplica) sumados por la base para un grupo: tipo, fuente, domicilio o colaborador.</summary>
public record SumaGrupo(long Id, string Nombre, string? Grupo, decimal KgCo2, decimal Consumo, string? Unidad);

/// <summary>Resultado de las consultas agrupadas del Dashboard (todo viene ya sumado desde SQL).</summary>
public record AgregadosEmisiones(
    IReadOnlyList<SumaPeriodoAlcance> PorPeriodoAlcance,
    IReadOnlyList<SumaGrupo> PorTipo,
    IReadOnlyList<SumaGrupo> PorFuente,
    IReadOnlyList<SumaGrupo> PorDomicilio,
    IReadOnlyList<SumaGrupo> PorColaborador,
    int CantidadRegistros,
    int CantidadDomicilios);

public class FiltroEmisiones
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<long> IdsDomicilios { get; set; } = new();
    public List<long> IdsAlcances { get; set; } = new();
    public List<long> IdsTiposEmision { get; set; } = new();
    public List<long> IdsFuentes { get; set; } = new();

    public long? IdProvincia { get; set; }
    public long? IdDistrito { get; set; }
    public long? IdCiudad { get; set; }

    // Solo para reportes globales (administrador)
    public List<long> IdsRoles { get; set; } = new();

    /// <summary>Titulares de los domicilios. En reportes de usuario se fuerza al usuario actual.</summary>
    public List<long> IdsUsuarios { get; set; } = new();
}
