namespace FrontEnd.Views
{
    public partial class vInicio : ContentPage
    {
        private const double OpacidadVelo = 0.4;
        private const uint DuracionMenu = 200;
        private const double DesplazamientoMenu = -16;

        private bool _menuAbierto;

        public vInicio()
        {
            InitializeComponent();

            // TODO: cuando exista el backend, poner aquí la foto y los datos del
            // usuario. Mientras imgAvatar no tenga Source se ve la silueta, y los
            // datos muestran "—".
            // imgAvatar.Source = imgAvatarMenu.Source = ImageSource.FromUri(new Uri(urlFoto));
            // lblNombreUsuario.Text = $"Hola, {nombre}";
            // lblNombre.Text = nombre;
            // lblApellido.Text = apellido;
            // lblCorreo.Text = correo;
        }

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
