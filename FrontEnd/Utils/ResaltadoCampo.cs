namespace FrontEnd.Utils
{
    // Pinta de violeta el borde del campo (el Border que envuelve al Entry o Editor)
    // mientras tiene el foco, y lo regresa a su color normal al salir.
    public static class ResaltadoCampo
    {
        private const double GrosorNormal = 1;
        private const double GrosorFoco = 2;

        private static readonly BindableProperty ConErrorProperty =
            BindableProperty.CreateAttached("ConError", typeof(bool), typeof(ResaltadoCampo), false);

        public static void Aplicar(params InputView[] campos)
        {
            foreach (InputView campo in campos)
            {
                campo.Focused += (_, _) => Pintar(campo, conFoco: true);
                campo.Unfocused += (_, _) => Pintar(campo, conFoco: false);
            }
        }

        // Con error, el borde se queda en rojo aunque el campo tenga el foco.
        public static void MarcarError(InputView campo, bool conError)
        {
            Border? contenedor = BuscarContenedor(campo);
            if (contenedor is null || (bool)campo.GetValue(ConErrorProperty) == conError)
                return;

            campo.SetValue(ConErrorProperty, conError);
            if (Application.Current?.Resources.TryGetValue(
                    conError ? "CampoConError" : "CampoContenedor", out object? estilo) == true)
                contenedor.Style = (Style)estilo;
            Pintar(campo, campo.IsFocused);
        }

        private static void Pintar(InputView campo, bool conFoco)
        {
            Border? contenedor = BuscarContenedor(campo);
            if (contenedor is null)
                return;

            if (conFoco && !(bool)campo.GetValue(ConErrorProperty))
            {
                contenedor.Stroke = Recurso("Acento");
            }
            else
            {
                // Quitar el valor local hace que el borde vuelva al de su estilo,
                // que ya cambia solo entre tema claro y oscuro.
                contenedor.ClearValue(Border.StrokeProperty);
            }
            contenedor.StrokeThickness = conFoco ? GrosorFoco : GrosorNormal;
        }

        public static Border? BuscarContenedor(Element elemento)
        {
            Element? actual = elemento.Parent;
            while (actual is not null and not Border)
                actual = actual.Parent;
            return actual as Border;
        }

        private static Color Recurso(string clave) =>
            Application.Current?.Resources.TryGetValue(clave, out object? valor) == true && valor is Color color
                ? color
                : Colors.Transparent;
    }
}
