using FrontEnd.Views;

namespace FrontEnd
{
    // El Shell tiene una sola raíz: el login (sin sesión) o la pantalla de inicio
    // (con sesión). Para entrar o salir se crea un Shell nuevo con App.CambiarRaiz;
    // así la app arranca limpia y el botón Atrás nunca cruza de una raíz a la otra.
    public partial class AppShell : Shell
    {
        public AppShell(bool sesionIniciada = false)
        {
            InitializeComponent();

            Items.Add(sesionIniciada
                ? new ShellContent
                {
                    Title = "Galería",
                    Route = "vInicio",
                    ContentTemplate = new DataTemplate(typeof(vInicio))
                }
                : new ShellContent
                {
                    Title = "Iniciar sesión",
                    Route = "vLogin",
                    ContentTemplate = new DataTemplate(typeof(vLogin))
                });
        }
    }
}
