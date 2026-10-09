using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HDC.BLL.Comun;
using HDC.Entity.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HDC.BLL.Servicios;

public class TokenServicio(IConfiguration config)
{
    public (string Token, DateTime Expira) Generar(Usuario usuario, IEnumerable<string> permisos)
    {
        var clave = config["Jwt:Clave"] ?? throw new InvalidOperationException("Falta configurar Jwt:Clave");
        var minutos = int.TryParse(config["Jwt:ExpiraMinutos"], out var m) ? m : 120;
        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
            new("idRol", usuario.IdRol.ToString()),
        };
        claims.AddRange(permisos.Select(p => new Claim(Permisos.Claim, p)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Emisor"],
            audience: config["Jwt:Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
