using FrontEnd.Models;
using FrontEnd.Repositories;
using FrontEnd.Views;

namespace FrontEnd.Utils
{
    // Opciones de una foto que comparten la galería y el visor.
    public static class AccionesRecuerdo
    {
        private const string Compartir = "Compartir";
        private const string VerDetalles = "Ver detalles";
        private const string Eliminar = "Eliminar";
        private const string Cancelar = "Cancelar";

        // Devuelve true si la foto se eliminó.
        public static async Task<bool> MostrarMenu(Page pagina, Recuerdo recuerdo)
        {
            // Las fotos de ejemplo no se pueden compartir.
            string[] opciones = CompartirFoto.Disponible(recuerdo) ? [Compartir, VerDetalles] : [VerDetalles];
            string? opcion = await pagina.DisplayActionSheetAsync("Opciones de la foto", Cancelar, Eliminar, opciones);

            if (opcion == Eliminar)
                return await ConfirmarEliminar(pagina, recuerdo);

            if (opcion == Compartir)
            {
                try
                {
                    await CompartirFoto.Compartir(pagina, recuerdo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    await pagina.DisplayAlertAsync("No se pudo compartir", "Inténtalo de nuevo.", "Aceptar");
                }
            }

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

            return await Conexion.Ejecutar(pagina, t => RecuerdoRepository.EliminarAsync(recuerdo, t));
        }

        public static string DetalleUbicacion(Recuerdo recuerdo) =>
            recuerdo.PrecisionMetros is double metros
                ? $"Coordenadas: {recuerdo.Coordenadas}\nPrecisión: ±{metros:0} m"
                : $"Coordenadas: {recuerdo.Coordenadas}";
    }
}
