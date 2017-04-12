using System;
using SharpDX.Direct2D1;

namespace WpfDirect2d.Shapes
{
    internal class GeometryRealizationPath : IGeometryPath
    {
        public GeometryRealizationPath(string geometryPath, GeometryRealization filledGeometry, GeometryRealization strokedGeometry)
        {
            Path = geometryPath;
            FilledGeometry = filledGeometry;
            StrokedGeometry = strokedGeometry;
        }

        public GeometryRealization FilledGeometry { get; set; }
        public GeometryRealization StrokedGeometry { get; set; }

        /// <summary>
        /// Path describing the geometry in svg / xaml path format
        /// </summary>
        public string Path { get; set; }

        public GeometryType GeometryType => GeometryType.Realization;

        public void Dispose()
        {
            if (FilledGeometry != null && !FilledGeometry.IsDisposed)
            {
                FilledGeometry.Dispose();
            }

            if (StrokedGeometry != null && !StrokedGeometry.IsDisposed)
            {
                StrokedGeometry.Dispose();
            }
        }
    }
}
