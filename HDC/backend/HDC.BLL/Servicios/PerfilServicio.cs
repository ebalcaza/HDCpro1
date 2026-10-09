using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

public class PerfilServicio(UsuarioRepositorio usuarios, DomicilioRepositorio domicilios)
{
    public async Task<PerfilDto> ObtenerAsync(long idUsuario)
    {
        var u = await ObtenerUsuarioAsync(idUsuario);
        return await APerfilAsync(u);
    }

    public async Task<PerfilDto> ActualizarAsync(long idUsuario, ActualizarPerfilRequest req)
    {
        var u = await ObtenerUsuarioAsync(idUsuario);
        u.Telefono = Mapeos.Limpiar(req.Telefono);

        if (u.Individuo is { } ind)
        {
            var datos = req.Individuo ?? throw new NegocioException("Faltan los datos personales");
            var dni = datos.Dni.Trim();
            if (await usuarios.ExisteDniAsync(dni, idUsuario)) throw new ConflictoException("Ya existe un usuario con ese DNI");
            ind.Nombres = datos.Nombres.Trim();
            ind.Apellidos = datos.Apellidos.Trim();
            ind.Dni = dni;
            ind.FechaNacimiento = datos.FechaNacimiento;
            ind.Genero = Mapeos.Limpiar(datos.Genero);
        }
        else if (u.Organizacion is { } org)
        {
            var datos = req.Organizacion ?? throw new NegocioException("Faltan los datos de la organización");
            var cuit = Mapeos.NormalizarCuit(datos.Cuit);
            if (await usuarios.ExisteCuitAsync(cuit, idUsuario)) throw new ConflictoException("Ya existe una organización con ese CUIT");
            org.RazonSocial = datos.RazonSocial.Trim();
            org.Cuit = cuit;
            org.Area = datos.Area.Trim();
            org.Descripcion = Mapeos.Limpiar(datos.Descripcion);
            org.CantidadMiembros = datos.CantidadMiembros;
        }

        await usuarios.GuardarAsync();
        return await APerfilAsync(u);
    }

    public async Task CambiarContrasenaAsync(long idUsuario, CambiarContrasenaRequest req)
    {
        var u = await ObtenerUsuarioAsync(idUsuario);
        if (!BCrypt.Net.BCrypt.Verify(req.ContrasenaActual, u.Contrasena))
            throw new NegocioException("La contraseña actual es incorrecta");
        u.Contrasena = AuthServicio.HashContrasena(req.ContrasenaNueva);
        await usuarios.GuardarAsync();
    }

    public async Task<VehiculoDto> AgregarVehiculoAsync(long idUsuario, VehiculoRequest req)
    {
        var u = await ObtenerUsuarioAsync(idUsuario);
        var v = new Vehiculo { IdUsuario = u.IdUsuario };
        Aplicar(v, req);
        usuarios.AgregarVehiculo(v);
        await usuarios.GuardarAsync();
        return Mapeos.ToDto(v);
    }

    public async Task<VehiculoDto> ActualizarVehiculoAsync(long idUsuario, long idVehiculo, VehiculoRequest req)
    {
        var v = await ObtenerVehiculoPropioAsync(idUsuario, idVehiculo);
        Aplicar(v, req);
        await usuarios.GuardarAsync();
        return Mapeos.ToDto(v);
    }

    public async Task EliminarVehiculoAsync(long idUsuario, long idVehiculo)
    {
        var v = await ObtenerVehiculoPropioAsync(idUsuario, idVehiculo);
        usuarios.EliminarVehiculo(v);
        await usuarios.GuardarAsync();
    }

    internal async Task<PerfilDto> APerfilAsync(Usuario u)
    {
        var doms = await domicilios.ListarPorUsuarioAsync(u.IdUsuario, incluirBajas: true);
        return new PerfilDto(
            u.IdUsuario, u.Email, u.Telefono, u.IdRol, u.Rol.Nombre, Mapeos.NombreMostrar(u), Mapeos.TipoPerfil(u),
            u.FechaAlta, u.FechaBaja,
            u.Individuo is { } i ? new IndividuoDto
            {
                Nombres = i.Nombres, Apellidos = i.Apellidos, Dni = i.Dni,
                FechaNacimiento = i.FechaNacimiento, Genero = i.Genero
            } : null,
            u.Organizacion is { } o ? new OrganizacionDto
            {
                RazonSocial = o.RazonSocial, Cuit = o.Cuit, Area = o.Area,
                Descripcion = o.Descripcion, CantidadMiembros = o.CantidadMiembros
            } : null,
            u.Vehiculos.Select(Mapeos.ToDto).ToList(),
            doms.Count(d => d.FechaBaja == null),
            doms.Sum(d => d.TotalKgCo2));
    }

    private async Task<Usuario> ObtenerUsuarioAsync(long idUsuario) =>
        await usuarios.ObtenerCompletoAsync(idUsuario) ?? throw new NoEncontradoException("Usuario no encontrado");

    private async Task<Vehiculo> ObtenerVehiculoPropioAsync(long idUsuario, long idVehiculo)
    {
        var v = await usuarios.ObtenerVehiculoAsync(idVehiculo) ?? throw new NoEncontradoException("Vehículo no encontrado");
        if (v.IdUsuario != idUsuario) throw new ProhibidoException("El vehículo no te pertenece");
        return v;
    }

    private static void Aplicar(Vehiculo v, VehiculoRequest req)
    {
        v.TipoVehiculo = req.TipoVehiculo;
        v.Marca = req.Marca.Trim();
        v.Modelo = req.Modelo.Trim();
        v.Patente = Mapeos.NormalizarPatente(req.Patente);
        v.TipoCombustible = req.TipoCombustible;
        v.TipoUso = req.TipoUso;
    }
}
