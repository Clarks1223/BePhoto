using BePhoto.Api.Configuracion;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BePhoto.Api.Servicios
{
    public record FotoSubida(string Url, string PublicId);

    // Dónde se guardan las fotos. Hoy es Cloudinary.
    public interface IAlmacenFotos
    {
        Task<FotoSubida> SubirAsync(IFormFile foto, Guid usuarioId, CancellationToken ct);
        Task EliminarAsync(string publicId);
    }

    public class CloudinaryAlmacenFotos : IAlmacenFotos
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _carpeta;

        public CloudinaryAlmacenFotos(CloudinaryOpciones opciones)
        {
            _cloudinary = new Cloudinary(new Account(opciones.CloudName, opciones.ApiKey, opciones.ApiSecret));
            _cloudinary.Api.Secure = true;
            _carpeta = opciones.Carpeta.Trim('/');
        }

        public async Task<FotoSubida> SubirAsync(IFormFile foto, Guid usuarioId, CancellationToken ct)
        {
            await using Stream flujo = foto.OpenReadStream();

            var parametros = new ImageUploadParams
            {
                File = new FileDescription(foto.FileName, flujo),
                // Nombre único: ProyectoFinalMoviles/{usuario}/{guid}
                PublicId = $"{_carpeta}/{usuarioId}/{Guid.NewGuid():N}",
                Overwrite = false
            };

            ImageUploadResult resultado = await _cloudinary.UploadAsync(parametros, ct);
            if (resultado.Error is not null)
                throw new InvalidOperationException($"Cloudinary rechazó la foto: {resultado.Error.Message}");

            return new FotoSubida(resultado.SecureUrl.ToString(), resultado.PublicId);
        }

        public async Task EliminarAsync(string publicId)
        {
            await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        }
    }
}
