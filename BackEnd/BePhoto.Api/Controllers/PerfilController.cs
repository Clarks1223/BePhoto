using BePhoto.Api.Data;
using BePhoto.Api.Dtos;
using BePhoto.Api.Entidades;
using BePhoto.Api.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Controllers
{
    // HU-BP-0008: datos del usuario y resumen de su actividad.
    [ApiController]
    [Authorize]
    [Route("api/perfil")]
    public class PerfilController(AppDbContext db) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PerfilDto>> Obtener(CancellationToken ct)
        {
            Guid id = User.UsuarioId();

            Usuario? usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (usuario is null)
                return Problem(detail: "La sesión ya no es válida.", statusCode: StatusCodes.Status401Unauthorized);

            var puntos = await db.Recuerdos
                .AsNoTracking()
                .Where(r => r.UsuarioId == id)
                .Select(r => new { r.Latitud, r.Longitud })
                .ToListAsync(ct);

            // Dos fotos a menos de ~100 m cuentan como el mismo lugar.
            int ubicaciones = puntos
                .Select(p => (Math.Round(p.Latitud, 3), Math.Round(p.Longitud, 3)))
                .Distinct()
                .Count();

            return new PerfilDto(
                new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo),
                puntos.Count,
                ubicaciones);
        }
    }
}
