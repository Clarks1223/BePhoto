namespace FrontEnd.Utils
{
    // Toque y pulsación larga sobre una foto. MAUI no trae un gesto de pulsación larga,
    // y en Android ambos deben venir del mismo control nativo para no estorbarse.
    public class ToqueFoto : PlatformBehavior<View>
    {
        // El sender es la vista tocada; su BindingContext es el recuerdo.
        public event EventHandler? Tocada;
        public event EventHandler? Mantenida;

        private View? _vista;

        private void NotificarToque() => Tocada?.Invoke(_vista, EventArgs.Empty);

        private void NotificarMantenida() => Mantenida?.Invoke(_vista, EventArgs.Empty);

#if ANDROID
        protected override void OnAttachedTo(View bindable, Android.Views.View platformView)
        {
            base.OnAttachedTo(bindable, platformView);
            _vista = bindable;
            platformView.Click += OnClick;
            platformView.LongClick += OnLongClick;
        }

        protected override void OnDetachedFrom(View bindable, Android.Views.View platformView)
        {
            platformView.Click -= OnClick;
            platformView.LongClick -= OnLongClick;
            _vista = null;
            base.OnDetachedFrom(bindable, platformView);
        }

        private void OnClick(object? sender, EventArgs e) => NotificarToque();

        // Android ya vibra al devolver Handled.
        private void OnLongClick(object? sender, Android.Views.View.LongClickEventArgs e)
        {
            e.Handled = true;
            NotificarMantenida();
        }
#elif IOS || MACCATALYST
        private UIKit.UITapGestureRecognizer? _toque;
        private UIKit.UILongPressGestureRecognizer? _pulsacion;

        protected override void OnAttachedTo(View bindable, UIKit.UIView platformView)
        {
            base.OnAttachedTo(bindable, platformView);
            _vista = bindable;
            _toque = new UIKit.UITapGestureRecognizer(NotificarToque);
            _pulsacion = new UIKit.UILongPressGestureRecognizer(gesto =>
            {
                if (gesto.State == UIKit.UIGestureRecognizerState.Began)
                    NotificarMantenida();
            });
            platformView.AddGestureRecognizer(_toque);
            platformView.AddGestureRecognizer(_pulsacion);
        }

        protected override void OnDetachedFrom(View bindable, UIKit.UIView platformView)
        {
            if (_toque is not null)
                platformView.RemoveGestureRecognizer(_toque);
            if (_pulsacion is not null)
                platformView.RemoveGestureRecognizer(_pulsacion);
            _vista = null;
            base.OnDetachedFrom(bindable, platformView);
        }
#elif WINDOWS
        protected override void OnAttachedTo(View bindable, Microsoft.UI.Xaml.FrameworkElement platformView)
        {
            base.OnAttachedTo(bindable, platformView);
            _vista = bindable;
            platformView.Tapped += OnTapped;
            platformView.RightTapped += OnRightTapped;
        }

        protected override void OnDetachedFrom(View bindable, Microsoft.UI.Xaml.FrameworkElement platformView)
        {
            platformView.Tapped -= OnTapped;
            platformView.RightTapped -= OnRightTapped;
            _vista = null;
            base.OnDetachedFrom(bindable, platformView);
        }

        private void OnTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => NotificarToque();

        // Clic derecho, o mantener presionado en pantallas táctiles.
        private void OnRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e) =>
            NotificarMantenida();
#endif
    }
}
