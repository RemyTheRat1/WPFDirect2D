using System.Collections.Generic;

namespace WpfDirect2d.Shapes
{
    public class VectorShape
    {
        public VectorShape()
        {
            ShapeInstances = new List<VectorShapeInstance>();
        }      

        /// <summary>
        /// Path describing the geometry in svg / xaml path format
        /// </summary>
        public string GeometryPath { get; set; }

        /// <summary>
        /// Collection of instances of this geometry / vector. Each instance has its own width/height, location, and color
        /// </summary>
        public List<VectorShapeInstance> ShapeInstances { get; private set; }
    }
}
