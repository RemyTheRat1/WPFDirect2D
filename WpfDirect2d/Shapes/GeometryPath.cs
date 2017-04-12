using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct2D1;

namespace WpfDirect2d.Shapes
{
    internal class GeometryPath : IGeometryPath
    {
        public GeometryPath(string geometryPath, PathGeometry geometry)
        {
            Path = geometryPath;
            Geometry = geometry;
        }

        public string Path { get; set; }

        public PathGeometry Geometry { get; set; }

        public GeometryType GeometryType => GeometryType.Normal;

        public void Dispose()
        {
            if (Geometry != null)
            {
                Geometry.Dispose();
            }
        }
    }
}
