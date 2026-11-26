using FrontEnd.Services;

namespace FrontEnd
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Si quedó una sesión guardada, se abre directo en la galería.
            return new Window(new AppShell(Api.HaySesion));
        }

        // Cambia toda la app al login (false) o a la pantalla de inicio (true).
        public static void CambiarRaiz(bool sesionIniciada)
        {
            if (Current?.Windows.FirstOrDefault() is Window ventana)
                ventana.Page = new AppShell(sesionIniciada);
        }
    }
}
