using SharpDX.Direct2D1;

namespace WpfDirect2d.Shapes
{
    internal class GeometryPath : BaseGeometry
    {
        public GeometryPath(string geometryPath, PathGeometry geometry) : base(geometry)
        {
            Path = geometryPath;            
        }

        public string Path { get; set; }

        public override bool IsGeometryForShape(IShape shape)
        {
            var pathShape = shape as VectorShape;
            if (pathShape == null)
            {
                return false;
            }

            return pathShape.GeometryPath == Path;
        }
    }
}
