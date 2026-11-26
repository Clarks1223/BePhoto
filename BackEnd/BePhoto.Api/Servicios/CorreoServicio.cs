using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BePhoto.Api.Configuracion;

namespace BePhoto.Api.Servicios
{
    // Envío de correos. Hoy se hace con Resend (https://resend.com/docs/api-reference/emails/send-email).
    public interface ICorreoServicio
    {
        Task EnviarCodigoRecuperacionAsync(string destino, string nombre, string codigo, CancellationToken ct);
    }

    public class ResendCorreoServicio(
        HttpClient http,
        ResendOpciones opciones,
        IHostEnvironment entorno,
        ILogger<ResendCorreoServicio> log) : ICorreoServicio
    {
        private const string Url = "https://api.resend.com/emails";

        public async Task EnviarCodigoRecuperacionAsync(string destino, string nombre, string codigo, CancellationToken ct)
        {
            if (opciones.ApiKey.Length == 0)
            {
                // Sin la clave de Resend, en desarrollo el código se ve en la consola de la API
                // para poder probar el flujo completo. Fuera de desarrollo es un error.
                if (entorno.IsDevelopment())
                {
                    log.LogWarning("Resend:ApiKey no está configurada. Código de recuperación para {Correo}: {Codigo}",
                        destino, codigo);
                    return;
                }
                throw new InvalidOperationException("Falta configurar Resend:ApiKey.");
            }

            // Con una cuenta de Resend sin dominio verificado solo se puede escribir al dueño de
            // la cuenta. DestinoPruebas manda todos los códigos a ese correo para poder probar
            // con cuentas de mentira; el asunto dice a quién iban dirigidos.
            bool redirigido = opciones.DestinoPruebas.Length > 0
                && !string.Equals(opciones.DestinoPruebas, destino, StringComparison.OrdinalIgnoreCase);
            string para = redirigido ? opciones.DestinoPruebas : destino;
            string asunto = redirigido
                ? $"Código de BePhoto para {destino}"
                : "Tu código para recuperar tu contraseña de BePhoto";

            string nombreHtml = WebUtility.HtmlEncode(nombre);
            string html = $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;max-width:420px;margin:auto;color:#1f2937">
                  <h2 style="margin:0 0 12px">Recupera tu contraseña</h2>
                  <p>Hola {nombreHtml}, usa este código en la app de BePhoto:</p>
                  <p style="font-size:34px;letter-spacing:8px;font-weight:700;margin:20px 0">{codigo}</p>
                  <p>Vale 15 minutos y solo se puede usar una vez.</p>
                  <p style="color:#6b7280;font-size:13px">Si no lo pediste tú, ignora este correo: tu contraseña no cambia.</p>
                </div>
                """;
            string texto =
                $"Hola {nombre}, tu código para recuperar tu contraseña de BePhoto es {codigo}. " +
                "Vale 15 minutos y solo se puede usar una vez. Si no lo pediste, ignora este correo.";

            using var peticion = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = JsonContent.Create(new
                {
                    from = opciones.Remitente,
                    to = new[] { para },
                    subject = asunto,
                    html,
                    text = texto
                })
            };
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opciones.ApiKey);

            using HttpResponseMessage respuesta = await http.SendAsync(peticion, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                string detalle = await respuesta.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Resend respondió {(int)respuesta.StatusCode}: {detalle}");
            }
        }
    }
}
