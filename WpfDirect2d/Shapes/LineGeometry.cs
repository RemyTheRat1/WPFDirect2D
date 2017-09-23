using System;
using SharpDX.Direct2D1;
using System.Collections.Generic;

namespace WpfDirect2D.Shapes
{
    internal class LineGeometry : BaseGeometry
    {
        public LineGeometry(List<System.Windows.Point> lineNodes, PathGeometry geometry)
            : base(geometry)
        {
            LineNodes = lineNodes;
            SetGeometryHash();
        }

        public List<System.Windows.Point> LineNodes { get; }

        protected sealed override void SetGeometryHash()
        {
            GeometryHash = LineNodes.GetSequenceHashCode();
        }

        public override void CreateRealizations(DeviceContext1 deviceContext)
        {
            //lines do not support realizations at this time
            throw new NotImplementedException();
        }
    }
}
