using System;
using SharpDX.Direct2D1;

namespace WpfDirect2d.Shapes
{
    internal class RenderedGeometryPath : IDisposable
    {
        public RenderedGeometryPath(string geometryPath, PathGeometry geometry)
        {
            GeometryPath = geometryPath;
            Geometry = geometry;
        }

        public PathGeometry Geometry { get; set; }

        /// <summary>
        /// Path describing the geometry in svg / xaml path format
        /// </summary>
        public string GeometryPath { get; set; }

        public void Dispose()
        {
            if (Geometry != null && !Geometry.IsDisposed)
            {
                Geometry.Dispose();
            }
        }
    }
}
