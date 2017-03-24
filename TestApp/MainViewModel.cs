using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using WpfDirect2d.Shapes;
using Prism.Mvvm;

namespace TestApp
{
    public class MainViewModel : BindableBase
    {
        private const string DOT_CIRCLE_VECTOR = "F1 M 38,27.1542C 43.99,27.1542 48.8458,32.01 48.8458,38C 48.8458,43.99 43.99,48.8458 38,48.8458C 32.01,48.8458 27.1542,43.99 27.1542,38C 27.1542,32.01 32.01,27.1542 38,27.1542 Z M 38,16.625C 49.8051,16.625 59.375,26.1949 59.375,38C 59.375,49.8051 49.8051,59.375 38,59.375C 26.1949,59.375 16.625,49.8051 16.625,38C 16.625,26.1949 26.1949,16.625 38,16.625 Z M 38,20.5833C 28.381,20.5833 20.5833,28.381 20.5833,38C 20.5833,47.619 28.381,55.4167 38,55.4167C 47.6189,55.4167 55.4167,47.619 55.4167,38C 55.4167,28.381 47.619,20.5833 38,20.5833 Z";
        private const string MOVE_VECTOR = "F1 M 34,39.1716L 34,22.4583L 28,28.0833L 28,22.5833L 36,15.0833L 44,22.5833L 44,28.0833L 38,22.4583L 38,38L 54.625,38L 49,32L 54.5,32L 62,40L 54.5,48L 49,48L 54.625,42L 36.8284,42L 25.8284,53L 34,53L 30,57L 19,57L 19,46L 23,42L 23,50.1716L 34,39.1716 Z M 40.2533,47.5333L 37.8,52.345L 37.8,55L 36.0933,55L 36.0933,52.375L 33.7467,47.5333L 35.6683,47.5333L 36.8633,50.3183L 37.0333,50.9283L 37.055,50.9283L 37.22,50.34L 38.4717,47.5333L 40.2533,47.5333 Z M 48.52,37L 46.49,37L 45.1817,34.5683L 45.0283,34.0683L 45.0067,34.0683L 44.8317,34.59L 43.5183,37L 41.48,37L 43.9,33.2667L 41.6933,29.5333L 43.7717,29.5333L 44.855,31.7717L 45.0817,32.4017L 45.1033,32.4017L 45.3383,31.7517L 46.5317,29.5333L 48.4133,29.5333L 46.1783,33.235L 48.52,37 Z M 32.04,38L 25.96,38L 25.96,37.015L 29.835,31.92L 26.28,31.92L 26.28,30.5333L 32.04,30.5333L 32.04,31.4883L 28.2483,36.6133L 32.04,36.6133L 32.04,38 Z";

        private int _numberOfItemsToRender;
        private readonly Random _random;

        public MainViewModel()
        {
            _random = new Random(10);
            ApplyNumberOfRenderItems = new DelegateCommand(() => GenerateShapes());
            NumberOfItemsToRender = 10;
            Geometries = new List<VectorShape>();

            GenerateShapes();            
        }

        public ICommand ApplyNumberOfRenderItems { get; private set; }
        
        public int NumberOfItemsToRender
        {
            get { return _numberOfItemsToRender; }
            set { SetProperty(ref _numberOfItemsToRender, value); }
        }

        public List<VectorShape> Geometries { get; private set; }

        private void GenerateShapes()
        {
            Geometries = null;
            OnPropertyChanged(() => Geometries);

            //circle icon
            var circle = new VectorShape();
            circle.GeometryPath = DOT_CIRCLE_VECTOR;

            //move icon
            var moveIcon = new VectorShape();
            moveIcon.GeometryPath = MOVE_VECTOR;

            int count = 0;
            while(count < NumberOfItemsToRender)
            {
                circle.ShapeInstances.Add(new VectorShapeInstance
                {
                    PixelXLocation = GetRandomPixelLocation(),
                    PixelYLocation = GetRandomPixelLocation(),
                    FillColor = Colors.Black,
                    StrokeColor = Colors.Black,
                    StrokeWidth = 0.5f,
                    Scaling = 0.2f
                });

                circle.ShapeInstances.Add(new VectorShapeInstance
                {
                    PixelXLocation = GetRandomPixelLocation(),
                    PixelYLocation = GetRandomPixelLocation(),
                    FillColor = Colors.Blue,
                    StrokeColor = Colors.Red,
                    StrokeWidth = 0.5f,
                    Scaling = 0.4f
                });

                moveIcon.ShapeInstances.Add(new VectorShapeInstance
                {
                    PixelXLocation = GetRandomPixelLocation(),
                    PixelYLocation = GetRandomPixelLocation(),
                    FillColor = Colors.Black,
                    StrokeColor = Colors.Black,
                    StrokeWidth = 0.5f,
                    Scaling = 0.6f
                });

                moveIcon.ShapeInstances.Add(new VectorShapeInstance
                {
                    PixelXLocation = GetRandomPixelLocation(),
                    PixelYLocation = GetRandomPixelLocation(),
                    FillColor = Colors.Blue,
                    StrokeColor = Colors.Red,
                    StrokeWidth = 0.5f,
                    Scaling = 0.8f
                });

                count += 4;
            }

            Geometries = new List<VectorShape>
            {
                circle, moveIcon
            };
            OnPropertyChanged(() => Geometries);
        }

        private int GetRandomPixelLocation()
        {
            return _random.Next(10, 600);
        }
    }
}
