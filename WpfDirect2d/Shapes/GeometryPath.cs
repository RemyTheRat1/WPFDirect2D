using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace WpfDirect2D.Shapes
{
    internal class GeometryPath : BaseGeometry
    {
        private string _path;

        public GeometryPath(string geometryPath, PathGeometry geometry) : base(geometry)
        {
            Path = geometryPath;            
        }

        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                SetGeometryHash();
            }
        }

        protected override void SetGeometryHash()
        {
            GeometryHash = Path.GetHashCode();
        }

        public override void CreateRealizations(DeviceContext1 deviceContext)
        {
            var matrix = new RawMatrix3x2(Matrix3x2.Identity.M11, Matrix3x2.Identity.M12, Matrix3x2.Identity.M21, Matrix3x2.Identity.M22, Matrix3x2.Identity.M31, Matrix3x2.Identity.M32);
            var tolerance = D2D1.ComputeFlatteningTolerance(ref matrix, maxZoomFactor: 4);
            FilledRealization = new GeometryRealization(deviceContext, Geometry, tolerance);
            StrokedRealization = new GeometryRealization(deviceContext, Geometry, tolerance, 1f, null);
        }
    }
}
