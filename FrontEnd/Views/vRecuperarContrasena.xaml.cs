using FrontEnd.Services;
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

            // El servidor responde lo mismo exista o no la cuenta, así que siempre se
            // pasa a la pantalla donde se escribe el código.
            string? problema = null;
            if (!await Conexion.Ejecutar(this, btnEnviar, async t =>
                {
                    try
                    {
                        await Api.OlvideContrasenaAsync(txtCorreo.Text, t);
                    }
                    catch (ApiException ex)
                    {
                        problema = ex.Message;
                    }
                }))
                return;

            if (problema is not null)
            {
                await DisplayAlertAsync("No se pudo enviar el código", problema, "Entendido");
                return;
            }

            await Navigation.PushAsync(new vCambiarContrasena(correoRecuperacion: txtCorreo.Text));
        }

        private async void OnIrLoginClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
