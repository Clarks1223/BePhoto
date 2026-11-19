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
            // TODO: cuando exista el backend, abrir con sesionIniciada: true si
            // hay una sesión guardada.
            return new Window(new AppShell());
        }

        // Cambia toda la app al login (false) o a la pantalla de inicio (true).
        public static void CambiarRaiz(bool sesionIniciada)
        {
            if (Current?.Windows.FirstOrDefault() is Window ventana)
                ventana.Page = new AppShell(sesionIniciada);
        }
    }
}
