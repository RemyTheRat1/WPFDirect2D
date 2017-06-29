using System;
using SharpDX.Direct2D1;
using System.Collections.Generic;
using System.Linq;

namespace WpfDirect2D.Shapes
{
    internal class LineGeometry : BaseGeometry
    {
        public LineGeometry(List<System.Windows.Point> lineNodes, PathGeometry geometry)
            : base(geometry)
        {
            LineNodes = lineNodes;
        }

        public List<System.Windows.Point> LineNodes { get; private set; }

        public override bool IsGeometryForShape(IShape shape)
        {
            var lineShape = shape as LineShape;
            if (lineShape == null)
            {
                return false;
            }

            foreach (var node in LineNodes)
            {
                if (!lineShape.LineNodes.Any(n => n.X == node.X && n.Y == node.Y))
                {
                    return false;
                }
            }

            return true;
        }

        public override void CreateRealizations(DeviceContext1 deviceContext)
        {
            //lines do not support realizations at this time
            throw new NotImplementedException();
        }
    }
}
