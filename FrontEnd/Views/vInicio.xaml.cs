namespace FrontEnd.Views
{
    public partial class vInicio : ContentPage
    {
        private const double OpacidadVelo = 0.4;
        private const uint DuracionMenu = 200;
        private const double DesplazamientoMenu = -16;

        private readonly List<RecuerdoDemo> _recuerdos = new()
        {
            new("Atardecer en la costa", "18 nov 2026", "recuerdo_costa.svg",
                "Una tarde tranquila frente al mar.", "19.4326, -99.1332"),
            new("Mirador de montaña", "02 nov 2026", "recuerdo_montana.svg",
                "Un camino entre montañas y un paisaje inolvidable.", "19.4326, -99.1332"),
            new("Centro histórico", "20 oct 2026", "recuerdo_ciudad.svg",
                "Un paseo por las calles del centro.", "19.4326, -99.1332")
        };

        private bool _menuAbierto;

        public vInicio()
        {
            InitializeComponent();
            cvRecuerdos.ItemsSource = _recuerdos;
            ActualizarConteo(_recuerdos.Count);
        }

        private void OnBusquedaTextoCambiado(object? sender, TextChangedEventArgs e)
        {
            string busqueda = e.NewTextValue?.Trim() ?? string.Empty;
            List<RecuerdoDemo> filtrados = string.IsNullOrEmpty(busqueda)
                ? _recuerdos
                : _recuerdos.Where(recuerdo =>
                    recuerdo.Titulo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    recuerdo.FechaTexto.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();

            cvRecuerdos.ItemsSource = filtrados;
            cvRecuerdos.IsVisible = filtrados.Count > 0;
            stkEstadoVacio.IsVisible = filtrados.Count == 0;
            ActualizarConteo(filtrados.Count);

            if (filtrados.Count == 0 && !string.IsNullOrEmpty(busqueda))
            {
                lblEstadoVacio.Text = "No encontramos recuerdos con esa búsqueda";
                btnEstadoVacio.Text = "Limpiar búsqueda";
            }
            else if (filtrados.Count == 0)
            {
                lblEstadoVacio.Text = "Aún no tienes recuerdos guardados";
                btnEstadoVacio.Text = "Capturar recuerdo";
            }
        }

        private void ActualizarConteo(int cantidad)
        {
            lblConteo.Text = cantidad == 1 ? "1 recuerdo" : $"{cantidad} recuerdos";
        }

        private async void OnRecuerdoSeleccionado(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not RecuerdoDemo recuerdo)
                return;

            cvRecuerdos.SelectedItem = null;
            await DisplayAlertAsync(recuerdo.Titulo,
                $"{recuerdo.Descripcion}\n\nFecha: {recuerdo.FechaTexto}\nCoordenadas: {recuerdo.Coordenadas}",
                "Cerrar");
        }

        private void OnAccionEstadoVacioClicked(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                txtBusqueda.Text = string.Empty;
                return;
            }

            OnCapturarClicked(sender, e);
        }

        private async void OnCapturarClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Capturar recuerdo",
                "La captura con la cámara se conectará en una siguiente etapa.",
                "Entendido");
        }

        private sealed record RecuerdoDemo(
            string Titulo,
            string FechaTexto,
            string Foto,
            string Descripcion,
            string Coordenadas);

        private async void OnAvatarTapped(object? sender, TappedEventArgs e)
        {
            await AbrirMenu();
        }

        private async void OnCerrarMenuTapped(object? sender, TappedEventArgs e)
        {
            await CerrarMenu();
        }

        private async void OnCambiarContrasenaTapped(object? sender, TappedEventArgs e)
        {
            await CerrarMenu();
            await Navigation.PushAsync(new vCambiarContrasena(requiereContrasenaActual: true));
        }

        private void OnCerrarSesionTapped(object? sender, TappedEventArgs e)
        {
            // TODO: borrar la sesión guardada cuando exista el backend.
            App.CambiarRaiz(sesionIniciada: false);
        }

        // Con el menú abierto, el botón Atrás de Android lo cierra en vez de salir.
        protected override bool OnBackButtonPressed()
        {
            if (_menuAbierto)
            {
                _ = CerrarMenu();
                return true;
            }
            return base.OnBackButtonPressed();
        }

        // La tarjeta baja un poco mientras aparece y el velo se oscurece al mismo ritmo.
        private async Task AbrirMenu()
        {
            if (_menuAbierto)
                return;
            _menuAbierto = true;

            bxVelo.Opacity = 0;
            brdMenu.Opacity = 0;
            brdMenu.TranslationY = DesplazamientoMenu;
            grdMenu.IsVisible = true;

            await AnimarMenu(abrir: true);
        }

        private async Task CerrarMenu()
        {
            if (!_menuAbierto)
                return;
            _menuAbierto = false;

            await AnimarMenu(abrir: false);

            grdMenu.IsVisible = false;
        }

        private Task AnimarMenu(bool abrir)
        {
            var terminado = new TaskCompletionSource();
            double desde = abrir ? 0 : 1;
            double hasta = abrir ? 1 : 0;

            var animacion = new Animation(avance =>
            {
                bxVelo.Opacity = avance * OpacidadVelo;
                brdMenu.Opacity = avance;
                brdMenu.TranslationY = (1 - avance) * DesplazamientoMenu;
            }, desde, hasta);

            animacion.Commit(this, "MenuCuenta", length: DuracionMenu,
                easing: abrir ? Easing.CubicOut : Easing.CubicIn,
                finished: (_, _) => terminado.TrySetResult());

            return terminado.Task;
        }
    }
}
