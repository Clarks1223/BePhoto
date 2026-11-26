using System.ComponentModel.DataAnnotations;
using BePhoto.Api.Entidades;

namespace BePhoto.Api.Dtos
{
    // ---------- Entrada: cuenta ----------

    public class RegistroRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, ErrorMessage = "El apellido puede tener hasta 50 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(100, ErrorMessage = "El correo puede tener hasta 100 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Ingresa tu correo.")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        [StringLength(50)]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class CambiarContrasenaRequest
    {
        [Required(ErrorMessage = "Escribe tu contraseña actual.")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la nueva contraseña.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        public string NuevaContrasena { get; set; } = string.Empty;
    }

    // Paso 1 de recuperar la contraseña: se envía un código al correo.
    public class OlvideContrasenaRequest
    {
        [Required(ErrorMessage = "Escribe tu correo.")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;
    }

    // Paso 2: el código recibido y la nueva contraseña.
    public class RestablecerContrasenaRequest
    {
        [Required(ErrorMessage = "Escribe tu correo.")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe el código que te llegó al correo.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 números.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la nueva contraseña.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        public string NuevaContrasena { get; set; } = string.Empty;
    }

    // ---------- Entrada: recuerdos ----------

    // Se envía como multipart/form-data porque lleva la foto.
    public class CrearRecuerdoRequest
    {
        public IFormFile? Foto { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(60, ErrorMessage = "El título puede tener hasta 60 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción puede tener hasta 300 caracteres.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "Falta la latitud.")]
        [Range(-90.0, 90.0, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
        public double? Latitud { get; set; }

        [Required(ErrorMessage = "Falta la longitud.")]
        [Range(-180.0, 180.0, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
        public double? Longitud { get; set; }

        public double? Altitud { get; set; }

        // Fecha en que se tomó la foto, con zona horaria (por ejemplo 2026-11-24T20:30:00-05:00).
        // Si no se envía, se usa la hora actual del servidor.
        public DateTimeOffset? Fecha { get; set; }
    }

    public class ActualizarRecuerdoRequest
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(60, ErrorMessage = "El título puede tener hasta 60 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción puede tener hasta 300 caracteres.")]
        public string? Descripcion { get; set; }
    }

    // ---------- Salida ----------

    public record UsuarioDto(Guid Id, string Nombre, string Apellido, string Correo);

    public record AuthRespuesta(string Token, DateTime Expira, UsuarioDto Usuario);

    public record PerfilDto(UsuarioDto Usuario, int TotalRecuerdos, int TotalUbicaciones);

    public record RecuerdoDto(
        Guid Id,
        string Titulo,
        string Descripcion,
        string FotoUrl,
        string MiniaturaUrl,
        double Latitud,
        double Longitud,
        double? Altitud,
        DateTime Fecha)
    {
        public static RecuerdoDto Desde(Recuerdo r) => new(
            r.Id,
            r.Titulo,
            r.Descripcion,
            r.FotoUrl,
            Miniatura(r.FotoUrl),
            r.Latitud,
            r.Longitud,
            r.Altitud,
            r.Fecha);

        // Cloudinary entrega la misma foto reducida si se agrega una transformación a la URL.
        private static string Miniatura(string url) =>
            url.Replace("/upload/", "/upload/c_fill,w_400,h_300,q_auto,f_auto/");
    }
}
