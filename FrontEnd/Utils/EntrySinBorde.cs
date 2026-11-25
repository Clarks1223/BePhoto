namespace FrontEnd.Utils
{
    // Quita la línea inferior que Android dibuja en los Entry y el borde que
    // iOS les pone, para que el campo se vea solo con el Border de BePhoto.
    public static class EntrySinBorde
    {
        public static void Configurar()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinBorde", (handler, _) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
            });

#if ANDROID
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("SinBorde", (handler, _) =>
            {
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                handler.PlatformView.SetPadding(0, 0, 0, 0);
            });
#endif
        }
    }
}
