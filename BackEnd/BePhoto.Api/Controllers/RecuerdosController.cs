using BePhoto.Api.Data;
using BePhoto.Api.Dtos;
using BePhoto.Api.Entidades;
using BePhoto.Api.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Controllers
{
    // HU-BP-0003 (guardar), 0004 (galería), 0005 (detalle), 0006 (editar) y 0007 (eliminar).
    // Cada usuario solo ve y modifica sus propios recuerdos.
    [ApiController]
    [Authorize]
    [Route("api/recuerdos")]
    public class RecuerdosController(
        AppDbContext db,
        IAlmacenFotos fotos,
        ILogger<RecuerdosController> log) : ControllerBase
    {
        private const long TamanoMaximoFoto = 10 * 1024 * 1024; // 10 MB

        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".heic" };

        // Galería: los más recientes primero.
        [HttpGet]
        public async Task<ActionResult<List<RecuerdoDto>>> Listar(CancellationToken ct)
        {
            Guid usuarioId = User.UsuarioId();

            List<Recuerdo> recuerdos = await db.Recuerdos
                .AsNoTracking()
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync(ct);

            return recuerdos.Select(RecuerdoDto.Desde).ToList();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RecuerdoDto>> Obtener(Guid id, CancellationToken ct)
        {
            Guid usuarioId = User.UsuarioId();

            Recuerdo? recuerdo = await db.Recuerdos
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);

            return recuerdo is null ? NoEncontrado() : RecuerdoDto.Desde(recuerdo);
        }

        // Recibe la foto y sus datos, sube la foto a Cloudinary y guarda el registro.
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<ActionResult<RecuerdoDto>> Crear([FromForm] CrearRecuerdoRequest solicitud, CancellationToken ct)
        {
            string titulo = solicitud.Titulo.Trim();
            if (titulo.Length == 0)
                return Problem(detail: "El título es obligatorio.", statusCode: StatusCodes.Status400BadRequest);

            IFormFile? foto = solicitud.Foto;
            if (foto is null || foto.Length == 0)
                return Problem(detail: "Debes enviar la foto del recuerdo.", statusCode: StatusCodes.Status400BadRequest);

            if (foto.Length > TamanoMaximoFoto)
                return Problem(detail: "La foto pesa más de 10 MB.", statusCode: StatusCodes.Status400BadRequest);

            if (!EsImagen(foto))
                return Problem(detail: "El archivo debe ser una imagen (JPG, PNG o WEBP).", statusCode: StatusCodes.Status400BadRequest);

            Guid usuarioId = User.UsuarioId();

            // La foto va a la carpeta del usuario, nombrada con su correo.
            string? correo = await db.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.Correo)
                .FirstOrDefaultAsync(ct);
            if (correo is null)
                return Problem(detail: "La sesión ya no es válida.", statusCode: StatusCodes.Status401Unauthorized);

            FotoSubida subida;
            try
            {
                subida = await fotos.SubirAsync(foto, correo, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "No se pudo subir la foto a Cloudinary");
                return Problem(detail: "No se pudo subir la foto. Intenta de nuevo.", statusCode: StatusCodes.Status502BadGateway);
            }

            var recuerdo = new Recuerdo
            {
                UsuarioId = usuarioId,
                Titulo = titulo,
                Descripcion = solicitud.Descripcion?.Trim() ?? string.Empty,
                FotoUrl = subida.Url,
                FotoPublicId = subida.PublicId,
                Latitud = solicitud.Latitud!.Value,
                Longitud = solicitud.Longitud!.Value,
                Altitud = solicitud.Altitud,
                Fecha = solicitud.Fecha?.UtcDateTime ?? DateTime.UtcNow
            };

            db.Recuerdos.Add(recuerdo);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                // Si no se pudo guardar el registro, la foto subida no debe quedar huérfana.
                await IntentarBorrarFotoAsync(subida.PublicId);
                throw;
            }

            return CreatedAtAction(nameof(Obtener), new { id = recuerdo.Id }, RecuerdoDto.Desde(recuerdo));
        }

        // Solo se pueden editar el título y la descripción (la foto y el lugar no cambian).
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<RecuerdoDto>> Actualizar(
            Guid id, [FromBody] ActualizarRecuerdoRequest solicitud, CancellationToken ct)
        {
            string titulo = solicitud.Titulo.Trim();
            if (titulo.Length == 0)
                return Problem(detail: "El título es obligatorio.", statusCode: StatusCodes.Status400BadRequest);

            Guid usuarioId = User.UsuarioId();
            Recuerdo? recuerdo = await db.Recuerdos
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);
            if (recuerdo is null)
                return NoEncontrado();

            recuerdo.Titulo = titulo;
            recuerdo.Descripcion = solicitud.Descripcion?.Trim() ?? string.Empty;
            await db.SaveChangesAsync(ct);

            return RecuerdoDto.Desde(recuerdo);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
        {
            Guid usuarioId = User.UsuarioId();
            Recuerdo? recuerdo = await db.Recuerdos
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);
            if (recuerdo is null)
                return NoEncontrado();

            await IntentarBorrarFotoAsync(recuerdo.FotoPublicId);

            db.Recuerdos.Remove(recuerdo);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task IntentarBorrarFotoAsync(string publicId)
        {
            try
            {
                await fotos.EliminarAsync(publicId);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "No se pudo borrar la foto {PublicId} de Cloudinary", publicId);
            }
        }

        private static bool EsImagen(IFormFile foto) =>
            foto.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || ExtensionesPermitidas.Contains(Path.GetExtension(foto.FileName));

        private ObjectResult NoEncontrado() =>
            Problem(detail: "No se encontró el recuerdo.", statusCode: StatusCodes.Status404NotFound);
    }
}
