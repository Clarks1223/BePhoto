using FrontEnd.Services;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vRegistro : ContentPage
    {
        public vRegistro()
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtNombre, txtApellido, txtCorreo, txtContrasena, txtConfirmarContrasena);
            Formulario.EnviarConTeclado(btnRegistrarse.SendClicked,
                txtNombre, txtApellido, txtCorreo, txtContrasena, txtConfirmarContrasena);
            stkRequisitos.Seguir(txtContrasena);
        }

        private async void OnRegistrarseClicked(object? sender, EventArgs e)
        {
            if (!Formulario.Validar(
                    (txtNombre, Reglas.Texto("El nombre")),
                    (txtApellido, Reglas.Texto("El apellido")),
                    (txtCorreo, Reglas.Correo),
                    (txtContrasena, Reglas.Contrasena),
                    (txtConfirmarContrasena, Reglas.Coincide(() => txtContrasena.Text))))
                return;

            // Un correo repetido (409) o datos no válidos (400) los explica el servidor.
            string? problema = null;
            if (!await Conexion.Ejecutar(this, btnRegistrarse, async t =>
                {
                    try
                    {
                        await Api.RegistrarAsync(
                            txtNombre.Text, txtApellido.Text, txtCorreo.Text, txtContrasena.Text, t);
                    }
                    catch (ApiException ex)
                    {
                        problema = ex.Message;
                    }
                }))
                return;

            if (problema is not null)
            {
                await DisplayAlertAsync("No se pudo crear la cuenta", problema, "Entendido");
                return;
            }

            // La cuenta queda lista: el servidor ya devolvió la sesión.
            App.CambiarRaiz(sesionIniciada: true);
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
