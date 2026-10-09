using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Comun;

public static class Mapeos
{
    public static DateOnly InicioDeMes(DateOnly fecha) => new(fecha.Year, fecha.Month, 1);

    public static DateOnly MesActual() => InicioDeMes(DateOnly.FromDateTime(DateTime.Now));

    public static string FormatoPeriodo(DateOnly p) => $"{p.Year:D4}-{p.Month:D2}";

    public static string NombreMostrar(Usuario u) =>
        u.Individuo is { } i ? $"{i.Nombres} {i.Apellidos}"
        : u.Organizacion?.RazonSocial ?? u.Email;

    public static string? TipoPerfil(Usuario u) =>
        u.Individuo != null ? Entity.Dtos.TipoPerfil.Individuo
        : u.Organizacion != null ? Entity.Dtos.TipoPerfil.Organizacion
        : null;

    public static string DescripcionCorta(Domicilio d)
    {
        var partes = new List<string> { $"{d.Calle} {d.Numero}" };
        if (!string.IsNullOrWhiteSpace(d.Piso)) partes.Add($"Piso {d.Piso}");
        if (!string.IsNullOrWhiteSpace(d.Departamento)) partes.Add($"Dto. {d.Departamento}");
        if (!string.IsNullOrWhiteSpace(d.Block)) partes.Add($"Block {d.Block}");
        if (!string.IsNullOrWhiteSpace(d.Manzana)) partes.Add($"Mz. {d.Manzana}");
        return string.Join(", ", partes);
    }

    public static string DescripcionCompleta(Domicilio d) =>
        d.Ciudad is null ? DescripcionCorta(d) : $"{DescripcionCorta(d)} - {d.Ciudad.Nombre}";

    public static FuenteResumenDto ToResumen(FuenteEmision f) => new(
        f.IdFuenteEmision, f.Nombre, f.UnidadMedida.Nombre,
        f.IdTipoEmision, f.TipoEmision.Nombre, f.TipoEmision.IdAlcance, f.TipoEmision.Alcance.Nombre);

    public static FuenteEmisionDto ToDto(FuenteEmision f) => new(
        f.IdFuenteEmision, f.Nombre, f.ValorFactorEmision,
        f.IdUnidadMedida, f.UnidadMedida.Nombre,
        f.IdTipoEmision, f.TipoEmision.Nombre,
        f.TipoEmision.IdAlcance, f.TipoEmision.Alcance.Nombre,
        f.FechaAlta, f.FechaBaja, f.FechaModificacion, f.FechaBaja == null);

    public static DomicilioDto ToDto(Domicilio d, int registros, int colaboradores) => new(
        d.IdDomicilio, d.Calle, d.Numero, d.Departamento, d.Piso, d.Block, d.Manzana,
        d.CodigoPostal, d.Telefono,
        d.IdCiudad, d.Ciudad.Nombre,
        d.Ciudad.IdDistrito, d.Ciudad.Distrito.Nombre,
        d.Ciudad.Distrito.IdProvincia, d.Ciudad.Distrito.Provincia.Nombre,
        d.FechaAlta, d.FechaBaja, d.TotalKgCo2,
        DescripcionCompleta(d),
        d.Fuentes.Select(f => ToResumen(f.FuenteEmision)).OrderBy(f => f.Nombre).ToList(),
        registros, colaboradores);

    public static VehiculoDto ToDto(Vehiculo v) =>
        new(v.IdVehiculo, v.TipoVehiculo, v.Marca, v.Modelo, v.Patente, v.TipoCombustible, v.TipoUso);

    public static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static string NormalizarCuit(string cuit)
    {
        var digitos = new string(cuit.Where(char.IsDigit).ToArray());
        return digitos.Length == 11 ? $"{digitos[..2]}-{digitos[2..10]}-{digitos[10]}" : cuit.Trim();
    }

    public static string NormalizarPatente(string patente) =>
        new string(patente.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
