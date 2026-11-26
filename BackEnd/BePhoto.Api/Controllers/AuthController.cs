using System.Security.Cryptography;
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
    public class AuthController(
        AppDbContext db,
        TokenServicio tokens,
        ICorreoServicio correos,
        ILogger<AuthController> log) : ControllerBase
    {
        private const int MinutosValidezCodigo = 15;
        private const int SegundosEntreCodigos = 60;
        private const int MaxIntentosCodigo = 5;

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

        // Recuperar la contraseña, paso 1: manda un código de 6 dígitos al correo.
        // Responde siempre lo mismo exista o no la cuenta, para no revelar quién está registrado.
        [HttpPost("olvide-contrasena")]
        public async Task<IActionResult> OlvideContrasena([FromBody] OlvideContrasenaRequest solicitud, CancellationToken ct)
        {
            string correo = Normalizar(solicitud.Correo);
            Usuario? usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo, ct);

            if (usuario is not null)
            {
                DateTime ahora = DateTime.UtcNow;

                // Si ya se envió uno hace menos de un minuto no se manda otro (evita llenar el buzón).
                bool reciente = await db.CodigosRestablecimiento.AnyAsync(
                    c => c.UsuarioId == usuario.Id && c.FechaCreacion > ahora.AddSeconds(-SegundosEntreCodigos), ct);

                if (!reciente)
                {
                    // El código nuevo anula los anteriores.
                    await db.CodigosRestablecimiento
                        .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
                        .ExecuteUpdateAsync(c => c.SetProperty(x => x.Usado, true), ct);

                    string codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                    db.CodigosRestablecimiento.Add(new CodigoRestablecimiento
                    {
                        UsuarioId = usuario.Id,
                        CodigoHash = BCrypt.Net.BCrypt.HashPassword(codigo),
                        Expira = ahora.AddMinutes(MinutosValidezCodigo),
                        FechaCreacion = ahora
                    });
                    await db.SaveChangesAsync(ct);

                    try
                    {
                        await correos.EnviarCodigoRecuperacionAsync(usuario.Correo, usuario.Nombre, codigo, ct);
                    }
                    catch (Exception ex)
                    {
                        // Al usuario no se le dice (la respuesta es la misma); queda en el registro del servidor.
                        log.LogError(ex, "No se pudo enviar el correo de recuperación a {Correo}", usuario.Correo);
                    }
                }
            }

            return Ok(new
            {
                mensaje = $"Si el correo está registrado, te enviamos un código de 6 dígitos. Vale {MinutosValidezCodigo} minutos."
            });
        }

        // Recuperar la contraseña, paso 2: con el código correcto se guarda la nueva contraseña.
        [HttpPost("restablecer-contrasena")]
        public async Task<IActionResult> RestablecerContrasena([FromBody] RestablecerContrasenaRequest solicitud, CancellationToken ct)
        {
            const string invalido = "El código no es correcto o ya venció. Pide uno nuevo.";
            string correo = Normalizar(solicitud.Correo);

            Usuario? usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo, ct);
            if (usuario is null)
                return Problem(detail: invalido, statusCode: StatusCodes.Status400BadRequest);

            DateTime ahora = DateTime.UtcNow;
            CodigoRestablecimiento? codigo = await db.CodigosRestablecimiento
                .Where(c => c.UsuarioId == usuario.Id && !c.Usado && c.Expira > ahora)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync(ct);
            if (codigo is null)
                return Problem(detail: invalido, statusCode: StatusCodes.Status400BadRequest);

            // El intento se cuenta antes de comprobar el código, en una sola operación, para que
            // varias peticiones a la vez no puedan pasarse del límite.
            int contado = await db.CodigosRestablecimiento
                .Where(c => c.Id == codigo.Id && c.Intentos < MaxIntentosCodigo)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.Intentos, x => x.Intentos + 1), ct);
            if (contado == 0)
                return Problem(detail: "Demasiados intentos. Pide un código nuevo.", statusCode: StatusCodes.Status400BadRequest);

            if (!BCrypt.Net.BCrypt.Verify(solicitud.Codigo, codigo.CodigoHash))
                return Problem(detail: invalido, statusCode: StatusCodes.Status400BadRequest);

            // Cada código se puede usar una sola vez, aunque lleguen dos peticiones iguales.
            int gastado = await db.CodigosRestablecimiento
                .Where(c => c.Id == codigo.Id && !c.Usado)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.Usado, true), ct);
            if (gastado == 0)
                return Problem(detail: invalido, statusCode: StatusCodes.Status400BadRequest);

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
