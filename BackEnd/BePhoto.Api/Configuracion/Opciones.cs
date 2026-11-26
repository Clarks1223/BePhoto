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

    public class CloudinaryOpciones
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;

        // Prefijo con el que se nombran las fotos en Cloudinary.
        public string Carpeta { get; set; } = "BePhoto";
    }
}
