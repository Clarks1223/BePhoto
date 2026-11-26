using System.Security.Claims;
using System.Text;
using BePhoto.Api.Configuracion;
using BePhoto.Api.Entidades;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BePhoto.Api.Servicios
{
    // Crea el token (JWT) que la app guarda al iniciar sesión y envía en cada petición.
    public class TokenServicio(JwtOpciones opciones)
    {
        public (string Token, DateTime Expira) Crear(Usuario usuario)
        {
            DateTime expira = DateTime.UtcNow.AddDays(opciones.DiasValidez);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", usuario.Id.ToString()),
                    new Claim("email", usuario.Correo),
                    new Claim("name", $"{usuario.Nombre} {usuario.Apellido}")
                }),
                Issuer = opciones.Issuer,
                Audience = opciones.Audience,
                Expires = expira,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key)),
                    SecurityAlgorithms.HmacSha256)
            };

            string token = new JsonWebTokenHandler().CreateToken(descriptor);
            return (token, expira);
        }
    }
}
