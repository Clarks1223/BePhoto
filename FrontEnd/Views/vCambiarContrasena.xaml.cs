using FrontEnd.Utils;

namespace FrontEnd.Views
{
    // Se usa en dos flujos:
    // - Recuperación (desde el login): el usuario olvidó su contraseña, así que
    //   solo escribe la nueva dos veces y al terminar vuelve al login.
    // - Dentro de la app (menú de la cuenta): el usuario conoce su contraseña,
    //   así que además escribe la actual, y al terminar vuelve a la app.
    public partial class vCambiarContrasena : ContentPage
    {
        private readonly bool _requiereContrasenaActual;

        public vCambiarContrasena(bool requiereContrasenaActual = false)
        {
            InitializeComponent();
            _requiereContrasenaActual = requiereContrasenaActual;

            if (requiereContrasenaActual)
            {
                lblTitulo.Text = "Cambia tu contraseña";
                lblSubtitulo.Text = "Escribe tu contraseña actual y luego la nueva dos veces.";
                stkContrasenaActual.IsVisible = true;
            }

            ResaltadoCampo.Aplicar(txtContrasenaActual, txtNuevaContrasena, txtConfirmarContrasena);
        }

        private async void OnCambiarContrasenaClicked(object? sender, EventArgs e)
        {
            if (_requiereContrasenaActual && !Formulario.Validar(lblMensaje,
                    (txtContrasenaActual, Reglas.Obligatorio("Escribe tu contraseña actual."))))
                return;

            if (!Formulario.Validar(lblMensaje,
                    (txtNuevaContrasena, Reglas.Contrasena),
                    (txtConfirmarContrasena, Reglas.Coincide(() => txtNuevaContrasena.Text))))
                return;

            // TODO: enviar los campos al backend (incluida la
            // contraseña actual cuando _requiereContrasenaActual es true).
            if (_requiereContrasenaActual)
                await Navigation.PopAsync();       // vuelve a la app
            else
                await Navigation.PopToRootAsync(); // vuelve al login
        }

        private void OnMostrarActualClicked(object? sender, EventArgs e)
        {
            txtContrasenaActual.IsPassword = !txtContrasenaActual.IsPassword;
            btnMostrarActual.Text = txtContrasenaActual.IsPassword ? "Mostrar" : "Ocultar";
        }

        // Muestra u oculta la nueva contraseña y su confirmación a la vez para poder compararlas.
        private void OnMostrarContrasenaClicked(object? sender, EventArgs e)
        {
            bool ocultar = !txtNuevaContrasena.IsPassword;
            txtNuevaContrasena.IsPassword = ocultar;
            txtConfirmarContrasena.IsPassword = ocultar;
            btnMostrarContrasena.Text = ocultar ? "Mostrar" : "Ocultar";
        }
    }
}
