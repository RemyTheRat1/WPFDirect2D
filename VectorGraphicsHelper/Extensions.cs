using System.Collections.Generic;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace VectorGraphicsHelper
{
    public static class Extensions
    {
        public static List<RawVector2> ToRawVector2(this List<Vector2> vectors)
        {
            List<RawVector2> rawVectors = new List<RawVector2>();
            foreach (var vector in vectors)
            {
                rawVectors.Add(new RawVector2(vector.X, vector.Y));
            }

            return rawVectors;
        }
    }
}
