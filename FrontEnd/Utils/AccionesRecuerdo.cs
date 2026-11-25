using FrontEnd.Models;
using FrontEnd.Repositories;
using FrontEnd.Views;

namespace FrontEnd.Utils
{
    // Opciones de una foto que comparten la galería y el visor.
    public static class AccionesRecuerdo
    {
        private const string VerDetalles = "Ver detalles";
        private const string Eliminar = "Eliminar";
        private const string Cancelar = "Cancelar";

        // Devuelve true si la foto se eliminó.
        public static async Task<bool> MostrarMenu(Page pagina, Recuerdo recuerdo)
        {
            string? opcion = await pagina.DisplayActionSheetAsync("Opciones de la foto", Cancelar, Eliminar, VerDetalles);

            if (opcion == Eliminar)
                return await ConfirmarEliminar(pagina, recuerdo);

            if (opcion == VerDetalles)
                await pagina.Navigation.PushAsync(new vDetalle(recuerdo));
            return false;
        }

        // Devuelve true si la foto se eliminó.
        public static async Task<bool> ConfirmarEliminar(Page pagina, Recuerdo recuerdo)
        {
            if (!await pagina.DisplayAlertAsync("¿Eliminar esta foto?",
                    $"Se eliminará la foto del {recuerdo.FechaTexto} con su ubicación. Esta acción no se puede deshacer.",
                    Eliminar, Cancelar))
                return false;

            try
            {
                await RecuerdoRepository.EliminarAsync(recuerdo);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await pagina.DisplayAlertAsync("No se pudo eliminar",
                    "Ocurrió un problema al eliminar la foto. Inténtalo de nuevo.", "Entendido");
                return false;
            }
        }

        public static string DetalleUbicacion(Recuerdo recuerdo) =>
            recuerdo.PrecisionMetros is double metros
                ? $"Coordenadas: {recuerdo.Coordenadas}\nPrecisión: ±{metros:0} m"
                : $"Coordenadas: {recuerdo.Coordenadas}";
    }
}
