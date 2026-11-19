using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vLogin : ContentPage
    {
        public vLogin()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtCorreo, txtContrasena);
        }

        private void OnIniciarSesionClicked(object? sender, EventArgs e)
        {
            // TODO: enviar txtCorreo y txtContrasena al backend y entrar solo si
            // las credenciales son correctas. Por ahora entra directamente.
            App.CambiarRaiz(sesionIniciada: true);
        }

        private async void OnOlvideContrasenaClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new vRecuperarContrasena());
        }

        private void OnMostrarContrasenaClicked(object? sender, EventArgs e)
        {
            txtContrasena.IsPassword = !txtContrasena.IsPassword;
            btnMostrarContrasena.Text = txtContrasena.IsPassword ? "Mostrar" : "Ocultar";
        }

        private async void OnIrRegistroClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new vRegistro());
        }
    }
}
