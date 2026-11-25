using System.Collections.ObjectModel;
using FrontEnd.Models;
using FrontEnd.Utils;

namespace FrontEnd.Views
{
    public partial class vVisor : ContentPage
    {
        private readonly ObservableCollection<Recuerdo> _fotos;
        private bool _ocupado;

        // Lo que tarda el carrusel en acomodarse tras cambiar de foto.
        private const int EsperaCarrusel = 100;

        public vVisor(IEnumerable<Recuerdo> fotos, Recuerdo inicial)
        {
            InitializeComponent();
            _fotos = new ObservableCollection<Recuerdo>(fotos);
            crvFotos.ItemsSource = _fotos;
            crvFotos.Position = Math.Max(0, _fotos.IndexOf(inicial));
            MostrarPosicion();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            PantallaCompleta.Activar(true);
        }

        protected override void OnDisappearing()
        {
            PantallaCompleta.Activar(false);
            base.OnDisappearing();
        }

        private void OnPosicionCambiada(object? sender, PositionChangedEventArgs e) => MostrarPosicion();

        private void MostrarPosicion()
        {
            int posicion = Math.Clamp(crvFotos.Position, 0, Math.Max(0, _fotos.Count - 1));
            lblPosicion.Text = $"{posicion + 1} de {_fotos.Count}";
        }

        private async void OnFotoMantenida(object? sender, EventArgs e)
        {
            if ((sender as View)?.BindingContext is Recuerdo recuerdo)
                await Atender(recuerdo, AccionesRecuerdo.MostrarMenu);
        }

        private async void OnEliminarTapped(object? sender, TappedEventArgs e)
        {
            if (crvFotos.Position >= 0 && crvFotos.Position < _fotos.Count)
                await Atender(_fotos[crvFotos.Position], AccionesRecuerdo.ConfirmarEliminar);
        }

        private async Task Atender(Recuerdo recuerdo, Func<Page, Recuerdo, Task<bool>> accion)
        {
            if (_ocupado)
                return;
            _ocupado = true;

            try
            {
                if (!await accion(this, recuerdo))
                    return;

                // Al borrar la foto visible el carrusel saltaría a la primera;
                // se pasa antes a la anterior.
                int indice = _fotos.IndexOf(recuerdo);
                if (indice > 0)
                {
                    crvFotos.Position = indice - 1;
                    await Task.Delay(EsperaCarrusel);
                }

                _fotos.Remove(recuerdo);
                if (_fotos.Count == 0)
                    await Navigation.PopAsync();
                else
                    MostrarPosicion();
            }
            finally
            {
                _ocupado = false;
            }
        }

        private async void OnVolverTapped(object? sender, TappedEventArgs e)
        {
            if (_ocupado)
                return;
            _ocupado = true;
            await Navigation.PopAsync();
        }
    }
}
