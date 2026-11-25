namespace FrontEnd.Utils
{
    public static class Formulario
    {
        // Quita los espacios a los lados de cada campo y aplica su regla. Si alguna
        // falla, muestra el primer error, lleva el foco a ese campo y devuelve false.
        public static bool Validar(Label mensaje, params (InputView Campo, Func<string, string?> Regla)[] campos)
        {
            mensaje.IsVisible = false;

            // Primero se limpian todos, porque una regla puede comparar dos campos.
            foreach ((InputView campo, _) in campos)
                campo.Text = campo.Text?.Trim() ?? string.Empty;

            foreach ((InputView campo, Func<string, string?> regla) in campos)
            {
                if (regla(campo.Text) is not string error)
                    continue;

                mensaje.Text = error;
                mensaje.IsVisible = true;
                SemanticScreenReader.Announce(error);
                campo.Focus();
                return false;
            }

            return true;
        }
    }
}
