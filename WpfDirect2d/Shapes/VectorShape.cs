using System.Windows.Media;

namespace WpfDirect2d.Shapes
{
    public class VectorShape : IShape
    {
        public bool IsValid => !string.IsNullOrEmpty(GeometryPath);

        /// <summary>
        /// Path describing the geometry in svg / xaml path format
        /// </summary>
        public string GeometryPath { get; set; }

        public float PixelXLocation { get; set; }

        public float PixelYLocation { get; set; }

        public float Scaling { get; set; }

        public Color FillColor { get; set; }

        public Color StrokeColor { get; set; }

        public float StrokeWidth { get; set; }

        public bool IsSelected { get; set; }

        public Color SelectedColor { get; set; }
    }
}
