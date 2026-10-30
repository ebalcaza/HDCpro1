using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using HDC.Entity.Validaciones;

namespace HDC.BLL.Servicios;

public class AuthServicio(UsuarioRepositorio usuarios, DomicilioServicio domicilioServicio, TokenServicio tokens)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var u = await usuarios.ObtenerPorEmailAsync(req.Email.Trim().ToLowerInvariant());
        if (u is null || !BCrypt.Net.BCrypt.Verify(req.Contrasena, u.Contrasena))
            throw new NoAutorizadoException("Email o contraseña incorrectos");
        if (u.FechaBaja != null)
            throw new ProhibidoException("Tu cuenta está deshabilitada. Contactá al administrador.");
        return CrearRespuesta(u);
    }

    public async Task<AuthResponse> RegistrarIndividuoAsync(RegistroIndividuoRequest req)
    {
        var dni = req.Dni.Trim();
        if (await usuarios.ExisteDniAsync(dni)) throw new ConflictoException("Ya existe un usuario con ese DNI");

        var u = await CrearUsuarioBaseAsync(req, RolesSistema.Individuo);
        u.Individuo = new Individuo
        {
            // Se guardan con mayúscula inicial y sin espacios sobrantes (ver Reglas.NormalizarNombre)
            Nombres = Reglas.NormalizarNombre(req.Nombres),
            Apellidos = Reglas.NormalizarNombre(req.Apellidos),
            Dni = dni,
            FechaNacimiento = req.FechaNacimiento,
            Genero = Mapeos.Limpiar(req.Genero)
        };
        return await GuardarYResponderAsync(u, req.Domicilio);
    }

    public async Task<AuthResponse> RegistrarOrganizacionAsync(RegistroOrganizacionRequest req)
    {
        var cuit = Mapeos.NormalizarCuit(req.Cuit);
        if (await usuarios.ExisteCuitAsync(cuit)) throw new ConflictoException("Ya existe una organización con ese CUIT");

        var u = await CrearUsuarioBaseAsync(req, RolesSistema.Organizacion);
        u.Organizacion = new Organizacion
        {
            RazonSocial = req.RazonSocial.Trim(),
            Cuit = cuit,
            Area = req.Area.Trim(),
            Descripcion = Mapeos.Limpiar(req.Descripcion),
            CantidadMiembros = req.CantidadMiembros
        };
        return await GuardarYResponderAsync(u, req.Domicilio);
    }

    /// <summary>
    /// Verifica si el email y/o el DNI ya están registrados (lo usa el registro al avanzar de paso).
    /// Devuelve { campo: mensaje } con los que ya existen; vacío si están todos disponibles.
    /// </summary>
    public async Task<Dictionary<string, string>> VerificarDisponibilidadAsync(VerificarDisponibilidadRequest req)
    {
        var errores = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(req.Email) && await usuarios.ExisteEmailAsync(req.Email.Trim().ToLowerInvariant()))
            errores["email"] = "El email ya está registrado";
        if (!string.IsNullOrWhiteSpace(req.Dni) && await usuarios.ExisteDniAsync(req.Dni.Trim()))
            errores["dni"] = "Ya existe un usuario con ese DNI";
        return errores;
    }

    public async Task<UsuarioSesionDto> SesionAsync(long idUsuario)
    {
        var u = await usuarios.ObtenerCompletoAsync(idUsuario) ?? throw new NoEncontradoException("Usuario no encontrado");
        return ASesion(u);
    }

    public static string HashContrasena(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 11);

    private async Task<Usuario> CrearUsuarioBaseAsync(RegistroBaseRequest req, long idRol)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await usuarios.ExisteEmailAsync(email)) throw new ConflictoException("El email ya está registrado");
        return new Usuario
        {
            IdRol = idRol,
            Email = email,
            Contrasena = HashContrasena(req.Contrasena),
            Telefono = Mapeos.Limpiar(req.Telefono),
            FechaAlta = DateTime.Now
        };
    }

    private async Task<AuthResponse> GuardarYResponderAsync(Usuario u, DomicilioRequest domicilio)
    {
        var d = new Domicilio { FechaAlta = DateTime.Now, Usuario = u };
        await domicilioServicio.AplicarAsync(d, domicilio);
        u.Domicilios.Add(d);
        usuarios.Agregar(u);
        await usuarios.GuardarAsync();
        return CrearRespuesta((await usuarios.ObtenerPorEmailAsync(u.Email))!);
    }

    private AuthResponse CrearRespuesta(Usuario u)
    {
        var sesion = ASesion(u);
        var (token, expira) = tokens.Generar(u, sesion.Permisos);
        return new AuthResponse(token, expira, sesion);
    }

    private static UsuarioSesionDto ASesion(Usuario u) => new(
        u.IdUsuario, u.Email, u.IdRol, u.Rol.Nombre, Mapeos.NombreMostrar(u), Mapeos.TipoPerfil(u),
        u.Rol.RolPermisos.Select(rp => rp.Permiso.Nombre).OrderBy(p => p).ToList());
}
