using System.Collections.Generic;
using System.Windows.Media;

namespace WpfDirect2D.Shapes
{
    public interface IShape
    {
        /// <summary>
        /// Is this shape instance valid, i.e. is the necessary data provided to render the shape
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Fill color to use when rendering this shape
        /// </summary>
        Color FillColor { get; set; }

        /// <summary>
        /// Stroke color to use when rendering this shape
        /// </summary>
        Color StrokeColor { get; set; }

        /// <summary>
        /// Stroke width
        /// </summary>
        float StrokeWidth { get; set; }

        /// <summary>
        /// Is this shape selected
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Color to set shape as when it is selected
        /// </summary>
        Color SelectedColor { get; set; }

        /// <summary>
        /// Additional colors to cache
        /// </summary>
        List<Color> BrushColorsToCache { get; }

        /// <summary>
        /// Get a list of all the colors this shape could use
        /// </summary>
        /// <returns></returns>
        List<Color> GetColorsToCache();
    }
}
