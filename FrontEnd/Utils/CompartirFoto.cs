using FrontEnd.Models;

namespace FrontEnd.Utils
{
    // Comparte una foto con la hoja del sistema (WhatsApp, Facebook, Instagram, correo...).
    // Se manda el archivo y no el enlace: Instagram solo acepta imágenes y así la URL
    // de Cloudinary no queda a la vista.
    public static class CompartirFoto
    {
        private static readonly string Carpeta = Path.Combine(FileSystem.CacheDirectory, "compartir");

        // Solo descarga imágenes; el límite lo pone Conexion con su CancellationToken.
        private static readonly HttpClient Cliente = new() { Timeout = Timeout.InfiniteTimeSpan };

        private static readonly TimeSpan EsperaDescarga = TimeSpan.FromSeconds(30);

        // Las fotos de ejemplo vienen como SVG dentro de la app y las redes no los aceptan.
        public static bool Disponible(Recuerdo recuerdo) =>
            EsRemota(recuerdo.Foto) || File.Exists(recuerdo.Foto);

        public static async Task Compartir(Page pagina, Recuerdo recuerdo)
        {
            if (!Disponible(recuerdo))
                return;

            string? archivo = null;
            if (EsRemota(recuerdo.Foto))
            {
                if (!await Conexion.Ejecutar(pagina,
                        async ct => archivo = await DescargarAsync(recuerdo, ct), espera: EsperaDescarga))
                    return;
            }
            else
            {
                archivo = CopiarLocal(recuerdo);
            }

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = recuerdo.TextoAccesible,
                File = new ShareFile(archivo!, TipoContenido(archivo!)),
                // En iPad la hoja necesita un punto de origen.
                PresentationSourceBounds = new Rect(pagina.Width / 2, pagina.Height / 2, 0, 0)
            });
        }

        private static bool EsRemota(string foto) =>
            foto.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        // Cloudinary la convierte a JPG: WhatsApp e Instagram no siempre aceptan WebP o HEIC.
        private static async Task<string> DescargarAsync(Recuerdo recuerdo, CancellationToken ct)
        {
            string url = recuerdo.Foto.Contains("/upload/")
                ? recuerdo.Foto.Replace("/upload/", "/upload/f_jpg/")
                : recuerdo.Foto;

            using HttpResponseMessage respuesta = await Cliente.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            respuesta.EnsureSuccessStatusCode();

            string destino = PrepararDestino(recuerdo, ".jpg");
            await using (FileStream archivo = File.Create(destino))
                await respuesta.Content.CopyToAsync(archivo, ct);
            return destino;
        }

        // Las fotos de antes del backend ya están en el teléfono.
        private static string CopiarLocal(Recuerdo recuerdo)
        {
            string destino = PrepararDestino(recuerdo, Path.GetExtension(recuerdo.Foto));
            File.Copy(recuerdo.Foto, destino, overwrite: true);
            return destino;
        }

        // Se guarda solo la última foto compartida para que la caché no crezca.
        // El nombre (foto_0004.jpg) es el que ven las apps que muestran el archivo.
        private static string PrepararDestino(Recuerdo recuerdo, string extension)
        {
            Directory.CreateDirectory(Carpeta);
            foreach (string anterior in Directory.GetFiles(Carpeta))
            {
                try
                {
                    File.Delete(anterior);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            string nombre = string.IsNullOrWhiteSpace(recuerdo.Nombre) ? "foto" : recuerdo.Nombre;
            return Path.Combine(Carpeta, nombre + extension);
        }

        private static string TipoContenido(string archivo) =>
            Path.GetExtension(archivo).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                _ => "image/jpeg"
            };
    }
}
