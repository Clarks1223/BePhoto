namespace FrontEnd.Utils
{
    // Pinta de violeta el borde del campo (el Border que envuelve al Entry)
    // mientras el Entry tiene el foco, y lo regresa a su color normal al salir.
    public static class ResaltadoCampo
    {
        private const double GrosorNormal = 1;
        private const double GrosorFoco = 2;

        public static void Aplicar(params Entry[] campos)
        {
            foreach (Entry campo in campos)
            {
                campo.Focused += (_, _) => Pintar(campo, conFoco: true);
                campo.Unfocused += (_, _) => Pintar(campo, conFoco: false);
            }
        }

        private static void Pintar(Entry campo, bool conFoco)
        {
            Border? contenedor = BuscarContenedor(campo);
            if (contenedor is null)
                return;

            if (conFoco)
            {
                contenedor.Stroke = Recurso("Acento");
                contenedor.StrokeThickness = GrosorFoco;
            }
            else
            {
                // Quitar el valor local hace que el borde vuelva al del estilo
                // CampoContenedor, que ya cambia solo entre tema claro y oscuro.
                contenedor.ClearValue(Border.StrokeProperty);
                contenedor.StrokeThickness = GrosorNormal;
            }
        }

        private static Border? BuscarContenedor(Element elemento)
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
