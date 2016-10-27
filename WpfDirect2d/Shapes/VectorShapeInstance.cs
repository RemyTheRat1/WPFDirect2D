using System.Windows.Media;

namespace WpfDirect2d.Shapes
{
    public class VectorShapeInstance
    {
        public float PixelXLocation { get; set; }

        public float PixelYLocation { get; set; }

        public float Scaling { get; set; }

        public Color FillColor { get; set; }

        public Color StrokeColor { get; set; }

        public float StrokeWidth { get; set; }
    }
}
