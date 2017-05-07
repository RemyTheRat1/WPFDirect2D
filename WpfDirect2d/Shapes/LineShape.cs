using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WpfDirect2d.Shapes
{
    public class LineShape : IShape
    {
        public LineShape()
        {
            LineNodes = new List<Point>();
        }

        public bool IsValid => LineNodes.Count >= 2;

        public Color FillColor { get; set; }

        public Color StrokeColor { get; set; }

        public float StrokeWidth { get; set; }

        public bool IsSelected { get; set; }

        public Color SelectedColor { get; set; }

        public List<Point> LineNodes { get; }

        public bool IsLineClosed { get; set; }

        public Point GetStartingPoint()
        {
            return LineNodes.FirstOrDefault();
        }

        /// <summary>
        /// Get the list of points to connect to the starting point.
        /// </summary>
        /// <returns></returns>
        public List<Point> GetConnectingPoints()
        {
            if (!IsValid)
            {
                return null;
            }

            return LineNodes.Skip(1).ToList();
        }
    }
}
