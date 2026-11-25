#if ANDROID
using AndroidX.Core.View;
#endif

namespace FrontEnd.Utils
{
    // Oculta las barras de Android; deslizar desde el borde las muestra un momento.
    public static class PantallaCompleta
    {
        public static void Activar(bool activar)
        {
#if ANDROID
            if (Platform.CurrentActivity?.Window is not Android.Views.Window ventana)
                return;

            WindowInsetsControllerCompat? controlador = WindowCompat.GetInsetsController(ventana, ventana.DecorView);
            if (controlador is null)
                return;

            if (activar)
            {
                controlador.SystemBarsBehavior =
                    WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                controlador.Hide(WindowInsetsCompat.Type.SystemBars());
            }
            else
            {
                controlador.Show(WindowInsetsCompat.Type.SystemBars());
            }
#endif
        }
    }
}
