using System.Text.Json.Nodes;
using FrontEnd.Models;
using FrontEnd.Repositories;

namespace FrontEnd.Views
{
    // El mapa es Leaflet (Resources/Raw/mapa) dentro de un HybridWebView.
    public partial class vMapa : ContentPage
    {
        private const uint DuracionAparicion = 250;
        private const float LadoMiniatura = 256;
        private const string PrefijoElegido = "elegido:";
        private const string UrlAtribucion = "https://www.openstreetmap.org/copyright";

        private readonly IReadOnlyList<Recuerdo> _recuerdos = RecuerdoRepository.ObtenerTodos();
        private bool _cerrando;

        public vMapa()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Application.Current is not null)
                Application.Current.RequestedThemeChanged += OnTemaCambiado;
        }

        protected override void OnDisappearing()
        {
            if (Application.Current is not null)
                Application.Current.RequestedThemeChanged -= OnTemaCambiado;
            base.OnDisappearing();
        }

        private void OnMensajeMapa(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
        {
            // Los mensajes pueden llegar fuera del hilo de la interfaz.
            Dispatcher.Dispatch(() => AtenderMensaje(e.Message ?? string.Empty));
        }

        private void AtenderMensaje(string mensaje)
        {
            switch (mensaje)
            {
                case "listo":
                    _ = EnviarRecuerdos();
                    _ = wvMapa.FadeToAsync(1, DuracionAparicion);
                    break;
                case "cerrar":
                    brdDetalle.IsVisible = false;
                    break;
                case "sinconexion":
                    brdSinConexion.IsVisible = true;
                    break;
                case "conexion":
                    brdSinConexion.IsVisible = false;
                    break;
                case "atribucion":
                    _ = Launcher.TryOpenAsync(UrlAtribucion);
                    break;
                default:
                    if (mensaje.StartsWith(PrefijoElegido, StringComparison.Ordinal) &&
                        int.TryParse(mensaje[PrefijoElegido.Length..], out int indice) &&
                        indice >= 0 && indice < _recuerdos.Count)
                    {
                        brdDetalle.BindingContext = _recuerdos[indice];
                        brdDetalle.IsVisible = true;
                    }
                    break;
            }
        }

        private async Task EnviarRecuerdos()
        {
            var lista = new JsonArray();
            foreach (Recuerdo recuerdo in _recuerdos)
            {
                lista.Add((JsonNode)new JsonObject
                {
                    ["texto"] = recuerdo.TextoAccesible,
                    ["foto"] = await Task.Run(() => FotoParaMapa(recuerdo.Foto)),
                    ["lat"] = recuerdo.Latitud,
                    ["lng"] = recuerdo.Longitud
                });
            }

            Enviar(new JsonObject
            {
                ["tipo"] = "recuerdos",
                ["oscuro"] = EsOscuro(),
                ["recuerdos"] = lista
            });
        }

        // FrontEnd.csproj copia las fotos de ejemplo a mapa/fotos. El mapa no puede leer
        // las capturadas, así que se le mandan como miniaturas dentro del mensaje.
        private static string FotoParaMapa(string foto)
        {
            if (foto.StartsWith("https://", StringComparison.Ordinal))
                return foto;

            if (!Path.IsPathRooted(foto))
                return $"fotos/{foto}";

            try
            {
                using FileStream archivo = File.OpenRead(foto);
                using Microsoft.Maui.Graphics.IImage imagen =
                    Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(archivo);
                using Microsoft.Maui.Graphics.IImage miniatura = imagen.Downsize(LadoMiniatura);
                return $"data:image/jpeg;base64,{miniatura.AsBase64(ImageFormat.Jpeg, 0.8f)}";
            }
            catch (Exception ex)
            {
                // Una foto ilegible no debe impedir que el mapa muestre las demás.
                System.Diagnostics.Debug.WriteLine(ex);
                return string.Empty;
            }
        }

        private void Enviar(JsonObject mensaje) => wvMapa.SendRawMessage(mensaje.ToJsonString());

        private static bool EsOscuro() => Application.Current?.RequestedTheme == AppTheme.Dark;

        private void OnTemaCambiado(object? sender, AppThemeChangedEventArgs e) =>
            Enviar(new JsonObject { ["tipo"] = "tema", ["oscuro"] = EsOscuro() });

        private void OnReintentarClicked(object? sender, EventArgs e) =>
            Enviar(new JsonObject { ["tipo"] = "reintentar" });

        private void OnCerrarDetalleTapped(object? sender, TappedEventArgs e)
        {
            brdDetalle.IsVisible = false;
            Enviar(new JsonObject { ["tipo"] = "quitar" });
        }

        private async void OnVolverTapped(object? sender, TappedEventArgs e) => await Volver();

        protected override bool OnBackButtonPressed()
        {
            _ = Volver();
            return true;
        }

        // Sin animación de Shell: vInicio anima la salida del panel hacia la derecha.
        private async Task Volver()
        {
            if (_cerrando)
                return;
            _cerrando = true;
            await Navigation.PopAsync(animated: false);
        }
    }
}
