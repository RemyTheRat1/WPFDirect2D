using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;

namespace VectorGraphicsHelper
{
    public class VectorGeometryHelper
    {
        private readonly GeometrySink _sink;
        private Vector2 _startPoint;
        private Vector2 _previousPoint;

        public bool IsFigureOpen { get; private set; }

        public VectorGeometryHelper(GeometrySink sink)
        {
            _sink = sink;
        }

        public void Execute(IEnumerable<VectorCommand> commands)
        {
            foreach (var instruction in commands)
            {
                bool isRelative = instruction.IsRelative;

                switch (instruction.Type)
                {
                    case CommandType.Move:
                        Move(instruction, isRelative);
                        break;

                    case CommandType.Line:
                        Line(instruction, isRelative);
                        break;

                    case CommandType.HorizontalLine:
                        HorizontalLine(instruction, isRelative);
                        break;

                    case CommandType.VerticalLine:
                        VerticalLine(instruction, isRelative);
                        break;

                    case CommandType.EllipticalArc:
                        Arc(instruction, isRelative);
                        break;

                    case CommandType.CubicBezierCurve:
                        CubicBezierCurve(instruction, isRelative);
                        break;

                    case CommandType.Close:
                        Close(FigureEnd.Closed);
                        break;

                }
            }
        }

        private void HorizontalLine(VectorCommand instruction, bool isRelative)
        {
            var points = new List<Vector2>();
            for (var i = 0; i < instruction.Arguments.Length; i++)
            {
                var point = new Vector2(instruction.Arguments[i], _previousPoint.Y);
                if (isRelative)
                {
                    point += new Vector2(_previousPoint.X, 0);
                }
                points.Add(point);
                _previousPoint = points[i];
            }
            _sink.AddLines(points.ToRawVector2().ToArray());
        }

        private void VerticalLine(VectorCommand instruction, bool isRelative)
        {
            var points = new List<Vector2>();
            for (var i = 0; i < instruction.Arguments.Length; i++)
            {
                var point = new Vector2(_previousPoint.X, instruction.Arguments[i]);
                if (isRelative)
                    point += new Vector2(0, _previousPoint.Y);
                points.Add(point);
                _previousPoint = points[i];
            }
            _sink.AddLines(points.ToRawVector2().ToArray());
        }

        private void Move(VectorCommand instruction, bool isRelative)
        {
            if (IsFigureOpen)
            {
                Close(FigureEnd.Open);
            }

            var point = new Vector2(instruction.Arguments[0], instruction.Arguments[1]);
            if (isRelative)
            {
                point += _startPoint;
            }
            _startPoint = isRelative ? point + _startPoint : point;
            _previousPoint = _startPoint;
            _sink.BeginFigure(_startPoint, FigureBegin.Filled);
            IsFigureOpen = true;
        }

        private void Line(VectorCommand instruction, bool isRelative)
        {
            var points = new List<Vector2>();
            for (var i = 0; i < instruction.Arguments.Length; i = i + 2)
            {
                var point = new Vector2(instruction.Arguments[i], instruction.Arguments[i + 1]);
                if (isRelative)
                {
                    point += _previousPoint;
                }
                points.Add(point);
                _previousPoint = points[i];
            }
            _sink.AddLines(points.ToRawVector2().ToArray());
        }

        private void Arc(VectorCommand instruction, bool isRelative)
        {
            for (int i = 0; i < instruction.Arguments.Length; i = i + 6)
            {
                float w = instruction.Arguments[0];
                float h = instruction.Arguments[1];
                float a = instruction.Arguments[2];
                bool isLargeArc = (int)instruction.Arguments[3] == 1;
                bool sweepDirection = (int)instruction.Arguments[4] == 1;

                var p = new Vector2(instruction.Arguments[5], instruction.Arguments[6]);
                if (isRelative)
                {
                    p += _previousPoint;
                }

                var arcSegment = new ArcSegment
                {
                    ArcSize = isLargeArc ? ArcSize.Large : ArcSize.Small,
                    RotationAngle = a,
                    SweepDirection = sweepDirection ? SweepDirection.Clockwise : SweepDirection.CounterClockwise,
                    Point = p,
                    Size = new Size2F(w, h)
                };
                _sink.AddArc(arcSegment);
            }
        }

        private void CubicBezierCurve(VectorCommand instruction, bool isRelative)
        {
            for (int i = 0; i < instruction.Arguments.Length; i = i + 6)
            {
                var p1 = new Vector2(instruction.Arguments[0], instruction.Arguments[1]);
                var p2 = new Vector2(instruction.Arguments[2], instruction.Arguments[3]);
                var p3 = new Vector2(instruction.Arguments[4], instruction.Arguments[5]);
                if (isRelative)
                {
                    p1 += _previousPoint;
                    p2 += _previousPoint;
                    p3 += _previousPoint;
                }

                var bezSegment = new BezierSegment()
                {
                    Point1 = p1,
                    Point2 = p2,
                    Point3 = p3
                };

                _sink.AddBezier(bezSegment);
            }
        }

        private void Close(FigureEnd endType)
        {
            IsFigureOpen = false;
            _sink.EndFigure(endType);
        }

    }
}
