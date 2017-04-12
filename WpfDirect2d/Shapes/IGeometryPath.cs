using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDirect2d.Shapes
{
    public interface IGeometryPath : IDisposable
    {
        /// <summary>
        /// Path describing the geometry in svg / xaml path format
        /// </summary>
        string Path { get; set; }

        GeometryType GeometryType { get; }
    }
}
