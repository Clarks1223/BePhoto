using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vRecuperarContrasena : ContentPage
    {
        public vRecuperarContrasena()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtCorreo);
        }

        private async void OnEnviarClicked(object? sender, EventArgs e)
        {
            // TODO: enviar txtCorreo al backend para que mande el correo de
            // recuperación. Por ahora solo se pasa a la pantalla de cambiar la
            // contraseña para poder recorrer el flujo.
            await Navigation.PushAsync(new vCambiarContrasena());
        }

        private async void OnIrLoginClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
