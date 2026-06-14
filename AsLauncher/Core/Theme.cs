using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AsLauncher.Core
{
    public static class Theme
    {
        // App Icon
        public static readonly Uri LauncherIcon = new Uri("pack://application:,,,/Assets/Logo/AsLauncher.ico");
        
        // Assets
        public static BitmapImage LauncherLogo => new(new Uri("pack://application:,,,/Assets/Logo/AsLauncher.png"));

        public static BitmapImage ExpandButton => new(new Uri("pack://application:,,,/Assets/Textures/ExpandButton.png"));

        public static BitmapImage RemoveButton => new(new Uri("pack://application:,,,/Assets/Textures/RemoveButton.png"));

        public static BitmapImage IconGeneral => new(new Uri("pack://application:,,,/Assets/Textures/IconGeneral.png"));
        
        public static BitmapImage IconVanilla => new(new Uri("pack://application:,,,/Assets/Textures/IconVanilla.png"));
        
        public static BitmapImage IconModpacks => new(new Uri("pack://application:,,,/Assets/Textures/IconModpacks.png"));

        public static BitmapImage IconConfigs => new(new Uri("pack://application:,,,/Assets/Textures/IconConfigs.png"));

        // CheckBoxMark
        public static readonly Geometry CheckBoxMark = Geometry.Parse("M 5 9 L 7 11 L 12 6");

        // Transparent
        public static readonly Brush Invisible = (Brush)new BrushConverter().ConvertFrom("#01000000")!;

        public static readonly Brush Transparent = Brushes.Transparent;

        // Based UI palette
        public static readonly Brush Background = (Brush)new BrushConverter().ConvertFrom("#111111")!;

        public static readonly Brush Middleground = (Brush)new BrushConverter().ConvertFrom("#181818")!;

        public static readonly Brush Foreground = (Brush)new BrushConverter().ConvertFrom("#303030")!;

        public static readonly Brush ForegroundHovered = (Brush)new BrushConverter().ConvertFrom("#252525")!;

        public static readonly Brush ProgressBar = (Brush)new BrushConverter().ConvertFrom("#2A2A2A")!;

        // White
        public static readonly Brush White = (Brush)new BrushConverter().ConvertFrom("#FFFFFF")!;

        public static readonly Brush WhiteHovered = (Brush)new BrushConverter().ConvertFrom("#E5E7EB")!;

        // Grey
        public static readonly Brush Grey = (Brush)new BrushConverter().ConvertFrom("#3A3A3A")!;

        public static readonly Brush LightGrey = (Brush)new BrushConverter().ConvertFrom("#BBBBBB")!;

        // Black
        public static readonly Brush Black = (Brush)new BrushConverter().ConvertFrom("#000000")!;

        // Green
        public static readonly Brush Green = (Brush)new BrushConverter().ConvertFrom("#16A34A")!;

        // Blue
        public static readonly Brush Blue = (Brush)new BrushConverter().ConvertFrom("#5865F2")!;

        public static readonly Brush LightBlue = (Brush)new BrushConverter().ConvertFrom("#4CC2FF")!;

        // Red
        public static readonly Brush Red = (Brush)new BrushConverter().ConvertFrom("#DA373C")!;

        // Yellow
        public static readonly Brush Yellow = (Brush)new BrushConverter().ConvertFrom("#FAA61A")!;
    }
}