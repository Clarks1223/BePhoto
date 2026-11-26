namespace BePhoto.Api.Configuracion
{
    public class JwtOpciones
    {
        // Clave con la que se firman los tokens (mínimo 32 caracteres). Va en user-secrets.
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "BePhoto";
        public string Audience { get; set; } = "BePhoto.App";
        public int DiasValidez { get; set; } = 30;
    }

    public class ResendOpciones
    {
        // Clave de Resend (empieza con "re_"). Va en user-secrets o en la variable RESEND_API_KEY.
        // Si falta, en desarrollo el código de recuperación se muestra en la consola de la API.
        public string ApiKey { get; set; } = string.Empty;

        // Sin dominio verificado, Resend solo deja enviar desde este remitente de pruebas.
        public string Remitente { get; set; } = "BePhoto <onboarding@resend.dev>";

        // Solo para pruebas: si tiene valor, todos los códigos se envían a este correo en vez de
        // al del usuario. Hace falta mientras la cuenta de Resend no tenga un dominio verificado,
        // porque entonces solo puede escribirle al dueño de la cuenta.
        public string DestinoPruebas { get; set; } = string.Empty;
    }

    public class CloudinaryOpciones
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;

        // Prefijo con el que se nombran las fotos en Cloudinary.
        public string Carpeta { get; set; } = "BePhoto";
    }
}
