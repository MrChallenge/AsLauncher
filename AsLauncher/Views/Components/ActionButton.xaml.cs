using AsLauncher.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AsLauncher.Views.Components
{
    public partial class ActionButton : UserControl
    {
        // Initialize
        public ActionButton()
        {
            InitializeComponent();

            // Forward click event
            ButtonRoot.Click += (s, e) =>
            {
                Click?.Invoke(this, e);
            };
        }

        public event RoutedEventHandler? Click;

        // ButtonContent property
        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            nameof(ButtonContent),
            typeof(object),
            typeof(ActionButton),
            new PropertyMetadata(null));

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        public static readonly DependencyProperty ButtonBackgroundProperty = DependencyProperty.Register(
            nameof(ButtonBackground),
            typeof(Brush),
            typeof(ActionButton),
            new PropertyMetadata(Theme.Green));

        public Brush ButtonBackground
        {
            get => (Brush)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }

        public static readonly DependencyProperty ButtonForegroundProperty = DependencyProperty.Register(
            nameof(ButtonForeground),
            typeof(Brush),
            typeof(ActionButton),
            new PropertyMetadata(Brushes.White));

        public Brush ButtonForeground
        {
            get => (Brush)GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
        }

        public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register(
            nameof(ButtonBorderBrush),
            typeof(Brush),
            typeof(ActionButton),
            new PropertyMetadata(Brushes.Transparent));

        public Brush ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }
    }
}