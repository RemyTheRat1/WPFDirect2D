using System;
using SharpDX.Direct2D1;

namespace WpfDirect2d.Shapes
{
    internal class GeometryPath : IDisposable
    {
        public GeometryPath(string geometryPath, PathGeometry geometry)
        {
            Path = geometryPath;
            Geometry = geometry;
        }

        public string Path { get; set; }

        public PathGeometry Geometry { get; set; }        

        public void Dispose()
        {
            if (Geometry != null)
            {
                Geometry.Dispose();
            }
        }
    }
}
