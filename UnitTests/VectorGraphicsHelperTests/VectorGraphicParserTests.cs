using NUnit.Framework;
using System;
using System.Linq;
using VectorGraphicsHelper;

namespace VectorGraphicsHelperTests
{
    public class VectorGraphicParserTests
    {
        [TestCase("F1 F23 F45", CommandType.FillRule, 3)]
        [TestCase("C1 C23", CommandType.CubicBezierCurve, 2)]
        [TestCase("A1 a7 a8 A9", CommandType.EllipticalArc, 4)]
        [TestCase("H1", CommandType.HorizontalLine, 1)]
        [TestCase("L45 L6", CommandType.Line, 2)]
        [TestCase("M2 M34.5 M1.5", CommandType.Move, 3)]
        [TestCase("Q4", CommandType.QuadraticBezierCurve, 1)]
        [TestCase("S4", CommandType.SmoothCubicBezierCurve, 1)]
        [TestCase("T4", CommandType.SmoothQuadraticBezierCurve, 1)]
        [TestCase("V4 V 5 V7", CommandType.VerticalLine, 3)]
        [TestCase("Z1 Z2", CommandType.Close, 2)]
        public void ParsePathDataRuleTest(string svgCommand, CommandType expectedType, int expectedCount)
        {            
            var commands = VectorGraphicParser.ParsePathData(svgCommand);

            Assert.AreEqual(expectedCount, commands.Count());
            foreach(var command in commands)
            {
                Assert.AreEqual(expectedType, command.Type);
            }            
        }

        [Test]
        public void ParseMultiplePathDataTest()
        {
            string svg = "F1 M 38,27.1542C 48.8458,38C 48.8458C Z M 38C 16.625 Z M 38,20.5833C 38,55.4167C 38A 38,20.5833 Z";
            var commands = VectorGraphicParser.ParsePathData(svg).ToList();

            Assert.AreEqual(14, commands.Count());
            Assert.AreEqual(CommandType.FillRule, commands[0].Type);
            Assert.AreEqual(CommandType.Move, commands[1].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[2].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[3].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[4].Type);
            Assert.AreEqual(CommandType.Close, commands[5].Type);
            Assert.AreEqual(CommandType.Move, commands[6].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[7].Type);
            Assert.AreEqual(CommandType.Close, commands[8].Type);
            Assert.AreEqual(CommandType.Move, commands[9].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[10].Type);
            Assert.AreEqual(CommandType.CubicBezierCurve, commands[11].Type);
            Assert.AreEqual(CommandType.EllipticalArc, commands[12].Type);
            Assert.AreEqual(CommandType.Close, commands[13].Type);
        }

        [Test]
        public void ParsePathDataArgumentsTest()
        {
            string svg = "F1 M 38,27.1542C";
            var commands = VectorGraphicParser.ParsePathData(svg).ToList();

            Assert.AreEqual(3, commands.Count);
            var command = commands[1];

            Assert.AreEqual(CommandType.Move, command.Type);
            Assert.AreEqual(2, command.Arguments.Count());
            Assert.AreEqual(38, command.Arguments.First());
            Assert.AreEqual(27.1542, Math.Round(command.Arguments.Last(), 4));
        }
    }
}