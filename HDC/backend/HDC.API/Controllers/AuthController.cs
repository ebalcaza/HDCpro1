using HDC.API.Infraestructura;
using HDC.BLL.Servicios;
using HDC.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDC.API.Controllers;

[Route("api/auth")]
public class AuthController(AuthServicio auth) : ControladorBase
{
    /// <summary>RFU02 - Inicio de sesión.</summary>
    [AllowAnonymous, HttpPost("login")]
    public async Task<AuthResponse> Login(LoginRequest req) => await auth.LoginAsync(req);

    /// <summary>RFU01 - Registro de un individuo.</summary>
    [AllowAnonymous, HttpPost("registro/individuo")]
    public async Task<AuthResponse> RegistrarIndividuo(RegistroIndividuoRequest req) => await auth.RegistrarIndividuoAsync(req);

    /// <summary>RFU01 - Registro de una organización.</summary>
    [AllowAnonymous, HttpPost("registro/organizacion")]
    public async Task<AuthResponse> RegistrarOrganizacion(RegistroOrganizacionRequest req) => await auth.RegistrarOrganizacionAsync(req);

    /// <summary>Datos de la sesión actual (para restaurar la sesión al recargar la página).</summary>
    [HttpGet("sesion")]
    public async Task<UsuarioSesionDto> Sesion() => await auth.SesionAsync(IdUsuario);
}

[Route("api/perfil")]
public class PerfilController(PerfilServicio perfil) : ControladorBase
{
    /// <summary>RFU04 - Mi perfil.</summary>
    [HttpGet]
    public async Task<PerfilDto> Obtener() => await perfil.ObtenerAsync(IdUsuario);

    /// <summary>RFU05 - Editar mi perfil.</summary>
    [HttpPut]
    public async Task<PerfilDto> Actualizar(ActualizarPerfilRequest req) => await perfil.ActualizarAsync(IdUsuario, req);

    [HttpPut("contrasena")]
    public async Task<MensajeDto> CambiarContrasena(CambiarContrasenaRequest req)
    {
        await perfil.CambiarContrasenaAsync(IdUsuario, req);
        return new MensajeDto("Contraseña actualizada");
    }

    [HttpPost("vehiculos")]
    public async Task<VehiculoDto> AgregarVehiculo(VehiculoRequest req) => await perfil.AgregarVehiculoAsync(IdUsuario, req);

    [HttpPut("vehiculos/{id:long}")]
    public async Task<VehiculoDto> ActualizarVehiculo(long id, VehiculoRequest req) => await perfil.ActualizarVehiculoAsync(IdUsuario, id, req);

    [HttpDelete("vehiculos/{id:long}")]
    public async Task<MensajeDto> EliminarVehiculo(long id)
    {
        await perfil.EliminarVehiculoAsync(IdUsuario, id);
        return new MensajeDto("Vehículo eliminado");
    }
}

/// <summary>Geografía (pública: se usa en el formulario de registro).</summary>
[AllowAnonymous, Route("api/geo")]
public class GeoController(CatalogoServicio catalogos) : ControladorBase
{
    [HttpGet("provincias")]
    public async Task<List<ProvinciaDto>> Provincias() => await catalogos.ListarProvinciasAsync();

    [HttpGet("provincias/{idProvincia:long}/distritos")]
    public async Task<List<DistritoDto>> Distritos(long idProvincia) => await catalogos.ListarDistritosAsync(idProvincia);

    [HttpGet("distritos/{idDistrito:long}/ciudades")]
    public async Task<List<CiudadDto>> Ciudades(long idDistrito) => await catalogos.ListarCiudadesAsync(idDistrito);
}
