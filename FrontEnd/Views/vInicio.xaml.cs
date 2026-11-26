using System.Collections.ObjectModel;
using FrontEnd.Models;
using FrontEnd.Repositories;
using FrontEnd.Services;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vInicio : ContentPage
    {
        private const double OpacidadVelo = 0.4;
        private const uint DuracionMapa = 300;
        private const double DistanciaAbrirMapa = 80;
        private static readonly TimeSpan EsperaUbicacion = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan EsperaAviso = TimeSpan.FromSeconds(4);
        private const uint DuracionMenu = 200;
        private const double DesplazamientoMenu = -16;

        private IReadOnlyList<Recuerdo> _recuerdos = RecuerdoRepository.ObtenerTodos();
        private ObservableCollection<Recuerdo> _visibles = [];

        private bool _menuAbierto;
        private bool _atendiendoFoto;
        private bool _capturando;
        private bool _arrastrandoMapa;
        private bool _abriendoMapa;
        private bool _mapaAbierto;
        private double _distanciaArrastre;
        private int _turnoAviso;
        private bool _sincronizando;

        public vInicio()
        {
            InitializeComponent();
            MostrarRecuerdos();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SoltarDeslizamiento();
            QuitarEliminados();
            DeslizamientoMapa.Movido += OnDeslizamientoMovido;
            DeslizamientoMapa.Terminado += OnDeslizamientoTerminado;

            // Los recuerdos del servidor se traen al entrar; sin conexión se muestra lo guardado.
            if (!RecuerdoRepository.Sincronizado && !_sincronizando)
                _ = SincronizarRecuerdos();

            // Al volver del mapa, el panel sale por donde entró.
            if (_mapaAbierto)
            {
                _mapaAbierto = false;
                await OcultarPanelMapa();
                _abriendoMapa = false;
            }
        }

        private async Task SincronizarRecuerdos()
        {
            _sincronizando = true;
            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await RecuerdoRepository.SincronizarAsync(limite.Token);
                _recuerdos = RecuerdoRepository.ObtenerTodos();
                MostrarRecuerdos();
            }
            catch (Exception ex)
            {
                // Sin conexión no es un error para el usuario: se reintenta la próxima vez.
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _sincronizando = false;
            }
        }

        protected override void OnDisappearing()
        {
            SoltarDeslizamiento();
            base.OnDisappearing();
        }

        private void SoltarDeslizamiento()
        {
            DeslizamientoMapa.Movido -= OnDeslizamientoMovido;
            DeslizamientoMapa.Terminado -= OnDeslizamientoTerminado;
        }

        // El panel del mapa sigue al dedo desde el borde derecho.
        private void OnDeslizamientoMovido(double distancia)
        {
            if (_menuAbierto || _abriendoMapa || _capturando || _atendiendoFoto)
                return;

            if (!_arrastrandoMapa)
            {
                _arrastrandoMapa = true;
                MostrarPanelMapa();
            }
            _distanciaArrastre = distancia;
            pnlMapa.TranslationX = Math.Clamp(Width - distancia, 0, Width);
        }

        private async void OnDeslizamientoTerminado(bool completo)
        {
            if (!_arrastrandoMapa)
                return;
            _arrastrandoMapa = false;

            if (completo && _distanciaArrastre >= DistanciaAbrirMapa)
                await AbrirMapa();
            else
                await OcultarPanelMapa();
        }

        private async void OnMapaTapped(object? sender, TappedEventArgs e)
        {
            if (_abriendoMapa || _arrastrandoMapa || _capturando)
                return;
            MostrarPanelMapa();
            await AbrirMapa();
        }

        private void MostrarPanelMapa()
        {
            txtBusqueda.Unfocus();
            pnlMapa.CancelAnimations();
            pnlMapa.TranslationX = Width;
            grdPanelMapa.IsVisible = true;
        }

        // Termina de entrar el panel y deja vMapa en su lugar, sin otra animación.
        private async Task AbrirMapa()
        {
            _abriendoMapa = true;
            await pnlMapa.TranslateToAsync(0, 0, DuracionMapa, Easing.CubicOut);
            _mapaAbierto = true;
            await Navigation.PushAsync(new vMapa(), animated: false);
        }

        private async Task OcultarPanelMapa()
        {
            bool interrumpida = await pnlMapa.TranslateToAsync(Width, 0, DuracionMapa, Easing.CubicOut);
            if (!interrumpida)
                grdPanelMapa.IsVisible = false;
        }

        private void OnBusquedaTextoCambiado(object? sender, TextChangedEventArgs e) => MostrarRecuerdos();

        private void MostrarRecuerdos()
        {
            string busqueda = txtBusqueda.Text?.Trim() ?? string.Empty;
            IReadOnlyList<Recuerdo> filtrados = string.IsNullOrEmpty(busqueda)
                ? _recuerdos
                : _recuerdos.Where(recuerdo =>
                    recuerdo.Descripcion.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    recuerdo.FechaTexto.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();

            _visibles = new ObservableCollection<Recuerdo>(filtrados);
            cvRecuerdos.ItemsSource = _visibles;
            ActualizarEstado();
        }

        // Quita solo las fotos eliminadas, para no perder el lugar en la lista.
        private void QuitarEliminados()
        {
            _recuerdos = RecuerdoRepository.ObtenerTodos();
            foreach (Recuerdo eliminado in _visibles.Except(_recuerdos).ToList())
                _visibles.Remove(eliminado);
            ActualizarEstado();
        }

        private void ActualizarEstado()
        {
            string busqueda = txtBusqueda.Text?.Trim() ?? string.Empty;
            cvRecuerdos.IsVisible = _visibles.Count > 0;
            stkEstadoVacio.IsVisible = _visibles.Count == 0;
            ActualizarConteo(_visibles.Count);

            if (_visibles.Count == 0 && !string.IsNullOrEmpty(busqueda))
            {
                lblEstadoVacio.Text = "No encontramos recuerdos con esa búsqueda";
                btnEstadoVacio.Text = "Limpiar búsqueda";
            }
            else if (_visibles.Count == 0)
            {
                lblEstadoVacio.Text = "Aún no tienes recuerdos guardados";
                btnEstadoVacio.Text = "Capturar recuerdo";
            }
        }

        private void ActualizarConteo(int cantidad)
        {
            lblConteo.Text = cantidad == 1 ? "1 recuerdo" : $"{cantidad} recuerdos";
        }

        private async void OnRecuerdoTocado(object? sender, EventArgs e)
        {
            if (!PuedeAtenderFoto || (sender as View)?.BindingContext is not Recuerdo recuerdo)
                return;
            _atendiendoFoto = true;

            try
            {
                txtBusqueda.Unfocus();
                await Navigation.PushAsync(new vVisor(_visibles, recuerdo));
            }
            finally
            {
                _atendiendoFoto = false;
            }
        }

        private async void OnRecuerdoMantenido(object? sender, EventArgs e)
        {
            if (!PuedeAtenderFoto || (sender as View)?.BindingContext is not Recuerdo recuerdo)
                return;
            _atendiendoFoto = true;

            try
            {
                txtBusqueda.Unfocus();
                if (await AccionesRecuerdo.MostrarMenu(this, recuerdo))
                    QuitarEliminados();
            }
            finally
            {
                _atendiendoFoto = false;
            }
        }

        private bool PuedeAtenderFoto =>
            !_atendiendoFoto && !_menuAbierto && !_capturando && !_abriendoMapa && !_arrastrandoMapa;

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
            if (_capturando)
                return;
            _capturando = true;
            txtBusqueda.Unfocus();
            btnCapturar.IsEnabled = false;
            btnEstadoVacio.IsEnabled = false;
            string textoOriginal = btnCapturar.Text;

            try
            {
                await CapturarRecuerdo();
            }
            catch (PermissionException)
            {
                await OfrecerAjustes("BePhoto necesita permiso para usar la cámara y guardar la foto.");
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlertAsync("Cámara no disponible",
                    "No encontramos una cámara en este teléfono.", "Entendido");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlertAsync("No se pudo guardar",
                    "Ocurrió un problema al capturar el recuerdo. Inténtalo de nuevo.", "Entendido");
            }
            finally
            {
                btnCapturar.Text = textoOriginal;
                btnCapturar.IsEnabled = true;
                btnEstadoVacio.IsEnabled = true;
                _capturando = false;
            }
        }

        private async Task CapturarRecuerdo()
        {
            // La foto se subirá al servidor: sin internet no se abre la cámara.
            if (!await Conexion.Comprobar(this))
                return;

            // Los permisos se piden antes, para no perder una foto ya tomada.
            if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
            {
                await OfrecerAjustes("BePhoto necesita la cámara para tomar la foto.");
                return;
            }

            // Restricted: en Android 12+ el usuario solo dio la ubicación aproximada.
            PermissionStatus permiso = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permiso != PermissionStatus.Granted)
            {
                await OfrecerAjustes(permiso == PermissionStatus.Restricted
                    ? "Activa la ubicación precisa para guardar el lugar exacto de la foto."
                    : "BePhoto necesita tu ubicación para guardar el lugar de la foto.");
                return;
            }

            FileResult? foto = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                RotateImage = true
            });
            if (foto is null)
                return;

            DateTime fecha = DateTime.Now;
            btnCapturar.Text = "Obteniendo ubicación…";
            Location? ubicacion = await ObtenerUbicacion();
            if (ubicacion is null)
                return;

            btnCapturar.Text = "Guardando…";
            Recuerdo? recuerdo = null;
            // Subir la foto tarda más que una consulta: se le da hasta 90 s.
            await Conexion.Ejecutar(this,
                async t => recuerdo = await RecuerdoRepository.GuardarAsync(foto, ubicacion, fecha, t),
                "Descartar foto", TimeSpan.FromSeconds(90));
            if (recuerdo is null)
                return;

            _recuerdos = RecuerdoRepository.ObtenerTodos();
            if (string.IsNullOrEmpty(txtBusqueda.Text))
                MostrarRecuerdos();
            else
                txtBusqueda.Text = string.Empty;

            _ = MostrarAviso("Recuerdo guardado", AccionesRecuerdo.DetalleUbicacion(recuerdo));
        }

        // Aviso que se va solo, con una vibración corta.
        private async Task MostrarAviso(string titulo, string texto)
        {
            int turno = ++_turnoAviso;
            lblAvisoTitulo.Text = titulo;
            lblAvisoTexto.Text = texto;
            SemanticScreenReader.Announce($"{titulo}. {texto}");

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch (Exception ex)
            {
                // Sin vibración el aviso sigue siendo útil.
                System.Diagnostics.Debug.WriteLine(ex);
            }

            brdAviso.CancelAnimations();
            brdAviso.IsVisible = true;
            await brdAviso.FadeToAsync(1, DuracionMenu);
            await Task.Delay(EsperaAviso);

            // Si llegó otro aviso mientras tanto, ese se encarga de ocultarlo.
            if (turno != _turnoAviso)
                return;
            await brdAviso.FadeToAsync(0, DuracionMenu);
            if (turno == _turnoAviso)
                brdAviso.IsVisible = false;
        }

        // Insiste hasta tener una lectura del GPS o hasta que el usuario descarte la foto.
        private async Task<Location?> ObtenerUbicacion()
        {
            while (true)
            {
                string problema;
                try
                {
                    Location? ubicacion = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Best, EsperaUbicacion));
                    if (ubicacion is not null)
                        return ubicacion;
                    problema = "No recibimos señal del GPS. Prueba en un lugar más abierto.";
                }
                catch (FeatureNotEnabledException)
                {
                    if (!await OfrecerActivarUbicacion())
                        return null;
                    continue;
                }

                if (!await DisplayAlertAsync("Sin ubicación", problema, "Reintentar", "Descartar foto"))
                    return null;
            }
        }

        // Devuelve false si el usuario descarta la foto.
        private async Task<bool> OfrecerActivarUbicacion()
        {
            const string abrir = "Abrir ajustes";
            const string descartar = "Descartar foto";

            string? opcion = await DisplayActionSheetAsync(
                "La ubicación del teléfono está apagada", null, descartar, abrir, "Reintentar");

            if (opcion == abrir)
                await AjustesUbicacion.AbrirYEsperar(Window);
            return opcion != descartar;
        }

        private async Task OfrecerAjustes(string mensaje)
        {
            if (await DisplayAlertAsync("Permiso necesario", mensaje, "Abrir ajustes", "Ahora no"))
                AppInfo.Current.ShowSettingsUI();
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
            Api.CerrarSesion();
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
            // MAUI falla si la actividad se cierra con el campo enfocado.
            txtBusqueda.Unfocus();
            return base.OnBackButtonPressed();
        }

        // La tarjeta baja un poco mientras aparece y el velo se oscurece al mismo ritmo.
        private async Task AbrirMenu()
        {
            if (_menuAbierto || _capturando)
                return;
            _menuAbierto = true;

            MostrarUsuario(Api.Usuario);
            _ = ActualizarUsuario();

            bxVelo.Opacity = 0;
            brdMenu.Opacity = 0;
            brdMenu.TranslationY = DesplazamientoMenu;
            grdMenu.IsVisible = true;

            await AnimarMenu(abrir: true);
        }

        private void MostrarUsuario(UsuarioApi? usuario)
        {
            string nombre = usuario?.Nombre?.Trim() ?? string.Empty;
            string apellido = usuario?.Apellido?.Trim() ?? string.Empty;
            string correo = usuario?.Correo?.Trim() ?? string.Empty;

            lblNombre.Text = string.IsNullOrEmpty(nombre) ? "—" : nombre;
            lblApellido.Text = string.IsNullOrEmpty(apellido) ? "—" : apellido;
            lblCorreo.Text = string.IsNullOrEmpty(correo) ? "—" : correo;

            string completo = $"{nombre} {apellido}".Trim();
            lblNombreUsuario.Text = string.IsNullOrEmpty(completo) ? "Tu cuenta" : completo;
        }

        // Refresca los datos desde el servidor; sin conexión se quedan los guardados.
        private async Task ActualizarUsuario()
        {
            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                UsuarioApi usuario = await Api.ObtenerPerfilAsync(limite.Token);
                MostrarUsuario(usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
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
