using SharpDX.Direct2D1;
using System;

namespace WpfDirect2D.Shapes
{
    internal abstract class BaseGeometry : IDisposable
    {
        private bool _disposedValue = false; // To detect redundant calls

        protected BaseGeometry(PathGeometry geometry)
        {
            Geometry = geometry;
        }

        public PathGeometry Geometry { get; protected set; }

        public abstract bool IsGeometryForShape(IShape shape);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Geometry?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }        
    }
}
