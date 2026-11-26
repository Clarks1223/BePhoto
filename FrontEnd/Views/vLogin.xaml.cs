using FrontEnd.Services;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vLogin : ContentPage
    {
        public vLogin()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtCorreo, txtContrasena);
            Formulario.EnviarConTeclado(btnIniciarSesion.SendClicked, txtCorreo, txtContrasena);
        }

        protected override bool OnBackButtonPressed()
        {
            // MAUI falla si la actividad se cierra con un campo enfocado.
            txtCorreo.Unfocus();
            txtContrasena.Unfocus();
            return base.OnBackButtonPressed();
        }

        private async void OnIniciarSesionClicked(object? sender, EventArgs e)
        {
            if (!Formulario.Validar(
                    (txtCorreo, Reglas.Correo),
                    (txtContrasena, Reglas.Obligatorio("Escribe tu contraseña."))))
                return;

            // Las credenciales incorrectas las explica el servidor (401): se muestran aquí
            // en vez de ofrecer "reintentar", que no arreglaría nada.
            string? problema = null;
            if (!await Conexion.Ejecutar(this, btnIniciarSesion, async t =>
                {
                    try
                    {
                        await Api.IniciarSesionAsync(txtCorreo.Text, txtContrasena.Text, t);
                    }
                    catch (ApiException ex)
                    {
                        problema = ex.Message;
                    }
                }))
                return;

            if (problema is not null)
            {
                await DisplayAlertAsync("No se pudo iniciar sesión", problema, "Entendido");
                return;
            }

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
