using SharpDX;
using System;
using System.Collections.Generic;
using Wpf = System.Windows.Media;
using Windows= System.Windows;
using SharpDX.Mathematics.Interop;

namespace WpfDirect2d
{
    internal static class Extensions
    {
        public static Color ToDirect2dColor(this Wpf.Color wpfColor)
        {
            return new Color(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
        }

        public static RawVector2 ToRawVector2(this Windows.Point point)
        {
            var rawVector = new RawVector2()
            {
                X = Convert.ToSingle(point.X),
                Y = Convert.ToSingle(point.Y)
            };
            return rawVector;
        }

        public static RawVector2[] ToRawVector2Array(this List<Windows.Point> points)
        {
            List<RawVector2> rawVectors = new List<RawVector2>();
            foreach (var wPoint in points)
            {
                rawVectors.Add(wPoint.ToRawVector2());
            }

            return rawVectors.ToArray();
        }
    }
}
