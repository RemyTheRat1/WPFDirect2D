using NSubstitute;
using NUnit.Framework;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using VectorGraphicsHelper;

namespace VectorGraphicsHelperTests
{
    public class VectorGeometryHelperTests
    {
        private GeometrySink _sink;
        private VectorGeometryHelper _sut;

        [SetUp]
        public void Init()
        {
            _sink = Substitute.For<GeometrySink>();
            _sut = new VectorGeometryHelper(_sink);
        }

        [Test]
        public void MoveFigureClosedTest()
        {
            string svg = "M 28,27";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            var startPoint = new RawVector2(28f, 27f);
            _sink.Received(1).BeginFigure(startPoint, FigureBegin.Filled);

            Assert.IsTrue(_sut.IsFigureOpen);
        }

        [Test]
        public void MoveFigureOpenTest()
        {
            string svg = "M 28,27";
            var commands = VectorGraphicParser.ParsePathData(svg);
            //this will open the figure
            _sut.Execute(commands);
            _sink.ClearReceivedCalls();

            //execute it again, and the figure should be closed and then reopened
            _sut.Execute(commands);

            var startPoint = new RawVector2(28f, 27f);
            _sink.Received(1).BeginFigure(startPoint, FigureBegin.Filled);

            Assert.IsTrue(_sut.IsFigureOpen);
            _sink.Received(1).EndFigure(FigureEnd.Open);
        }

        [Test]
        public void LineTest()
        {
            string svg = "L 28,27";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            _sink.Received(1).AddLines(Arg.Is<RawVector2[]>(x => x.Length == 1 && x[0].X == 28 && x[0].Y == 27));
        }

        [Test]
        public void HorizontalLineTest()
        {
            string svg = "M 28,27 H 13";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            //horizontal line uses the y position from the move command
            _sink.Received(1).AddLines(Arg.Is<RawVector2[]>(x => x.Length == 1 && x[0].X == 13 && x[0].Y == 27));
        }

        [Test]
        public void VerticalLineTest()
        {
            string svg = "M 28,27 V 13";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            //vertical line uses the x position from the move command
            _sink.Received(1).AddLines(Arg.Is<RawVector2[]>(x => x.Length == 1 && x[0].X == 28 && x[0].Y == 13));
        }

        [TestCase(true, SweepDirection.Clockwise)]
        [TestCase(false, SweepDirection.Clockwise)]
        [TestCase(true, SweepDirection.CounterClockwise)]
        [TestCase(false, SweepDirection.CounterClockwise)]
        public void ArcTest(bool isLargeArc, SweepDirection sweepDirection)
        {
            float rotation = 25;
            float width = 10;
            float height = 10;
            float x = 100;
            float y = 200;

            string svg = $"A {width} {height} {rotation} " + (isLargeArc ? " 1" : " 0") 
                + (sweepDirection == SweepDirection.Clockwise ? " 1" : " 0")
                + " " + x + " " + y;
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            var arcSegment = new ArcSegment
            {
                ArcSize = isLargeArc ? ArcSize.Large : ArcSize.Small,
                RotationAngle = rotation,
                SweepDirection = sweepDirection,
                Point = new Vector2(x, y),
                Size = new Size2F(width, height)
            };
            _sink.Received(2).AddArc(arcSegment);
        }

        [Test]
        public void CubicBezierCurveTest()
        {
            float x1 = 10;
            float y1 = 20;
            float x2 = 30;
            float y2 = 35;
            float x3 = 0;
            float y3 = 5;

            string svg = $"C {x1} {y1} {x2} {y2} {x3} {y3}";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            var bezSegment = new BezierSegment()
            {
                Point1 = new Vector2(x1, y1),
                Point2 = new Vector2(x2, y2),
                Point3 = new Vector2(x3, y3),
            };
            _sink.Received(1).AddBezier(bezSegment);
        }

        [Test]
        public void CloseTest()
        {
            string svg = "Z";
            var commands = VectorGraphicParser.ParsePathData(svg);
            _sut.Execute(commands);

            _sink.Received(1).EndFigure(FigureEnd.Closed);
            Assert.IsFalse(_sut.IsFigureOpen);
        }
    }
}
