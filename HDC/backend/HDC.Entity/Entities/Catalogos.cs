namespace HDC.Entity.Entities;

public class UnidadMedida
{
    public long IdUnidadMedida { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class Alcance
{
    public long IdAlcance { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<TipoEmision> TiposEmision { get; set; } = new List<TipoEmision>();
}

/// <summary>Qué datos pide el sistema para las fuentes de un tipo de emisión (v2.6).</summary>
public static class ModoCarga
{
    /// <summary>Solo la cantidad consumida.</summary>
    public const string Consumo = "CONSUMO";
    /// <summary>Litros de combustible y, opcionalmente, kilómetros recorridos.</summary>
    public const string Vehiculo = "VEHICULO";
    /// <summary>Medio de transporte, distancia, días por semana y período (ver TrasladoServicio).</summary>
    public const string Traslado = "TRASLADO";
    /// <summary>Kilómetros y fecha: se guarda como dato, con factor 0.</summary>
    public const string Viaje = "VIAJE";
}

public class TipoEmision
{
    public long IdTipoEmision { get; set; }
    public long IdAlcance { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ModoCarga { get; set; } = HDC.Entity.Entities.ModoCarga.Consumo;

    public Alcance Alcance { get; set; } = null!;
}

public class FuenteEmision
{
    public long IdFuenteEmision { get; set; }
    public long IdUnidadMedida { get; set; }
    public long IdTipoEmision { get; set; }
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Texto de ayuda para el usuario: qué es la fuente y dónde encontrar el dato del consumo.</summary>
    public string? Descripcion { get; set; }
    public decimal ValorFactorEmision { get; set; }
    /// <summary>
    /// Viajes institucionales (v2.9): fuente de la que sale el factor, por ejemplo "Gasoil para vehículos".
    /// El factor por km se calcula como factor de la fuente base × ConsumoPorKm. Null = factor cargado a mano.
    /// </summary>
    public long? IdFuenteBase { get; set; }
    /// <summary>Litros o kWh por km que convierten la distancia en consumo de la fuente base.</summary>
    public decimal? ConsumoPorKm { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public UnidadMedida UnidadMedida { get; set; } = null!;
    public TipoEmision TipoEmision { get; set; } = null!;
}
