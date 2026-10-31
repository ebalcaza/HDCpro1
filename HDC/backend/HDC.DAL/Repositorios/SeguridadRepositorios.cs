using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

public class UsuarioRepositorio(HdcDbContext ctx) : Repositorio<Usuario>(ctx)
{
    private IQueryable<Usuario> ConPerfil() => Ctx.Usuarios
        .Include(u => u.Rol).ThenInclude(r => r.RolPermisos).ThenInclude(rp => rp.Permiso)
        .Include(u => u.Individuo)
        .Include(u => u.Organizacion);

    public Task<Usuario?> ObtenerPorEmailAsync(string email) =>
        ConPerfil().FirstOrDefaultAsync(u => u.Email == email);

    public Task<Usuario?> ObtenerCompletoAsync(long idUsuario) =>
        ConPerfil().FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

    /// <summary>
    /// Listado de usuarios del admin, paginado en SQL.
    /// En lugar de traer cada usuario con TODOS sus domicilios (Include) para contarlos y sumarlos en memoria,
    /// se proyecta directo al DTO: la base calcula COUNT y SUM y devuelve una fila liviana por usuario.
    /// </summary>
    public async Task<PaginaDto<UsuarioListaDto>> PaginarAsync(FiltroUsuarios f)
    {
        var q = Ctx.Usuarios.AsNoTracking();

        // Filtros de la barra de la tabla
        if (f.IdRol is long idRol) q = q.Where(u => u.IdRol == idRol);
        if (f.Estado == "activos") q = q.Where(u => u.FechaBaja == null);
        else if (f.Estado == "inactivos") q = q.Where(u => u.FechaBaja != null);

        // Buscador: email, nombre y apellido o razón social (ILIKE: sin distinguir mayúsculas)
        if (!string.IsNullOrWhiteSpace(f.Busqueda))
        {
            var t = ExtensionesConsulta.PatronContiene(f.Busqueda.Trim());
            q = q.Where(u => EF.Functions.ILike(u.Email, t)
                || (u.Individuo != null && (EF.Functions.ILike(u.Individuo.Nombres, t) || EF.Functions.ILike(u.Individuo.Apellidos, t)))
                || (u.Organizacion != null && EF.Functions.ILike(u.Organizacion.RazonSocial, t)));
        }

        var total = await q.CountAsync();

        // Orden por la columna elegida (se ordena sobre la entidad, antes de proyectar, para que EF lo traduzca a SQL)
        var ordenada = (f.Orden?.ToLowerInvariant()) switch
        {
            "nombremostrar" => q.OrdenarPor(u => u.Individuo != null ? u.Individuo.Nombres
                : u.Organizacion != null ? u.Organizacion.RazonSocial : u.Email, f.Asc),
            "rol" => q.OrdenarPor(u => u.Rol.Nombre, f.Asc),
            "cantidaddomicilios" => q.OrdenarPor(u => u.Domicilios.Count(d => d.FechaBaja == null), f.Asc),
            "totalkgco2" => q.OrdenarPor(u => u.Domicilios.Sum(d => (decimal?)d.TotalKgCo2) ?? 0m, f.Asc),
            "fechaalta" => q.OrdenarPor(u => u.FechaAlta, f.Asc),
            // Por defecto: los últimos registrados primero
            _ => q.OrderByDescending(u => u.FechaAlta),
        };

        var items = await ordenada.ThenBy(u => u.IdUsuario)
            .Paginar(f.Saltear, f.Tamanio)
            .Select(u => new UsuarioListaDto(
                u.IdUsuario, u.Email, u.IdRol, u.Rol.Nombre,
                // Mismo criterio que Mapeos.NombreMostrar, escrito para que se traduzca a SQL
                u.Individuo != null ? u.Individuo.Nombres + " " + u.Individuo.Apellidos
                    : u.Organizacion != null ? u.Organizacion.RazonSocial : u.Email,
                u.Individuo != null ? TipoPerfil.Individuo : u.Organizacion != null ? TipoPerfil.Organizacion : null,
                u.Telefono, u.FechaAlta, u.FechaBaja, u.FechaBaja == null,
                u.Domicilios.Count(d => d.FechaBaja == null),
                u.Domicilios.Sum(d => (decimal?)d.TotalKgCo2) ?? 0m))
            .ToListAsync();

        return new PaginaDto<UsuarioListaDto>(items, total);
    }

    /// <summary>Total de usuarios y cuántos están activos, con dos COUNT en la base (sin traer filas).</summary>
    public async Task<(int Total, int Activos)> ContarAsync() =>
        (await Ctx.Usuarios.CountAsync(), await Ctx.Usuarios.CountAsync(u => u.FechaBaja == null));

    public Task<bool> ExisteEmailAsync(string email, long? excluirId = null) =>
        Ctx.Usuarios.AnyAsync(u => u.Email == email && (excluirId == null || u.IdUsuario != excluirId));

    public Task<bool> ExisteDniAsync(string dni, long? excluirId = null) =>
        Ctx.Individuos.AnyAsync(i => i.Dni == dni && (excluirId == null || i.IdUsuario != excluirId));

    public Task<bool> ExisteCuitAsync(string cuit, long? excluirId = null) =>
        Ctx.Organizaciones.AnyAsync(o => o.Cuit == cuit && (excluirId == null || o.IdUsuario != excluirId));

    public Task<bool> HayAdministradorAsync(long idRolAdmin) =>
        Ctx.Usuarios.AnyAsync(u => u.IdRol == idRolAdmin);

    public Task<int> ContarRegistrosAsync(long idUsuario) =>
        Ctx.Registros.CountAsync(r => r.Domicilio.IdUsuario == idUsuario || r.IdUsuarioCarga == idUsuario);
}

public class RolRepositorio(HdcDbContext ctx) : Repositorio<Rol>(ctx)
{
    /// <summary>Solo id y nombre de cada tipo de usuario (para combos de filtro), sin cargar permisos ni usuarios.</summary>
    public Task<List<OpcionRolDto>> ListarOpcionesAsync() => Ctx.Roles.AsNoTracking()
        .OrderBy(r => r.Nombre)
        .Select(r => new OpcionRolDto(r.IdRol, r.Nombre))
        .ToListAsync();

    public Task<List<Rol>> ListarAsync() => Ctx.Roles.AsNoTracking()
        .Include(r => r.RolPermisos).ThenInclude(rp => rp.Permiso)
        .Include(r => r.Usuarios)
        .OrderBy(r => r.IdRol)
        .ToListAsync();

    public Task<Rol?> ObtenerConPermisosAsync(long idRol) => Ctx.Roles
        .Include(r => r.RolPermisos).ThenInclude(rp => rp.Permiso)
        .Include(r => r.Usuarios)
        .FirstOrDefaultAsync(r => r.IdRol == idRol);

    public Task<bool> ExisteNombreAsync(string nombre, long? excluirId = null) =>
        Ctx.Roles.AnyAsync(r => r.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || r.IdRol != excluirId));

    public Task<bool> TieneUsuariosAsync(long idRol) => Ctx.Usuarios.AnyAsync(u => u.IdRol == idRol);
}

public class PermisoRepositorio(HdcDbContext ctx) : Repositorio<Permiso>(ctx)
{
    public Task<List<Permiso>> ListarAsync() => Ctx.Permisos.AsNoTracking()
        .Include(p => p.RolPermisos).ThenInclude(rp => rp.Rol)
        .OrderBy(p => p.Nombre)
        .ToListAsync();

    public Task<List<Permiso>> ObtenerPorIdsAsync(IEnumerable<long> ids) =>
        Ctx.Permisos.Where(p => ids.Contains(p.IdPermiso)).ToListAsync();

    public Task<bool> ExisteNombreAsync(string nombre, long? excluirId = null) =>
        Ctx.Permisos.AnyAsync(p => p.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || p.IdPermiso != excluirId));

    public Task<Permiso?> ObtenerConRolesAsync(long id) => Ctx.Permisos
        .Include(p => p.RolPermisos).ThenInclude(rp => rp.Rol)
        .FirstOrDefaultAsync(p => p.IdPermiso == id);
}

/// <summary>Pedidos de recuperación de contraseña (tabla RECUPERACION_CONTRASENA).</summary>
public class RecuperacionRepositorio(HdcDbContext ctx) : Repositorio<RecuperacionContrasena>(ctx)
{
    /// <summary>Busca el pedido por el hash del token, con su usuario (para cambiarle la contraseña).</summary>
    public Task<RecuperacionContrasena?> ObtenerPorHashAsync(string tokenHash) => Ctx.RecuperacionesContrasena
        .Include(r => r.Usuario)
        .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

    /// <summary>Pedidos del usuario que todavía no se usaron (para invalidarlos cuando pide uno nuevo o cambia la clave).</summary>
    public Task<List<RecuperacionContrasena>> ListarPendientesAsync(long idUsuario) => Ctx.RecuperacionesContrasena
        .Where(r => r.IdUsuario == idUsuario && r.FechaUso == null)
        .ToListAsync();
}
