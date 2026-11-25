using System.Net;

namespace FrontEnd.Utils
{
    // Toda llamada al backend pasa por aquí: si falta internet o el servidor no
    // responde, se avisa y se puede reintentar, y la app no se cierra.
    public static class Conexion
    {
        private static readonly TimeSpan Espera = TimeSpan.FromSeconds(20);

        // Avisa y deja reintentar hasta que haya internet. Devuelve false si el usuario cancela.
        public static async Task<bool> Comprobar(Page pagina, string cancelar = "Cancelar")
        {
            while (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                if (!await pagina.DisplayAlertAsync("Sin conexión a internet",
                        "Revisa tu Wi-Fi o tus datos móviles e inténtalo de nuevo.", "Reintentar", cancelar))
                    return false;
            }
            return true;
        }

        // Devuelve true si la operación terminó y la página sigue en pantalla.
        // La operación debe pasar el token a HttpClient y atender las respuestas
        // propias de su pantalla (por ejemplo, credenciales incorrectas).
        public static async Task<bool> Ejecutar(Page pagina, Button boton, Func<CancellationToken, Task> operacion)
        {
            // Evita enviar dos veces mientras se espera la respuesta.
            if (!boton.IsEnabled)
                return false;

            string textoOriginal = boton.Text;
            boton.IsEnabled = false;
            boton.Text = "Espera un momento…";
            try
            {
                return await Ejecutar(pagina, operacion);
            }
            finally
            {
                boton.Text = textoOriginal;
                boton.IsEnabled = true;
            }
        }

        // Para acciones sin botón propio, o cuya pantalla ya controla el suyo.
        public static async Task<bool> Ejecutar(
            Page pagina, Func<CancellationToken, Task> operacion, string cancelar = "Cancelar")
        {
            while (true)
            {
                if (!await Comprobar(pagina, cancelar))
                    return false;

                (string Titulo, string Texto)? problema = await Intentar(operacion);

                // El usuario pudo salir de la página mientras esperaba.
                if (!EstaVisible(pagina))
                    return false;
                if (problema is not { } aviso)
                    return true;
                if (!await pagina.DisplayAlertAsync(aviso.Titulo, aviso.Texto, "Reintentar", cancelar))
                    return false;
            }
        }

        // Devuelve null si todo salió bien.
        private static async Task<(string, string)?> Intentar(Func<CancellationToken, Task> operacion)
        {
            using var limite = new CancellationTokenSource(Espera);
            try
            {
                await operacion(limite.Token);
                return null;
            }
            catch (OperationCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ("El servidor tarda en responder", "Tu conexión puede estar lenta. Inténtalo de nuevo.");
            }
            catch (Exception ex) when (ex is HttpRequestException { StatusCode: null or >= HttpStatusCode.InternalServerError } or HttpIOException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ("No se pudo conectar", "El servidor no está disponible en este momento. Inténtalo de nuevo.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ("Algo salió mal", "Ocurrió un problema inesperado. Inténtalo de nuevo.");
            }
        }

        private static bool EstaVisible(Page pagina) => Shell.Current?.CurrentPage == pagina;
    }
}
