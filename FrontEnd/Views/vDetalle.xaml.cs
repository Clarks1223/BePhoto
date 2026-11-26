using FrontEnd.Models;
using FrontEnd.Repositories;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vDetalle : ContentPage
    {
        private readonly Recuerdo _recuerdo;
        private bool _saliendo;

        public vDetalle(Recuerdo recuerdo)
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtDetalle);

            _recuerdo = recuerdo;
            txtDetalle.Text = recuerdo.Descripcion;
            ActualizarEstado();
            bbVolver.Command = new Command(async () => await Salir());
            lblFecha.Text = recuerdo.FechaTexto;
            lblCoordenadas.Text = recuerdo.Coordenadas;
            stkPrecision.IsVisible = recuerdo.PrecisionMetros is not null;
            lblPrecision.Text = $"±{recuerdo.PrecisionMetros:0} m";
        }

        private bool HayCambios =>
            (txtDetalle.Text?.Trim() ?? string.Empty) != (_recuerdo.Descripcion?.Trim() ?? string.Empty);

        private void OnDetalleCambiado(object? sender, TextChangedEventArgs e) => ActualizarEstado();

        private void ActualizarEstado()
        {
            int usados = txtDetalle.Text?.Length ?? 0;
            lblContador.Text = $"{usados} / {Reglas.MaxTexto}";
            SemanticProperties.SetDescription(lblContador, $"{usados} de {Reglas.MaxTexto} caracteres");
            btnGuardar.IsEnabled = HayCambios;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = Salir();
            return true;
        }

        // Antes de salir confirma que se pueden perder los cambios sin guardar.
        private async Task Salir()
        {
            if (_saliendo)
                return;
            _saliendo = true;

            txtDetalle.Unfocus();
            if (HayCambios && !await DisplayAlertAsync("¿Descartar los cambios?",
                    "El detalle que escribiste no se ha guardado.", "Descartar", "Seguir editando"))
            {
                _saliendo = false;
                return;
            }

            await Navigation.PopAsync();
        }

        private async void OnGuardarClicked(object? sender, EventArgs e)
        {
            if (!Formulario.Validar((txtDetalle, Reglas.Texto("El detalle"))))
                return;

            if (_saliendo)
                return;
            _saliendo = true;
            btnGuardar.IsEnabled = false;

            if (await Conexion.Ejecutar(this,
                    t => RecuerdoRepository.EditarDetalleAsync(_recuerdo, txtDetalle.Text, t)))
            {
                await Navigation.PopAsync();
                return;
            }
            btnGuardar.IsEnabled = HayCambios;
            _saliendo = false;
        }
    }
}
