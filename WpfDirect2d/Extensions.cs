using SharpDX;
using Wpf = System.Windows.Media;

namespace WpfDirect2d
{
    public static class Extensions
    {
        public static Color ToDirect2dColor(this Wpf.Color wpfColor)
        {
            return new Color(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
        }
    }
}
