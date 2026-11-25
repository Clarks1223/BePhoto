using FrontEnd.Models;
using FrontEnd.Repositories;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vDetalle : ContentPage
    {
        private readonly Recuerdo _recuerdo;

        public vDetalle(Recuerdo recuerdo)
        {
            InitializeComponent();
            ResaltadoCampo.Aplicar(txtDetalle);

            _recuerdo = recuerdo;
            txtDetalle.Text = recuerdo.Descripcion;
            lblFecha.Text = recuerdo.FechaTexto;
            lblCoordenadas.Text = recuerdo.Coordenadas;
            stkPrecision.IsVisible = recuerdo.PrecisionMetros is not null;
            lblPrecision.Text = $"±{recuerdo.PrecisionMetros:0} m";
        }

        private async void OnGuardarClicked(object? sender, EventArgs e)
        {
            btnGuardar.IsEnabled = false;
            lblMensaje.IsVisible = false;

            try
            {
                await RecuerdoRepository.EditarDetalleAsync(_recuerdo, txtDetalle.Text?.Trim() ?? string.Empty);
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                lblMensaje.Text = "No se pudo guardar el detalle. Inténtalo de nuevo.";
                lblMensaje.IsVisible = true;
                btnGuardar.IsEnabled = true;
            }
        }
    }
}
