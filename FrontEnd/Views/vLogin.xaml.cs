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

            // TODO: enviar txtCorreo y txtContrasena al backend y entrar solo si
            // las credenciales son correctas. Por ahora entra directamente.
            if (!await Conexion.Ejecutar(this, btnIniciarSesion, async t => { using var h = new HttpClient(); await h.GetAsync(txtContrasena.Text == "apagado" ? "https://10.0.2.2:5999/" : "https://10.0.2.2:5998/", t); }))
                return;

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
