using System.Globalization;
using System.Text.Json.Serialization;

namespace FrontEnd.Models
{
    public sealed record Recuerdo(
        // Secuencial (foto_0001) e interno: no se muestra ni se edita.
        string Nombre,
        DateTime Fecha,
        // URL de Cloudinary (o una ruta local si es un ejemplo de la app).
        string Foto,
        string Descripcion,
        double Latitud,
        double Longitud,
        // Margen de error del GPS en metros; null si no se conoce.
        double? PrecisionMetros = null,
        // Id del recuerdo en el servidor; null si solo existe en este teléfono (los ejemplos).
        string? Id = null)
    {
        // Abreviaturas fijas para que la fecha se vea igual en todos los dispositivos.
        private static readonly string[] Meses =
            ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

        public string Descripcion { get; set; } = Descripcion;

        [JsonIgnore]
        public string DetalleTexto => string.IsNullOrWhiteSpace(Descripcion) ? "Sin detalle" : Descripcion;

        [JsonIgnore]
        public string TextoAccesible => $"Foto del {FechaTexto}";

        [JsonIgnore]
        public string FechaTexto => $"{Fecha.Day:00} {Meses[Fecha.Month - 1]} {Fecha.Year}";

        [JsonIgnore]
        public string Coordenadas => string.Create(CultureInfo.InvariantCulture,
            $"{Latitud:0.000000}, {Longitud:0.000000}");
    }
}
