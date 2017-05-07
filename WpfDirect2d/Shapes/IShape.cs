using System.Windows.Media;

namespace WpfDirect2d.Shapes
{
    public interface IShape
    {
        /// <summary>
        /// Is this shape instance valid, i.e. is the necessary data provided to render the shape
        /// </summary>
        bool IsValid { get; }

        Color FillColor { get; set; }

        Color StrokeColor { get; set; }

        float StrokeWidth { get; set; }

        bool IsSelected { get; set; }

        Color SelectedColor { get; set; }
    }
}
