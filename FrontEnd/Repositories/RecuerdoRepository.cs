using System.Text.Json;
using System.Text.Json.Serialization;
using FrontEnd.Models;
using FrontEnd.Services;

namespace FrontEnd.Repositories
{
    // Recuerdos que comparten la galería y el mapa: los del usuario y los de ejemplo.
    // La verdad está en el servidor (BackEnd): la foto se sube a Cloudinary desde la API y
    // aquí solo se guarda su URL. recuerdos.json (carpeta privada de la app) es una copia
    // para que la galería abra al instante y sirva sin conexión. La app no lleva las llaves
    // de Cloudinary.
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

        // true cuando la copia del teléfono ya se igualó con el servidor en esta sesión.
        public static bool Sincronizado { get; private set; }

        // Los más recientes primero.
        public static IReadOnlyList<Recuerdo> ObtenerTodos() =>
            [.. Capturados.Concat(EjemplosVigentes()).OrderByDescending(recuerdo => recuerdo.Fecha)];

        // Sube la foto al servidor y guarda el recuerdo. Si algo falla no queda nada a medias,
        // así que Conexion puede reintentar sin duplicar.
        public static async Task<Recuerdo> GuardarAsync(
            FileResult foto, Location ubicacion, DateTime fecha, CancellationToken ct = default)
        {
            // El número solo avanza cuando la foto ya se guardó: un reintento usa el mismo.
            int numero = Preferences.Default.Get(ClaveUltimoNumero, Ejemplos.Count) + 1;
            string nombre = $"foto_{numero:0000}";

            RecuerdoApi creado = await Api.CrearRecuerdoAsync(
                foto, nombre, ubicacion.Latitude, ubicacion.Longitude, ubicacion.Altitude, fecha, ct);
            Preferences.Default.Set(ClaveUltimoNumero, numero);

            // El servidor no guarda la precisión del GPS: se conserva solo en este teléfono.
            Recuerdo recuerdo = Mapear(creado) with { PrecisionMetros = ubicacion.Accuracy };

            Capturados.Insert(0, recuerdo);
            await GuardarCopiaAsync();
            return recuerdo;
        }

        // Trae los recuerdos del usuario desde el servidor y reemplaza la copia del teléfono.
        // Los que solo existen aquí (Id nulo, de antes de conectar el backend) se conservan.
        public static async Task SincronizarAsync(CancellationToken ct = default)
        {
            List<RecuerdoApi> remotos = await Api.ListarRecuerdosAsync(ct);

            Dictionary<string, double?> precisiones = Capturados
                .Where(recuerdo => recuerdo.Id is not null)
                .ToDictionary(recuerdo => recuerdo.Id!, recuerdo => recuerdo.PrecisionMetros);
            List<Recuerdo> soloLocales = Capturados.Where(recuerdo => recuerdo.Id is null).ToList();

            Capturados.Clear();
            Capturados.AddRange(remotos.Select(remoto =>
            {
                Recuerdo recuerdo = Mapear(remoto);
                return precisiones.TryGetValue(recuerdo.Id!, out double? precision)
                    ? recuerdo with { PrecisionMetros = precision }
                    : recuerdo;
            }));
            Capturados.AddRange(soloLocales);

            Sincronizado = true;
            await GuardarCopiaAsync();
        }

        // Al cerrar sesión o cambiar de cuenta no deben quedar los recuerdos del usuario anterior.
        public static void LimpiarSesion()
        {
            Sincronizado = false;
            if (Capturados.RemoveAll(recuerdo => recuerdo.Id is not null) == 0)
                return;

            try
            {
                File.WriteAllText(Archivo,
                    JsonSerializer.Serialize(Capturados, RecuerdoJson.Default.ListRecuerdo));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        // La foto de Cloudinary viene a su tamaño original; q_auto y w_1600 la aligeran
        // sin cambiar su formato, que es lo que la galería y el mapa necesitan.
        private static Recuerdo Mapear(RecuerdoApi remoto) => new(
            remoto.Titulo,
            remoto.Fecha.Kind == DateTimeKind.Local
                ? remoto.Fecha
                : DateTime.SpecifyKind(remoto.Fecha, DateTimeKind.Utc).ToLocalTime(),
            remoto.FotoUrl.Replace("/upload/", "/upload/q_auto,w_1600/"),
            remoto.Descripcion,
            remoto.Latitud,
            remoto.Longitud,
            Id: remoto.Id.ToString());

        // La copia del teléfono es solo comodidad: si no se puede escribir, el recuerdo
        // ya está a salvo en el servidor y no hay que repetir la operación.
        private static async Task GuardarCopiaAsync()
        {
            try
            {
                Directory.CreateDirectory(Carpeta);
                await EscribirAsync(Capturados);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task EditarDetalleAsync(
            Recuerdo recuerdo, string detalle, CancellationToken ct = default)
        {
            if (!Capturados.Contains(recuerdo))
            {
                Preferences.Default.Set(ClaveDetalleEjemplo + recuerdo.Nombre, detalle);
                recuerdo.Descripcion = detalle;
                return;
            }

            // Primero el servidor; si falla, el recuerdo queda como estaba.
            if (recuerdo.Id is not null)
                await Api.ActualizarRecuerdoAsync(recuerdo.Id, recuerdo.Nombre, detalle, ct);

            // Las pantallas comparten el mismo objeto, así que todas ven el cambio.
            recuerdo.Descripcion = detalle;
            await GuardarCopiaAsync();
        }

        // El número avanza siempre: el de una foto eliminada no se vuelve a usar.
        private static string SiguienteNombre()
        {
            int numero = Preferences.Default.Get(ClaveUltimoNumero, Ejemplos.Count) + 1;
            Preferences.Default.Set(ClaveUltimoNumero, numero);
            return $"foto_{numero:0000}";
        }

        // El servidor borra el recuerdo y también su foto de Cloudinary.
        public static async Task EliminarAsync(Recuerdo recuerdo, CancellationToken ct = default)
        {
            if (!Capturados.Contains(recuerdo))
            {
                // Los ejemplos vienen en la app: solo se recuerda cuáles se quitaron.
                Preferences.Default.Set(ClaveEjemplosEliminados,
                    string.Join('|', EjemplosEliminados().Append(recuerdo.Foto).Distinct()));
                return;
            }

            if (recuerdo.Id is not null)
                await Api.EliminarRecuerdoAsync(recuerdo.Id, ct);

            Capturados.Remove(recuerdo);
            await GuardarCopiaAsync();

            // Solo las fotos locales (de antes del backend) tienen archivo en el teléfono.
            if (recuerdo.Foto.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

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
