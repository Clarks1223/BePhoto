using System.Text.RegularExpressions;

namespace FrontEnd.Utils
{
    // Reglas de los campos. Reciben el texto ya sin espacios a los lados y
    // devuelven el mensaje de error, o null si el valor es válido.
    public static class Reglas
    {
        public const int MinTexto = 3;
        public const int MaxTexto = 300;
        public const int MinContrasena = 8;

        // algo@dominio.ext, sin espacios y con una sola @.
        private static readonly Regex FormatoCorreo =
            new(@"^[^\s@]+@[^\s@.]+(\.[^\s@.]+)+$", RegexOptions.CultureInvariant);

        public static Func<string, string?> Texto(string campo) => valor =>
            valor.Length is < MinTexto or > MaxTexto
                ? $"{campo} debe tener entre {MinTexto} y {MaxTexto} caracteres."
                : null;

        public static Func<string, string?> Obligatorio(string mensaje) => valor =>
            valor.Length == 0 ? mensaje : null;

        public static Func<string, string?> Coincide(Func<string> contrasena) => valor =>
            valor != contrasena() ? "Las contraseñas no coinciden." : null;

        public static string? Correo(string valor)
        {
            if (valor.Length == 0)
                return "Escribe tu correo electrónico.";
            return FormatoCorreo.IsMatch(valor)
                ? null
                : "Escribe un correo válido, por ejemplo tu@correo.com.";
        }

        public static readonly IReadOnlyList<(string Texto, Func<string, bool> Cumple)> RequisitosContrasena =
        [
            ($"al menos {MinContrasena} caracteres", valor => valor.Length >= MinContrasena),
            ("una mayúscula", valor => valor.Any(char.IsUpper)),
            ("una minúscula", valor => valor.Any(char.IsLower)),
            ("un carácter especial", valor => valor.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))),
        ];

        public static string? Contrasena(string valor)
        {
            foreach ((string texto, Func<string, bool> cumple) in RequisitosContrasena)
            {
                if (!cumple(valor))
                    return $"La contraseña debe tener {texto}.";
            }
            return null;
        }
    }
}
