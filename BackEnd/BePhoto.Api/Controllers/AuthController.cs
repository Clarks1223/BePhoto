using BePhoto.Api.Data;
using BePhoto.Api.Dtos;
using BePhoto.Api.Entidades;
using BePhoto.Api.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Controllers
{
    // HU-BP-0001: crear cuenta e iniciar sesión.
    [ApiController]
    [Route("api/auth")]
    public class AuthController(AppDbContext db, TokenServicio tokens) : ControllerBase
    {
        [HttpPost("registro")]
        public async Task<ActionResult<AuthRespuesta>> Registro([FromBody] RegistroRequest solicitud, CancellationToken ct)
        {
            string correo = Normalizar(solicitud.Correo);

            if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
                return CorreoDuplicado();

            var usuario = new Usuario
            {
                Nombre = solicitud.Nombre.Trim(),
                Apellido = solicitud.Apellido.Trim(),
                Correo = correo,
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(solicitud.Contrasena)
            };

            db.Usuarios.Add(usuario);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Dos registros con el mismo correo al mismo tiempo.
                db.ChangeTracker.Clear();
                if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
                    return CorreoDuplicado();
                throw;
            }

            // La cuenta queda lista para usarse: se devuelve el token directamente.
            return StatusCode(StatusCodes.Status201Created, Respuesta(usuario));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthRespuesta>> Login([FromBody] LoginRequest solicitud, CancellationToken ct)
        {
            string correo = Normalizar(solicitud.Correo);
            Usuario? usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo, ct);

            if (usuario is null || !BCrypt.Net.BCrypt.Verify(solicitud.Contrasena, usuario.ContrasenaHash))
                return Problem(detail: "Correo o contraseña incorrectos", statusCode: StatusCodes.Status401Unauthorized);

            return Ok(Respuesta(usuario));
        }

        [Authorize]
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest solicitud, CancellationToken ct)
        {
            Guid id = User.UsuarioId();
            Usuario? usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (usuario is null)
                return Problem(detail: "La sesión ya no es válida.", statusCode: StatusCodes.Status401Unauthorized);

            if (!BCrypt.Net.BCrypt.Verify(solicitud.ContrasenaActual, usuario.ContrasenaHash))
                return Problem(detail: "La contraseña actual no es correcta.", statusCode: StatusCodes.Status400BadRequest);

            usuario.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(solicitud.NuevaContrasena);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }

        private static string Normalizar(string correo) => correo.Trim().ToLowerInvariant();

        private ObjectResult CorreoDuplicado() =>
            Problem(detail: "Ya existe una cuenta con ese correo.", statusCode: StatusCodes.Status409Conflict);

        private AuthRespuesta Respuesta(Usuario usuario)
        {
            (string token, DateTime expira) = tokens.Crear(usuario);
            return new AuthRespuesta(token, expira,
                new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo));
        }
    }
}
