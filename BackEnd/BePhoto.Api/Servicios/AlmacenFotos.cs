using System.Text;
using BePhoto.Api.Configuracion;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BePhoto.Api.Servicios
{
    public record FotoSubida(string Url, string PublicId);

    // Dónde se guardan las fotos. Hoy es Cloudinary.
    // Cada usuario tiene su carpeta, nombrada con su correo: ProyectoFinalMoviles/{correo}/{foto}.
    public interface IAlmacenFotos
    {
        Task<FotoSubida> SubirAsync(IFormFile foto, string correoUsuario, CancellationToken ct);
        Task EliminarAsync(string publicId);

        // true si la foto ya está en la carpeta del usuario.
        bool EstaEnCarpeta(string publicId, string correoUsuario);

        // Lleva una foto ya subida a la carpeta del usuario y devuelve su nueva URL e id.
        Task<FotoSubida> MoverACarpetaAsync(string publicId, string correoUsuario, CancellationToken ct);
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

        public async Task<FotoSubida> SubirAsync(IFormFile foto, string correoUsuario, CancellationToken ct)
        {
            await using Stream flujo = foto.OpenReadStream();
            string carpeta = CarpetaDe(correoUsuario);

            var parametros = new ImageUploadParams
            {
                File = new FileDescription(foto.FileName, flujo),
                // Nombre único: ProyectoFinalMoviles/{correo}/{guid}
                PublicId = $"{carpeta}/{Guid.NewGuid():N}",
                // Para las cuentas de Cloudinary con "carpetas dinámicas", donde la carpeta
                // que se ve en la consola no sale del nombre. En las demás se ignora.
                AssetFolder = carpeta,
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

        public bool EstaEnCarpeta(string publicId, string correoUsuario) =>
            publicId.StartsWith(CarpetaDe(correoUsuario) + "/", StringComparison.Ordinal);

        public async Task<FotoSubida> MoverACarpetaAsync(string publicId, string correoUsuario, CancellationToken ct)
        {
            string carpeta = CarpetaDe(correoUsuario);
            string nombre = publicId[(publicId.LastIndexOf('/') + 1)..];
            string nuevoId = $"{carpeta}/{nombre}";

            RenameResult renombrado = await _cloudinary.RenameAsync(
                new RenameParams(publicId, nuevoId) { Overwrite = false, Invalidate = true }, ct);
            if (renombrado.Error is not null)
                throw new InvalidOperationException($"Cloudinary no pudo mover la foto: {renombrado.Error.Message}");

            // Con carpetas dinámicas, además hay que cambiar la carpeta que muestra la consola.
            // Con carpetas fijas no hace falta (el nombre ya la define) y Cloudinary lo rechaza.
            // No se lanza error: la foto ya se movió y debe guardarse su nuevo nombre.
            if (renombrado.AssetFolder is not null)
                await _cloudinary.UpdateResourceAsync(new UpdateParams(nuevoId) { AssetFolder = carpeta }, ct);

            return new FotoSubida(renombrado.SecureUrl, renombrado.PublicId);
        }

        // El correo ya llega en minúsculas. Solo se dejan los caracteres que Cloudinary
        // acepta sin problemas en un nombre; cualquier otro se cambia por "_".
        private string CarpetaDe(string correo)
        {
            var limpio = new StringBuilder(correo.Length);
            foreach (char c in correo.Trim().ToLowerInvariant())
                limpio.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '@' ? c : '_');
            return $"{_carpeta}/{limpio}";
        }
    }
}
