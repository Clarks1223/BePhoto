namespace FrontEnd.Utils
{
    // Sigue el dedo cuando se desliza hacia la izquierda en cualquier parte de la
    // pantalla. En Android lo alimenta MainActivity, porque un SwipeGestureRecognizer
    // no recibe los toques que caen sobre las tarjetas del CollectionView.
    public static class DeslizamientoMapa
    {
        // Distancia recorrida hacia la izquierda, en unidades de MAUI.
        public static event Action<double>? Movido;

        // true si el dedo se levantó; false si el sistema canceló el gesto.
        public static event Action<bool>? Terminado;

        public static bool HayOyentes => Movido is not null;

        public static void NotificarMovimiento(double distancia) => Movido?.Invoke(distancia);

        public static void NotificarFin(bool completo) => Terminado?.Invoke(completo);
    }
}
