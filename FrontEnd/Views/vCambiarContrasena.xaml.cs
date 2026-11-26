using FrontEnd.Services;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    // Se usa en dos flujos:
    // - Recuperación (desde el login): el usuario olvidó su contraseña, así que
    //   escribe el código que le llegó al correo y la nueva dos veces, y al terminar
    //   vuelve al login.
    // - Dentro de la app (menú de la cuenta): el usuario conoce su contraseña,
    //   así que además escribe la actual, y al terminar vuelve a la app.
    public partial class vCambiarContrasena : ContentPage
    {
        private readonly bool _requiereContrasenaActual;
        private readonly string? _correoRecuperacion;

        // correoRecuperacion solo se pasa en la recuperación: es el correo al que se envió el código.
        public vCambiarContrasena(bool requiereContrasenaActual = false, string? correoRecuperacion = null)
        {
            InitializeComponent();
            _requiereContrasenaActual = requiereContrasenaActual;
            _correoRecuperacion = correoRecuperacion;

            if (correoRecuperacion is not null)
            {
                lblSubtitulo.Text = $"Escribe el código de 6 números que enviamos a {correoRecuperacion} y tu nueva contraseña.";
                stkCodigo.IsVisible = true;
            }

            if (requiereContrasenaActual)
            {
                lblTitulo.Text = "Cambia tu contraseña";
                lblSubtitulo.Text = "Escribe tu contraseña actual y luego la nueva dos veces.";
                stkContrasenaActual.IsVisible = true;
            }

            ResaltadoCampo.Aplicar(txtCodigo, txtContrasenaActual, txtNuevaContrasena, txtConfirmarContrasena);
            Entry[] nuevas = [txtNuevaContrasena, txtConfirmarContrasena];
            Formulario.EnviarConTeclado(btnCambiarContrasena.SendClicked,
                requiereContrasenaActual ? [txtContrasenaActual, .. nuevas]
                : correoRecuperacion is not null ? [txtCodigo, .. nuevas]
                : nuevas);
            stkRequisitos.Seguir(txtNuevaContrasena);
        }

        private async void OnCambiarContrasenaClicked(object? sender, EventArgs e)
        {
            var campos = new List<(InputView Campo, Func<string, string?> Regla)>
            {
                (txtNuevaContrasena, Reglas.Contrasena),
                (txtConfirmarContrasena, Reglas.Coincide(() => txtNuevaContrasena.Text))
            };
            if (_requiereContrasenaActual)
                campos.Insert(0, (txtContrasenaActual, Reglas.Obligatorio("Escribe tu contraseña actual.")));
            if (_correoRecuperacion is not null)
                campos.Insert(0, (txtCodigo, Reglas.Codigo));

            if (!Formulario.Validar([.. campos]))
                return;

            // Se envía al backend, que explica los errores: la contraseña actual incorrecta
            // (dentro de la app) o el código equivocado o vencido (en la recuperación).
            string? problema = null;
            if (!await Conexion.Ejecutar(this, btnCambiarContrasena, async t =>
                {
                    try
                    {
                        if (_correoRecuperacion is not null)
                            await Api.RestablecerContrasenaAsync(
                                _correoRecuperacion, txtCodigo.Text, txtNuevaContrasena.Text, t);
                        else if (_requiereContrasenaActual)
                            await Api.CambiarContrasenaAsync(txtContrasenaActual.Text, txtNuevaContrasena.Text, t);
                    }
                    catch (ApiException ex)
                    {
                        problema = ex.Message;
                    }
                }))
                return;

            if (problema is not null)
            {
                await DisplayAlertAsync("No se pudo cambiar la contraseña", problema, "Entendido");
                return;
            }

            if (_requiereContrasenaActual)
            {
                await Navigation.PopAsync();       // vuelve a la app
                return;
            }

            if (_correoRecuperacion is not null)
                await DisplayAlertAsync("Contraseña cambiada", "Ya puedes iniciar sesión con tu nueva contraseña.", "Entendido");
            await Navigation.PopToRootAsync();     // vuelve al login
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
