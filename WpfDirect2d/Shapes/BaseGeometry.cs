using SharpDX.Direct2D1;
using System;
using SharpDX;
using SharpDX.Mathematics.Interop;

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

        public RawRectangleF GetBounds(Matrix3x2 scaleTransform)
        {
            return Geometry.GetBounds(scaleTransform);
        }

        public Matrix3x2 GetRenderTransform(float scaleFactor, float xLocation, float yLocation, ShapeRenderOrigin renderOrigin)
        {
            if (renderOrigin == ShapeRenderOrigin.Center)
            {
                return GetCenterRenderTransform(scaleFactor, xLocation, yLocation);
            }

            return GetTopLeftRenderTransform(scaleFactor, xLocation, yLocation);
        }

        private Matrix3x2 GetCenterRenderTransform(float scaleFactor, float xLocation, float yLocation)
        {
            var scaleTransform = Matrix3x2.Scaling(scaleFactor);
            var geometryBounds = GetBounds(scaleTransform);
            float centerScalingOffset = scaleFactor * 4;
            float xTranslate = xLocation - (geometryBounds.Right - geometryBounds.Left) + centerScalingOffset;
            float yTranslate = yLocation - (geometryBounds.Bottom - geometryBounds.Top) + centerScalingOffset;

            return scaleTransform * Matrix3x2.Translation(xTranslate, yTranslate);
        }

        private Matrix3x2 GetTopLeftRenderTransform(float scaleFactor, float xLocation, float yLocation)
        {
            return Matrix3x2.Scaling(scaleFactor) * Matrix3x2.Translation(xLocation, yLocation);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Geometry?.Dispose();
                }

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
