using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VectorGraphicsHelper
{
    public static class VectorGraphicParser
    {
        /// <summary>
        /// Parses a string representing a Geometry Path object in the Abbreviated Geometry Syntax (svg / xaml path)
        /// </summary>
        /// <param name="pathString"></param>
        public static IEnumerable<VectorCommand> ParsePathData(string pathString)
        {
            const string separators = @"(?=[FMLHVCQSTAZmlhvcqstaz])";
            var tokens = Regex.Split(pathString, separators).Where(t => !string.IsNullOrEmpty(t));

            var result = tokens.Select(Parse);

            return result;
        }

        private static VectorCommand Parse(string pathString)
        {
            var cmd = pathString.Cast<char>().Take(1).Single();
            string remainingargs = pathString.Substring(1);
            const string separators = @"[\s,]|(?=(?<!e)-)";

            var splitArgs = Regex
                .Split(remainingargs, separators)
                .Where(t => !string.IsNullOrEmpty(t));

            float[] floatArgs = splitArgs.Select(float.Parse).ToArray();
            bool relative;
            var primitiveType = Convert(cmd, out relative);
            return new VectorCommand(primitiveType, floatArgs, relative);
        }

        private static CommandType Convert(char cmd, out bool relative)
        {
            relative = char.IsLower(cmd);
            char invCmd = char.ToLower(cmd);

            switch (invCmd)
            {
                case 'f':
                    return CommandType.FillRule;
                case 'l':
                    return CommandType.Line;
                case 'h':
                    return CommandType.HorizontalLine;
                case 'a':
                    return CommandType.EllipticalArc;
                case 'm':
                    return CommandType.Move;
                case 'v':
                    return CommandType.VerticalLine;
                case 'c':
                    return CommandType.CubicBezierCurve;
                case 'q':
                    return CommandType.QuadraticBezierCurve;
                case 's':
                    return CommandType.SmoothCubicBezierCurve;
                case 't':
                    return CommandType.SmoothQuadraticBezierCurve;
                case 'z':
                    return CommandType.Close;
                default:
                    throw new ArgumentOutOfRangeException($"Command '{cmd}' is not valid");

            }
        }
    }
}
