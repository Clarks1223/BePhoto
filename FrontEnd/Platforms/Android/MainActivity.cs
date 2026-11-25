using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using FrontEnd.Utils;

namespace FrontEnd
{
    // AdjustResize: al abrir el teclado la ventana se achica en vez de desplazarse,
    // así el ScrollView puede subir el botón principal por encima del teclado.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = Android.Views.SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const float InicioDeslizamiento = 16; // dp

        private float _inicioX;
        private float _inicioY;
        private bool _puedeDeslizar;
        private bool _deslizando;

        // Al reconocer el deslizamiento se cancela el toque en los controles, para
        // que soltar el dedo sobre una tarjeta o un botón no cuente como pulsación.
        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (e is null)
                return base.DispatchTouchEvent(e);

            float densidad = Resources?.DisplayMetrics?.Density ?? 1;
            float distancia = (_inicioX - e.GetX()) / densidad;

            if (_deslizando)
            {
                switch (e.ActionMasked)
                {
                    case MotionEventActions.Move:
                        DeslizamientoMapa.NotificarMovimiento(distancia);
                        break;
                    case MotionEventActions.Up:
                    case MotionEventActions.Cancel:
                        _deslizando = false;
                        DeslizamientoMapa.NotificarFin(e.ActionMasked == MotionEventActions.Up);
                        break;
                }
                return true;
            }

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _inicioX = e.GetX();
                    _inicioY = e.GetY();
                    _puedeDeslizar = DeslizamientoMapa.HayOyentes;
                    break;

                case MotionEventActions.PointerDown:
                    _puedeDeslizar = false;
                    break;

                case MotionEventActions.Move when _puedeDeslizar:
                    float vertical = Math.Abs(e.GetY() - _inicioY) / densidad;

                    if (distancia >= InicioDeslizamiento && distancia > vertical * 1.5f)
                    {
                        _puedeDeslizar = false;
                        _deslizando = true;

                        MotionEvent? cancelar = MotionEvent.Obtain(e);
                        if (cancelar is not null)
                        {
                            cancelar.Action = MotionEventActions.Cancel;
                            base.DispatchTouchEvent(cancelar);
                            cancelar.Recycle();
                        }
                        DeslizamientoMapa.NotificarMovimiento(distancia);
                        return true;
                    }

                    // Ya es un desplazamiento vertical de la lista.
                    if (vertical >= InicioDeslizamiento * 1.5f)
                        _puedeDeslizar = false;
                    break;
            }

            return base.DispatchTouchEvent(e);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AjustarIconosBarras(Resources?.Configuration);
        }

        // El cambio de tema claro/oscuro llega aquí sin recrear la actividad.
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            AjustarIconosBarras(newConfig);
        }

        // Íconos oscuros en las barras del sistema con tema claro e íconos
        // claros con tema oscuro, para que siempre se lean sobre el fondo.
        private void AjustarIconosBarras(Configuration? configuracion)
        {
            if (Window is null)
                return;

            bool oscuro = configuracion is not null &&
                (configuracion.UiMode & UiMode.NightMask) == UiMode.NightYes;

            WindowInsetsControllerCompat? controlador = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controlador is null)
                return;

            controlador.AppearanceLightStatusBars = !oscuro;
            controlador.AppearanceLightNavigationBars = !oscuro;
        }
    }
}
