using System.ComponentModel.DataAnnotations;

namespace HDC.Entity.Dtos;

public class DetalleRegistroRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La fuente de emisión es obligatoria")]
    public long IdFuenteEmision { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999", ErrorMessage = "El consumo debe ser mayor a 0",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Consumo { get; set; }
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
    DateTime FechaUltimaModificacion);

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
    decimal KgCo2);

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
    IReadOnlyList<DetalleRegistroDto> Detalles);

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
