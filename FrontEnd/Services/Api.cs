using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FrontEnd.Repositories;

namespace FrontEnd.Services
{
    // Error que el servidor explica con un mensaje listo para mostrar (400, 401, 404, 409).
    // Los errores 5xx y la falta de red no usan esta clase: Conexion ya los trata como "reintentar".
    public sealed class ApiException(string mensaje, HttpStatusCode estado) : Exception(mensaje)
    {
        public HttpStatusCode Estado { get; } = estado;
    }

    // Toda la comunicación con el backend (BackEnd/BePhoto.Api). Las pantallas llaman
    // aquí dentro de Conexion.Ejecutar, que ya se encarga de la espera y los reintentos.
    public static class Api
    {
        // Emulador de Android: 10.0.2.2 es el PC. En un celular real, usa la IP del PC
        // (por ejemplo http://192.168.1.20:5080) o "adb reverse tcp:5080 tcp:5080" con localhost.
        private const string UrlAndroid = "http://10.0.2.2:5080";
        private const string UrlOtros = "http://localhost:5080";

        private const string ClaveSesion = "sesion_activa";
        private const string ClaveToken = "token_api";

        private static readonly HttpClient Cliente = new()
        {
            BaseAddress = new Uri(
#if ANDROID
                UrlAndroid
#else
                UrlOtros
#endif
            ),
            // El límite real lo pone Conexion con su CancellationToken.
            Timeout = Timeout.InfiniteTimeSpan
        };

        private static string? _token;

        // Se guarda aparte del token (que va cifrado y es asíncrono) para poder elegir
        // la pantalla inicial al arrancar sin esperar.
        public static bool HaySesion => Preferences.Default.Get(ClaveSesion, false);

        // ---------- Cuenta ----------

        public static async Task IniciarSesionAsync(string correo, string contrasena, CancellationToken ct)
        {
            using HttpResponseMessage respuesta = await EnviarAsync(HttpMethod.Post, "api/auth/login",
                Json(new LoginPeticion(correo.Trim(), contrasena), ApiJson.Default.LoginPeticion), false, ct);
            await GuardarSesionAsync(await LeerAsync(respuesta, ApiJson.Default.AuthRespuesta, ct));
        }

        // La API deja la cuenta lista para usarse: devuelve el token igual que el login.
        public static async Task RegistrarAsync(
            string nombre, string apellido, string correo, string contrasena, CancellationToken ct)
        {
            using HttpResponseMessage respuesta = await EnviarAsync(HttpMethod.Post, "api/auth/registro",
                Json(new RegistroPeticion(nombre.Trim(), apellido.Trim(), correo.Trim(), contrasena),
                    ApiJson.Default.RegistroPeticion), false, ct);
            await GuardarSesionAsync(await LeerAsync(respuesta, ApiJson.Default.AuthRespuesta, ct));
        }

        public static async Task CambiarContrasenaAsync(string actual, string nueva, CancellationToken ct)
        {
            using HttpResponseMessage _ = await EnviarAsync(HttpMethod.Post, "api/auth/cambiar-contrasena",
                Json(new CambiarContrasenaPeticion(actual, nueva), ApiJson.Default.CambiarContrasenaPeticion),
                true, ct);
        }

        // Borra la sesión y los recuerdos del usuario guardados en el teléfono.
        public static void CerrarSesion()
        {
            _token = null;
            Preferences.Default.Remove(ClaveSesion);
            try
            {
                SecureStorage.Default.Remove(ClaveToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            RecuerdoRepository.LimpiarSesion();
        }

        // ---------- Recuerdos ----------

        public static async Task<List<RecuerdoApi>> ListarRecuerdosAsync(CancellationToken ct)
        {
            using HttpResponseMessage respuesta = await EnviarAsync(HttpMethod.Get, "api/recuerdos", null, true, ct);
            return await LeerAsync(respuesta, ApiJson.Default.ListaRecuerdoApi, ct);
        }

        // Sube la foto (el servidor la manda a Cloudinary) y guarda título, fecha y coordenadas.
        public static async Task<RecuerdoApi> CrearRecuerdoAsync(
            FileResult foto, string titulo, double latitud, double longitud, double? altitud,
            DateTime fecha, CancellationToken ct)
        {
            // Se abre en cada intento: si Conexion reintenta, el flujo anterior ya se consumió.
            await using Stream flujo = await foto.OpenReadAsync();

            using var contenido = new MultipartFormDataContent();
            var archivo = new StreamContent(flujo);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrEmpty(foto.ContentType) ? "image/jpeg" : foto.ContentType);
            contenido.Add(archivo, "foto", string.IsNullOrEmpty(foto.FileName) ? "foto.jpg" : foto.FileName);
            contenido.Add(new StringContent(titulo), "titulo");
            // Con punto decimal siempre, sin importar el idioma del teléfono.
            contenido.Add(new StringContent(Numero(latitud)), "latitud");
            contenido.Add(new StringContent(Numero(longitud)), "longitud");
            if (altitud is double metros)
                contenido.Add(new StringContent(Numero(metros)), "altitud");
            // Con zona horaria (-05:00): el servidor la guarda en UTC.
            contenido.Add(new StringContent(
                new DateTimeOffset(fecha).ToString("o", CultureInfo.InvariantCulture)), "fecha");

            using HttpResponseMessage respuesta =
                await EnviarAsync(HttpMethod.Post, "api/recuerdos", contenido, true, ct);
            return await LeerAsync(respuesta, ApiJson.Default.RecuerdoApi, ct);
        }

        public static async Task ActualizarRecuerdoAsync(string id, string titulo, string descripcion, CancellationToken ct)
        {
            using HttpResponseMessage _ = await EnviarAsync(HttpMethod.Put, $"api/recuerdos/{id}",
                Json(new ActualizarRecuerdoPeticion(titulo, descripcion), ApiJson.Default.ActualizarRecuerdoPeticion),
                true, ct);
        }

        public static async Task EliminarRecuerdoAsync(string id, CancellationToken ct)
        {
            try
            {
                using HttpResponseMessage _ = await EnviarAsync(HttpMethod.Delete, $"api/recuerdos/{id}", null, true, ct);
            }
            catch (ApiException ex) when (ex.Estado == HttpStatusCode.NotFound)
            {
                // Ya no existe en el servidor: el resultado es el mismo que se buscaba.
            }
        }

        // ---------- Internos ----------

        private static async Task<HttpResponseMessage> EnviarAsync(
            HttpMethod metodo, string ruta, HttpContent? contenido, bool autenticada, CancellationToken ct)
        {
            using var peticion = new HttpRequestMessage(metodo, ruta) { Content = contenido };

            if (autenticada)
            {
                string? token = await ObtenerTokenAsync();
                if (token is null)
                {
                    SesionVencida();
                    throw new ApiException("Tu sesión venció. Inicia sesión de nuevo.", HttpStatusCode.Unauthorized);
                }
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage respuesta = await Cliente.SendAsync(peticion, ct);
            if (respuesta.IsSuccessStatusCode)
                return respuesta;

            using (respuesta)
            {
                string detalle = await LeerDetalleAsync(respuesta, ct);

                // 5xx (por ejemplo 502 si Cloudinary falla): Conexion lo ofrece como "reintentar".
                if ((int)respuesta.StatusCode >= 500)
                    throw new HttpRequestException(detalle, null, respuesta.StatusCode);

                // Token vencido o inválido en una llamada que lo necesita.
                if (autenticada && respuesta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SesionVencida();
                    detalle = "Tu sesión venció. Inicia sesión de nuevo.";
                }
                throw new ApiException(detalle, respuesta.StatusCode);
            }
        }

        // La API responde los errores como {"title": ..., "status": ..., "detail": "mensaje"}.
        private static async Task<string> LeerDetalleAsync(HttpResponseMessage respuesta, CancellationToken ct)
        {
            try
            {
                await using Stream flujo = await respuesta.Content.ReadAsStreamAsync(ct);
                ProblemaApi? problema = await JsonSerializer.DeserializeAsync(flujo, ApiJson.Default.ProblemaApi, ct);
                if (!string.IsNullOrWhiteSpace(problema?.Detail))
                    return problema.Detail;
            }
            catch (JsonException)
            {
                // La respuesta no era JSON (por ejemplo, la página de error de un proxy).
            }
            return $"El servidor respondió con el error {(int)respuesta.StatusCode}.";
        }

        private static async Task<T> LeerAsync<T>(
            HttpResponseMessage respuesta, JsonTypeInfo<T> tipo, CancellationToken ct)
        {
            await using Stream flujo = await respuesta.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync(flujo, tipo, ct)
                ?? throw new HttpRequestException("El servidor envió una respuesta vacía.");
        }

        private static StringContent Json<T>(T datos, JsonTypeInfo<T> tipo) =>
            new(JsonSerializer.Serialize(datos, tipo), Encoding.UTF8, "application/json");

        private static string Numero(double valor) => valor.ToString("R", CultureInfo.InvariantCulture);

        private static async Task GuardarSesionAsync(AuthRespuesta respuesta)
        {
            // Primero la sesión nueva: si otra cuenta usó antes este teléfono, no se mezclan sus recuerdos.
            RecuerdoRepository.LimpiarSesion();

            _token = respuesta.Token;
            try
            {
                await SecureStorage.Default.SetAsync(ClaveToken, respuesta.Token);
                Preferences.Default.Set(ClaveSesion, true);
            }
            catch (Exception ex)
            {
                // Sin almacenamiento seguro la sesión dura hasta cerrar la app.
                System.Diagnostics.Debug.WriteLine(ex);
                Preferences.Default.Remove(ClaveSesion);
            }
        }

        private static async Task<string?> ObtenerTokenAsync()
        {
            if (_token is not null)
                return _token;
            try
            {
                _token = await SecureStorage.Default.GetAsync(ClaveToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return _token;
        }

        // El token ya no sirve: se borra todo y la app vuelve al login.
        private static void SesionVencida()
        {
            CerrarSesion();
            MainThread.BeginInvokeOnMainThread(() => App.CambiarRaiz(sesionIniciada: false));
        }
    }

    // ---------- Datos que viajan por la API (los mismos nombres que BackEnd/.../Dtos.cs) ----------

    public sealed record LoginPeticion(string Correo, string Contrasena);

    public sealed record RegistroPeticion(string Nombre, string Apellido, string Correo, string Contrasena);

    public sealed record CambiarContrasenaPeticion(string ContrasenaActual, string NuevaContrasena);

    public sealed record ActualizarRecuerdoPeticion(string Titulo, string Descripcion);

    public sealed record UsuarioApi(Guid Id, string Nombre, string Apellido, string Correo);

    public sealed record AuthRespuesta(string Token, DateTime Expira, UsuarioApi Usuario);

    public sealed record RecuerdoApi(
        Guid Id,
        string Titulo,
        string Descripcion,
        string FotoUrl,
        string MiniaturaUrl,
        double Latitud,
        double Longitud,
        double? Altitud,
        DateTime Fecha);

    public sealed record ProblemaApi(string? Detail);

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(LoginPeticion))]
    [JsonSerializable(typeof(RegistroPeticion))]
    [JsonSerializable(typeof(CambiarContrasenaPeticion))]
    [JsonSerializable(typeof(ActualizarRecuerdoPeticion))]
    [JsonSerializable(typeof(AuthRespuesta))]
    [JsonSerializable(typeof(RecuerdoApi))]
    [JsonSerializable(typeof(List<RecuerdoApi>), TypeInfoPropertyName = "ListaRecuerdoApi")]
    [JsonSerializable(typeof(ProblemaApi))]
    internal sealed partial class ApiJson : JsonSerializerContext
    {
    }
}
