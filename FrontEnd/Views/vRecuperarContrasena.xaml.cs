using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vRecuperarContrasena : ContentPage
    {
        public vRecuperarContrasena()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtCorreo);
            Formulario.EnviarConTeclado(btnEnviar.SendClicked, txtCorreo);
        }

        private async void OnEnviarClicked(object? sender, EventArgs e)
        {
            if (!Formulario.Validar((txtCorreo, Reglas.Correo)))
                return;

            // TODO: enviar txtCorreo al backend para que mande el correo de
            // recuperación. Por ahora solo se pasa a la pantalla de cambiar la
            // contraseña para poder recorrer el flujo.
            if (!await Conexion.Ejecutar(this, btnEnviar, _ => Task.CompletedTask))
                return;

            await Navigation.PushAsync(new vCambiarContrasena());
        }

        private async void OnIrLoginClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
