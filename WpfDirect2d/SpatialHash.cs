using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SharpDX.Mathematics.Interop;
using WpfDirect2D.Shapes;

namespace WpfDirect2D
{
    public class SpatialHash
    {
        private Dictionary<int, List<VectorShape>> _buckets;

        public SpatialHash(int screenWidth, int screenHeight, int cellSize)
        {
            InitializeBuckets(screenWidth, screenHeight, cellSize);
        }

        public int Columns { get; private set; }

        public int Rows { get; private set; }

        public int CellSize { get; set; }

        private void InitializeBuckets(int screenWidth, int screenHeight, int cellSize)
        {
            CellSize = cellSize;
            Columns = screenWidth / cellSize;
            Rows = screenHeight / cellSize;
            _buckets = new Dictionary<int, List<VectorShape>>(Columns * Rows);
        }

        public void ClearBuckets()
        {
            _buckets.Clear();
        }

        public void RegisterItem(VectorShape shape, RawRectangleF bounds)
        {
            var cellIds = GetIdForShape(shape, bounds);
            foreach (var cellId in cellIds)
            {
                AddShape(cellId, shape);
            }
        }

        public List<VectorShape> GetNearbyShapes(VectorShape shape, RawRectangleF bounds)
        {
            var bucketIds = GetIdForShape(shape, bounds);
            var nearbyShapes = new List<VectorShape>();
            foreach (var id in bucketIds)
            {
                if (_buckets.ContainsKey(id))
                {
                    nearbyShapes.AddRange(_buckets[id]);
                }
            }

            return nearbyShapes;
        }

        private void AddShape(int cell, VectorShape shape)
        {
            if (!_buckets.ContainsKey(cell))
            {
                _buckets.Add(cell, new List<VectorShape>());
            }

            if (_buckets[cell] == null)
            {
                _buckets[cell] = new List<VectorShape>();
            }

            _buckets[cell].Add(shape);
        }

        private List<int> GetIdForShape(VectorShape shape, RawRectangleF bounds)
        {
            var idList = new List<int>();

            float xRadius = bounds.Right - bounds.Left;
            float yRadius = bounds.Top - bounds.Bottom;
            var min = new Vector(shape.PixelXLocation - xRadius, shape.PixelYLocation - yRadius);
            var max = new Vector(shape.PixelXLocation + xRadius, shape.PixelYLocation + yRadius);

            float width = Columns;
            //TopLeft
            AddBucket(min, width, idList);
            //TopRight
            AddBucket(new Vector(max.X, min.Y), width, idList);
            //BottomRight
            AddBucket(new Vector(max.X, max.Y), width, idList);
            //BottomLeft
            AddBucket(new Vector(min.X, max.Y), width, idList);

            return idList;
        }

        private void AddBucket(Vector vector, float width, List<int> bucketToAddTo)
        {
            int cellPosition = (int)(
                Math.Floor(vector.X / CellSize) +
                Math.Floor(vector.Y / CellSize) *
                width
            );

            if (!bucketToAddTo.Contains(cellPosition))
            {
                bucketToAddTo.Add(cellPosition);
            }
        }
    }
}
