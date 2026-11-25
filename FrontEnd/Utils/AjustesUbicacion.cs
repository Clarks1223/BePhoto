namespace FrontEnd.Utils
{
    public static class AjustesUbicacion
    {
        // Abre los ajustes de ubicación y termina cuando el usuario vuelve a la app.
        public static async Task AbrirYEsperar(Window ventana)
        {
            var regreso = new TaskCompletionSource();
            void AlVolver(object? sender, EventArgs e) => regreso.TrySetResult();

            ventana.Resumed += AlVolver;
            try
            {
                Abrir();
                await regreso.Task;
            }
            finally
            {
                ventana.Resumed -= AlVolver;
            }
        }

        private static void Abrir()
        {
#if ANDROID
            try
            {
                Platform.CurrentActivity!.StartActivity(
                    new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                return;
            }
            catch (Android.Content.ActivityNotFoundException)
            {
                // Sin esa pantalla quedan los ajustes de la app.
            }
#endif
            AppInfo.Current.ShowSettingsUI();
        }
    }
}
