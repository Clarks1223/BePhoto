namespace FrontEnd.Utils
{
    // Lista de requisitos de la contraseña que se marcan mientras se escribe.
    public class RequisitosContrasena : VerticalStackLayout
    {
        private readonly List<Label> _etiquetas = [];

        public RequisitosContrasena()
        {
            Spacing = 2;
            foreach (var _ in Reglas.RequisitosContrasena)
            {
                var etiqueta = new Label();
                _etiquetas.Add(etiqueta);
                Add(etiqueta);
            }
            Actualizar(string.Empty);
        }

        public void Seguir(Entry campo)
        {
            campo.TextChanged += (_, e) => Actualizar(e.NewTextValue ?? string.Empty);
        }

        private void Actualizar(string texto)
        {
            // Igual que al validar, los espacios a los lados no cuentan.
            string contrasena = texto.Trim();

            for (int i = 0; i < _etiquetas.Count; i++)
            {
                (string requisito, Func<string, bool> cumple) = Reglas.RequisitosContrasena[i];
                bool cumplido = cumple(contrasena);
                Label etiqueta = _etiquetas[i];

                etiqueta.Text = $"{(cumplido ? "✓" : "○")}  {Mayuscula(requisito)}";
                etiqueta.SetDynamicResource(StyleProperty, cumplido ? "RequisitoCumplido" : "TextoAyuda");
                SemanticProperties.SetDescription(etiqueta,
                    $"{Mayuscula(requisito)}: {(cumplido ? "cumplido" : "pendiente")}");
            }
        }

        private static string Mayuscula(string texto) => char.ToUpper(texto[0]) + texto[1..];
    }
}
