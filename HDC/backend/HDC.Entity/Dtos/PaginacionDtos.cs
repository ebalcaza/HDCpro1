namespace HDC.Entity.Dtos;

/// <summary>
/// Parámetros comunes de una consulta paginada en el servidor.
/// Llegan por query string, ej: ?pagina=2&amp;tamanio=20&amp;orden=periodo&amp;asc=false&amp;busqueda=gas
/// </summary>
public class ConsultaPagina
{
    /// <summary>Tamaños permitidos: los mismos que ofrece el selector "Mostrar" del frontend.</summary>
    public static readonly int[] TamaniosPermitidos = [10, 20, 50, 100];

    private int _pagina = 1;
    private int _tamanio = 10;

    /// <summary>Número de página, empieza en 1. Valores menores se corrigen a 1.</summary>
    public int Pagina { get => _pagina; set => _pagina = Math.Max(1, value); }

    /// <summary>Registros por página. Si no es un tamaño permitido se usa 10 (evita pedir miles de filas de una vez).</summary>
    public int Tamanio { get => _tamanio; set => _tamanio = TamaniosPermitidos.Contains(value) ? value : 10; }

    /// <summary>Clave de la columna por la que se ordena (la misma "clave" que usa la tabla del frontend).</summary>
    public string? Orden { get; set; }

    /// <summary>true = ascendente, false = descendente.</summary>
    public bool Asc { get; set; } = true;

    /// <summary>Texto libre del buscador de la tabla.</summary>
    public string? Busqueda { get; set; }

    /// <summary>Cuántas filas hay que saltear para llegar a la página pedida.</summary>
    public int Saltear => (Pagina - 1) * Tamanio;
}

/// <summary>Una página de resultados más el total de filas que cumplen los filtros (para armar la paginación).</summary>
public record PaginaDto<T>(IReadOnlyList<T> Items, int Total);

/// <summary>Filtros de "Mis emisiones" además de la paginación.</summary>
public class FiltroRegistros : ConsultaPagina
{
    public long? IdDomicilio { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
}

/// <summary>Página de registros + suma de kg CO₂ de TODOS los registros filtrados (no solo de la página visible).</summary>
public record PaginaRegistrosDto(IReadOnlyList<RegistroListaDto> Items, int Total, decimal TotalKgCo2);

/// <summary>Domicilio que aparece en el filtro de "Mis emisiones" (propios y de colaboraciones).</summary>
public record OpcionDomicilioDto(long IdDomicilio, string Descripcion);

/// <summary>Filtros del listado de usuarios del administrador.</summary>
public class FiltroUsuarios : ConsultaPagina
{
    public long? IdRol { get; set; }

    /// <summary>"activos", "inactivos" o vacío para todos.</summary>
    public string? Estado { get; set; }
}

/// <summary>Números generales de usuarios (KPI del panel admin) y los tipos para el filtro del listado.</summary>
public record ResumenUsuariosDto(int Total, int Activos, IReadOnlyList<OpcionRolDto> Roles);

public record OpcionRolDto(long IdRol, string Nombre);
