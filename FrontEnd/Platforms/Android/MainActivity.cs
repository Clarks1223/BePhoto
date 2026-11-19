using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;

namespace FrontEnd
{
    // AdjustResize: al abrir el teclado la ventana se achica en vez de desplazarse,
    // así el ScrollView puede subir el botón principal por encima del teclado.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = Android.Views.SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
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
