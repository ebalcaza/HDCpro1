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
        ConPerfil().Include(u => u.Vehiculos).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

    public Task<List<Usuario>> ListarAsync() => Ctx.Usuarios.AsNoTracking()
        .Include(u => u.Rol)
        .Include(u => u.Individuo)
        .Include(u => u.Organizacion)
        .Include(u => u.Domicilios)
        .OrderByDescending(u => u.FechaAlta)
        .ToListAsync();

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

    public Task<Vehiculo?> ObtenerVehiculoAsync(long idVehiculo) => Ctx.Vehiculos.FindAsync(idVehiculo).AsTask();

    public void AgregarVehiculo(Vehiculo v) => Ctx.Vehiculos.Add(v);

    public void EliminarVehiculo(Vehiculo v) => Ctx.Vehiculos.Remove(v);
}

public class RolRepositorio(HdcDbContext ctx) : Repositorio<Rol>(ctx)
{
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
        Ctx.Roles.AnyAsync(r => r.Nombre == nombre && (excluirId == null || r.IdRol != excluirId));

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
        Ctx.Permisos.AnyAsync(p => p.Nombre == nombre && (excluirId == null || p.IdPermiso != excluirId));

    public Task<Permiso?> ObtenerConRolesAsync(long id) => Ctx.Permisos
        .Include(p => p.RolPermisos).ThenInclude(rp => rp.Rol)
        .FirstOrDefaultAsync(p => p.IdPermiso == id);
}
