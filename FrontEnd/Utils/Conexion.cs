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
                while (true)
                {
                    if (!await Comprobar(pagina))
                        return false;

                    (string Titulo, string Texto)? problema = await Intentar(operacion);

                    // El usuario pudo salir de la página mientras esperaba.
                    if (!EstaVisible(pagina))
                        return false;
                    if (problema is not { } aviso)
                        return true;
                    if (!await pagina.DisplayAlertAsync(aviso.Titulo, aviso.Texto, "Reintentar", "Cancelar"))
                        return false;
                }
            }
            finally
            {
                boton.Text = textoOriginal;
                boton.IsEnabled = true;
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
            catch (Exception ex) when (ex is HttpRequestException { StatusCode: null } or IOException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ("No se pudo conectar", "No logramos comunicarnos con el servidor. Revisa tu conexión e inténtalo de nuevo.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ("Algo salió mal", "Ocurrió un problema con el servidor. Inténtalo de nuevo más tarde.");
            }
        }

        private static bool EstaVisible(Page pagina) => Shell.Current?.CurrentPage == pagina;
    }
}
