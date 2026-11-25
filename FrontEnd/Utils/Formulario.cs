using System.Runtime.CompilerServices;

namespace FrontEnd.Utils
{
    public static class Formulario
    {
        // La etiqueta de error de cada campo, creada la primera vez que falla.
        private static readonly ConditionalWeakTable<InputView, Label> Errores = new();

        // Quita los espacios a los lados de cada campo y aplica su regla. Muestra cada
        // error debajo de su campo y lleva el foco al primero. Devuelve true si todo es válido.
        public static bool Validar(params (InputView Campo, Func<string, string?> Regla)[] campos)
        {
            // Primero se limpian todos, porque una regla puede comparar dos campos.
            foreach ((InputView campo, _) in campos)
                campo.Text = campo.Text?.Trim() ?? string.Empty;

            InputView? primero = null;
            foreach ((InputView campo, Func<string, string?> regla) in campos)
            {
                string? error = regla(campo.Text);
                MostrarError(campo, error);

                if (error is not null && primero is null)
                {
                    primero = campo;
                    SemanticScreenReader.Announce(error);
                }
            }

            primero?.Focus();
            return primero is null;
        }

        // La tecla del teclado pasa al campo siguiente y, en el último, envía el formulario.
        public static void EnviarConTeclado(Action enviar, params Entry[] campos)
        {
            for (int i = 0; i < campos.Length; i++)
            {
                Entry campo = campos[i];
                Entry? siguiente = i + 1 < campos.Length ? campos[i + 1] : null;

                // Se espera a que el sistema termine de atender la tecla, porque
                // también intenta mover el foco por su cuenta.
                campo.Completed += (_, _) => campo.Dispatcher.Dispatch(() =>
                {
                    if (siguiente is not null)
                    {
                        siguiente.Focus();
                        return;
                    }
                    campo.Unfocus();
                    enviar();
                });
            }
        }

        private static void MostrarError(InputView campo, string? error)
        {
            ResaltadoCampo.MarcarError(campo, error is not null);

            if (!Errores.TryGetValue(campo, out Label? etiqueta))
            {
                if (error is null || (etiqueta = CrearEtiqueta(campo)) is null)
                    return;
                Errores.Add(campo, etiqueta);
                // El error desaparece en cuanto se corrige el campo.
                campo.TextChanged += (_, _) => MostrarError(campo, null);
            }

            etiqueta.Text = error;
            etiqueta.IsVisible = error is not null;
        }

        private static Label? CrearEtiqueta(InputView campo)
        {
            if (ResaltadoCampo.BuscarContenedor(campo) is not { Parent: Layout lista } contenedor)
                return null;

            var etiqueta = new Label { Margin = new Thickness(2, 6, 0, 0) };
            etiqueta.SetDynamicResource(VisualElement.StyleProperty, "MensajeError");
            lista.Insert(lista.IndexOf(contenedor) + 1, etiqueta);
            return etiqueta;
        }
    }
}
