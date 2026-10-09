using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

public class UsuarioServicio(
    UsuarioRepositorio usuarios,
    RolRepositorio roles,
    PerfilServicio perfiles,
    DomicilioServicio domicilios)
{
    public async Task<List<UsuarioListaDto>> ListarAsync()
    {
        var lista = await usuarios.ListarAsync();
        return lista.Select(u => new UsuarioListaDto(
            u.IdUsuario, u.Email, u.IdRol, u.Rol.Nombre, Mapeos.NombreMostrar(u), Mapeos.TipoPerfil(u),
            u.Telefono, u.FechaAlta, u.FechaBaja, u.FechaBaja == null,
            u.Domicilios.Count(d => d.FechaBaja == null),
            u.Domicilios.Sum(d => d.TotalKgCo2))).ToList();
    }

    public async Task<UsuarioDetalleDto> ObtenerAsync(long idUsuario)
    {
        var u = await usuarios.ObtenerCompletoAsync(idUsuario) ?? throw new NoEncontradoException("Usuario no encontrado");
        return new UsuarioDetalleDto(
            await perfiles.APerfilAsync(u),
            await domicilios.ListarAsync(idUsuario, incluirBajas: true),
            await usuarios.ContarRegistrosAsync(idUsuario));
    }

    public async Task CambiarRolAsync(long idAdmin, long idUsuario, long idRol)
    {
        if (idAdmin == idUsuario) throw new NegocioException("No podés cambiar tu propio tipo de usuario");
        var u = await usuarios.ObtenerPorIdAsync(idUsuario) ?? throw new NoEncontradoException("Usuario no encontrado");
        _ = await roles.ObtenerPorIdAsync(idRol) ?? throw new NegocioException("El tipo de usuario no existe");
        u.IdRol = idRol;
        await usuarios.GuardarAsync();
    }

    public async Task CambiarEstadoAsync(long idAdmin, long idUsuario, bool activo)
    {
        if (idAdmin == idUsuario) throw new NegocioException("No podés deshabilitar tu propia cuenta");
        var u = await usuarios.ObtenerPorIdAsync(idUsuario) ?? throw new NoEncontradoException("Usuario no encontrado");
        u.FechaBaja = activo ? null : DateTime.Now;
        await usuarios.GuardarAsync();
    }
}

public class RolServicio(RolRepositorio roles, PermisoRepositorio permisos)
{
    public async Task<List<RolDto>> ListarAsync() => (await roles.ListarAsync()).Select(ADto).ToList();

    public async Task<RolDto> CrearAsync(RolRequest req)
    {
        var nombre = req.Nombre.Trim();
        if (await roles.ExisteNombreAsync(nombre)) throw new ConflictoException("Ya existe un tipo de usuario con ese nombre");
        var rol = new Rol { Nombre = nombre, Descripcion = Mapeos.Limpiar(req.Descripcion) };
        await AsignarPermisosAsync(rol, req.IdsPermisos);
        roles.Agregar(rol);
        await roles.GuardarAsync();
        return ADto((await roles.ObtenerConPermisosAsync(rol.IdRol))!);
    }

    public async Task<RolDto> ActualizarAsync(long idRol, RolRequest req)
    {
        var rol = await roles.ObtenerConPermisosAsync(idRol) ?? throw new NoEncontradoException("Tipo de usuario no encontrado");
        var nombre = req.Nombre.Trim();
        if (rol.EsSistema && nombre != rol.Nombre)
            throw new NegocioException("No se puede renombrar un tipo de usuario del sistema");
        if (await roles.ExisteNombreAsync(nombre, idRol)) throw new ConflictoException("Ya existe un tipo de usuario con ese nombre");
        if (idRol == RolesSistema.Administrador)
        {
            var requeridos = await permisos.ObtenerPorIdsAsync(req.IdsPermisos);
            if (!requeridos.Any(p => p.Nombre == Permisos.AdminPanel) || !requeridos.Any(p => p.Nombre == Permisos.GestionarRoles))
                throw new NegocioException("El Administrador debe conservar los permisos ADMIN_PANEL y GESTIONAR_ROLES");
        }
        rol.Nombre = nombre;
        rol.Descripcion = Mapeos.Limpiar(req.Descripcion);
        await AsignarPermisosAsync(rol, req.IdsPermisos);
        await roles.GuardarAsync();
        return ADto((await roles.ObtenerConPermisosAsync(idRol))!);
    }

    public async Task EliminarAsync(long idRol)
    {
        var rol = await roles.ObtenerPorIdAsync(idRol) ?? throw new NoEncontradoException("Tipo de usuario no encontrado");
        if (rol.EsSistema) throw new NegocioException("No se puede eliminar un tipo de usuario del sistema");
        if (await roles.TieneUsuariosAsync(idRol)) throw new ConflictoException("Hay usuarios con este tipo asignado; reasignalos antes de eliminarlo");
        roles.Eliminar(rol);
        await roles.GuardarAsync();
    }

    private async Task AsignarPermisosAsync(Rol rol, List<long> idsPermisos)
    {
        var ids = idsPermisos.Distinct().ToList();
        var encontrados = await permisos.ObtenerPorIdsAsync(ids);
        if (encontrados.Count != ids.Count) throw new NegocioException("Alguno de los permisos no existe");
        foreach (var rp in rol.RolPermisos.Where(rp => !ids.Contains(rp.IdPermiso)).ToList())
            rol.RolPermisos.Remove(rp);
        foreach (var p in encontrados.Where(p => rol.RolPermisos.All(rp => rp.IdPermiso != p.IdPermiso)))
            rol.RolPermisos.Add(new RolPermiso { Permiso = p });
    }

    private static RolDto ADto(Rol r) => new(
        r.IdRol, r.Nombre, r.Descripcion, r.EsSistema, r.Usuarios.Count,
        r.RolPermisos.Select(rp => new PermisoDto(rp.Permiso.IdPermiso, rp.Permiso.Nombre, rp.Permiso.Descripcion, []))
            .OrderBy(p => p.Nombre).ToList());
}

public class PermisoServicio(PermisoRepositorio permisos)
{
    public async Task<List<PermisoDto>> ListarAsync() => (await permisos.ListarAsync()).Select(ADto).ToList();

    public async Task<PermisoDto> CrearAsync(PermisoRequest req)
    {
        var nombre = req.Nombre.Trim().ToUpperInvariant();
        if (await permisos.ExisteNombreAsync(nombre)) throw new ConflictoException("Ya existe un permiso con ese nombre");
        var p = new Permiso { Nombre = nombre, Descripcion = Mapeos.Limpiar(req.Descripcion) };
        permisos.Agregar(p);
        await permisos.GuardarAsync();
        return ADto(p);
    }

    public async Task<PermisoDto> ActualizarAsync(long id, PermisoRequest req)
    {
        var p = await permisos.ObtenerConRolesAsync(id) ?? throw new NoEncontradoException("Permiso no encontrado");
        var nombre = req.Nombre.Trim().ToUpperInvariant();
        if (Permisos.Todos.Contains(p.Nombre) && nombre != p.Nombre)
            throw new NegocioException("Este permiso lo usa el sistema; solo se puede editar su descripción");
        if (await permisos.ExisteNombreAsync(nombre, id)) throw new ConflictoException("Ya existe un permiso con ese nombre");
        p.Nombre = nombre;
        p.Descripcion = Mapeos.Limpiar(req.Descripcion);
        await permisos.GuardarAsync();
        return ADto(p);
    }

    public async Task EliminarAsync(long id)
    {
        var p = await permisos.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Permiso no encontrado");
        if (Permisos.Todos.Contains(p.Nombre)) throw new NegocioException("Este permiso lo usa el sistema y no se puede eliminar");
        permisos.Eliminar(p);
        await permisos.GuardarAsync();
    }

    private static PermisoDto ADto(Permiso p) => new(
        p.IdPermiso, p.Nombre, p.Descripcion, p.RolPermisos.Select(rp => rp.Rol.Nombre).OrderBy(n => n).ToList());
}
