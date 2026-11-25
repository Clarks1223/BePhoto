using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vRegistro : ContentPage
    {
        public vRegistro()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtNombre, txtApellido, txtCorreo, txtContrasena, txtConfirmarContrasena);
        }

        private void OnRegistrarseClicked(object? sender, EventArgs e)
        {
            if (!Formulario.Validar(lblMensaje,
                    (txtNombre, Reglas.Texto("El nombre")),
                    (txtApellido, Reglas.Texto("El apellido")),
                    (txtCorreo, Reglas.Correo),
                    (txtContrasena, Reglas.Contrasena),
                    (txtConfirmarContrasena, Reglas.Coincide(() => txtContrasena.Text))))
                return;

            // TODO: enviar los datos al backend para crear
            // la cuenta. Por ahora el botón no hace nada.
        }

        // Muestra u oculta las dos contraseñas a la vez para poder compararlas.
        private void OnMostrarContrasenaClicked(object? sender, EventArgs e)
        {
            bool ocultar = !txtContrasena.IsPassword;
            txtContrasena.IsPassword = ocultar;
            txtConfirmarContrasena.IsPassword = ocultar;
            btnMostrarContrasena.Text = ocultar ? "Mostrar" : "Ocultar";
        }

        // Regresa al login, que es la página anterior en la pila de navegación.
        private async void OnIrLoginClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
