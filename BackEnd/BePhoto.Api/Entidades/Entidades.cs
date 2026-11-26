namespace BePhoto.Api.Entidades
{
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public List<Recuerdo> Recuerdos { get; set; } = new();
    }

    // Código de 6 dígitos para recuperar la contraseña. Se guarda cifrado (hash), vale 15 minutos
    // y solo se puede intentar unas pocas veces.
    public class CodigoRestablecimiento
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string CodigoHash { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
        public int Intentos { get; set; }
        public bool Usado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    public class Recuerdo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // Foto en Cloudinary: la URL para mostrarla y el id público para poder borrarla.
        public string FotoUrl { get; set; } = string.Empty;
        public string FotoPublicId { get; set; } = string.Empty;

        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double? Altitud { get; set; }

        // Cuándo se tomó la foto (siempre en UTC) y cuándo se registró en el servidor.
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
