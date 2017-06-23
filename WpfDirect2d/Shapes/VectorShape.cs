using System.Collections.Generic;
using System.Windows.Media;

namespace WpfDirect2D.Shapes
{
    public class VectorShape : IShape
    {
        public VectorShape()
        {
            BrushColorsToCache = new List<Color>();
        }

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

        public List<Color> BrushColorsToCache { get; }

        public List<Color> GetColorsToCache()
        {
            //make sure the stoke, fill, and selected colors are in the list
            if (!BrushColorsToCache.Contains(FillColor))
            {
                BrushColorsToCache.Add(FillColor);
            }

            if (!BrushColorsToCache.Contains(StrokeColor))
            {
                BrushColorsToCache.Add(StrokeColor);
            }

            if (!BrushColorsToCache.Contains(SelectedColor))
            {
                BrushColorsToCache.Add(SelectedColor);
            }

            return BrushColorsToCache;
        }
    }
}
