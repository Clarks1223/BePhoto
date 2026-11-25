using System.Text.Json;
using System.Text.Json.Serialization;
using FrontEnd.Models;

namespace FrontEnd.Repositories
{
    // Recuerdos que comparten la galería y el mapa: los capturados en este teléfono
    // (fotos y recuerdos.json en la carpeta privada de la app) y los de ejemplo.
    // TODO: cuando exista el backend, subir la foto a Cloudinary y guardar en la base de
    // datos solo su URL (Recuerdo.Foto). La app no debe llevar las llaves de Cloudinary.
    public static class RecuerdoRepository
    {
        private static readonly IReadOnlyList<Recuerdo> Ejemplos = new List<Recuerdo>
        {
            new("foto_0003", new DateTime(2026, 11, 18), "recuerdo_costa.svg",
                "Una tarde tranquila frente al mar.", 19.4326, -99.1332),
            new("foto_0002", new DateTime(2026, 11, 2), "recuerdo_montana.svg",
                "Un camino entre montañas y un paisaje inolvidable.", 19.4326, -99.1332),
            new("foto_0001", new DateTime(2026, 10, 20), "recuerdo_ciudad.svg",
                "Un paseo por las calles del centro.", 19.4326, -99.1332)
        };

        private static readonly string Carpeta = Path.Combine(FileSystem.AppDataDirectory, "recuerdos");
        private static readonly string Archivo = Path.Combine(Carpeta, "recuerdos.json");

        private const string ClaveUltimoNumero = "ultimo_numero_foto";
        private const string ClaveDetalleEjemplo = "detalle_";
        private const string ClaveEjemplosEliminados = "ejemplos_eliminados";

        private static readonly List<Recuerdo> Capturados = Cargar();

        // Los más recientes primero.
        public static IReadOnlyList<Recuerdo> ObtenerTodos() =>
            [.. Capturados.Concat(EjemplosVigentes()).OrderByDescending(recuerdo => recuerdo.Fecha)];

        public static async Task<Recuerdo> GuardarAsync(FileResult foto, Location ubicacion, DateTime fecha)
        {
            Directory.CreateDirectory(Carpeta);
            string nombre = SiguienteNombre();
            string rutaFoto = Path.Combine(Carpeta, $"{nombre}.jpg");

            try
            {
                using (Stream origen = await foto.OpenReadAsync())
                using (FileStream destino = File.Create(rutaFoto))
                    await origen.CopyToAsync(destino);

                var recuerdo = new Recuerdo(
                    nombre,
                    fecha,
                    rutaFoto,
                    string.Empty,
                    ubicacion.Latitude,
                    ubicacion.Longitude,
                    ubicacion.Accuracy);

                List<Recuerdo> nuevos = [recuerdo, .. Capturados];

                await EscribirAsync(nuevos);

                Capturados.Insert(0, recuerdo);
                return recuerdo;
            }
            catch
            {
                File.Delete(rutaFoto);
                throw;
            }
        }

        public static async Task EditarDetalleAsync(Recuerdo recuerdo, string detalle)
        {
            if (!Capturados.Contains(recuerdo))
            {
                Preferences.Default.Set(ClaveDetalleEjemplo + recuerdo.Nombre, detalle);
                recuerdo.Descripcion = detalle;
                return;
            }

            // Las pantallas comparten el mismo objeto, así que todas ven el cambio.
            string anterior = recuerdo.Descripcion;
            recuerdo.Descripcion = detalle;
            try
            {
                await EscribirAsync(Capturados);
            }
            catch
            {
                recuerdo.Descripcion = anterior;
                throw;
            }
        }

        // El número avanza siempre: el de una foto eliminada no se vuelve a usar.
        private static string SiguienteNombre()
        {
            int numero = Preferences.Default.Get(ClaveUltimoNumero, Ejemplos.Count) + 1;
            Preferences.Default.Set(ClaveUltimoNumero, numero);
            return $"foto_{numero:0000}";
        }

        // TODO: eliminarla también de Cloudinary y del backend cuando existan.
        public static async Task EliminarAsync(Recuerdo recuerdo)
        {
            if (!Capturados.Contains(recuerdo))
            {
                // Los ejemplos vienen en la app: solo se recuerda cuáles se quitaron.
                Preferences.Default.Set(ClaveEjemplosEliminados,
                    string.Join('|', EjemplosEliminados().Append(recuerdo.Foto).Distinct()));
                return;
            }

            await EscribirAsync(Capturados.Where(otro => otro != recuerdo).ToList());
            Capturados.Remove(recuerdo);

            try
            {
                File.Delete(recuerdo.Foto);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // El recuerdo ya no está en la lista; solo queda un archivo sin usar.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static string[] EjemplosEliminados() =>
            Preferences.Default.Get(ClaveEjemplosEliminados, string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries);

        private static IEnumerable<Recuerdo> EjemplosVigentes()
        {
            string[] eliminados = EjemplosEliminados();
            return Ejemplos.Where(ejemplo => !eliminados.Contains(ejemplo.Foto));
        }

        // Se escribe aparte y se reemplaza, para no dejar el archivo a medias.
        private static async Task EscribirAsync(List<Recuerdo> recuerdos)
        {
            string temporal = Archivo + ".tmp";
            await File.WriteAllTextAsync(temporal,
                JsonSerializer.Serialize(recuerdos, RecuerdoJson.Default.ListRecuerdo));
            File.Move(temporal, Archivo, overwrite: true);
        }

        private static List<Recuerdo> Cargar()
        {
            try
            {
                foreach (Recuerdo ejemplo in Ejemplos)
                    ejemplo.Descripcion = Preferences.Default.Get(
                        ClaveDetalleEjemplo + ejemplo.Nombre, ejemplo.Descripcion);

                if (!File.Exists(Archivo))
                    return [];

                List<Recuerdo> recuerdos = JsonSerializer.Deserialize(
                    File.ReadAllText(Archivo), RecuerdoJson.Default.ListRecuerdo) ?? [];

                // Los guardados antes de numerar las fotos no traen nombre.
                if (recuerdos.Any(recuerdo => string.IsNullOrEmpty(recuerdo.Nombre)))
                {
                    Dictionary<Recuerdo, string> nombres = recuerdos
                        .Where(recuerdo => string.IsNullOrEmpty(recuerdo.Nombre))
                        .OrderBy(recuerdo => recuerdo.Fecha)
                        .ToDictionary(recuerdo => recuerdo, _ => SiguienteNombre());
                    recuerdos = recuerdos.ConvertAll(recuerdo =>
                        nombres.TryGetValue(recuerdo, out string? nombre)
                            ? recuerdo with { Nombre = nombre }
                            : recuerdo);
                    File.WriteAllText(Archivo,
                        JsonSerializer.Serialize(recuerdos, RecuerdoJson.Default.ListRecuerdo));
                }
                return recuerdos;
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                return [];
            }
        }
    }

    [JsonSerializable(typeof(List<Recuerdo>))]
    internal sealed partial class RecuerdoJson : JsonSerializerContext
    {
    }
}
