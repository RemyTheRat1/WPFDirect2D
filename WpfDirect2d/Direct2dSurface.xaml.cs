using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SharpDX;
using VectorGraphicsHelper;
using WpfDirect2D.Shapes;
using Point = System.Windows.Point;
using Wpf = System.Windows.Media;
using SharpDX.Direct2D1;

namespace WpfDirect2D
{
    /// <summary>
    /// Interaction logic for Direct2dSurface.xaml
    /// </summary>
    public partial class Direct2DSurface : UserControl, IDisposable
    {
        private const double ZOOM_IN_FACTOR = 1.1;
        private const double ZOOM_OUT_FACTOR = 0.9;

        private bool _disposedValue = false; // To detect redundant calls
        private bool _isRenderInitialized;
        private DeviceContext1 _context;

        private Point _mouseMoveStartPoint;
        private bool _isPanning;
        private Factory1 _d2dFactory;
        private StrokeStyle _lineStrokeStyle;

        private bool _renderRequiresInit;
        private readonly Dictionary<Wpf.Color, SolidColorBrush> _brushResources;
        private readonly Dictionary<int, BaseGeometry> _createdGeometries;

        #region Dependency Properties

        public static readonly DependencyProperty ShapesProperty =
            DependencyProperty.Register("Shapes", typeof(IEnumerable<IShape>), typeof(Direct2DSurface), new PropertyMetadata(OnShapesChanged));

        private static void OnShapesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Direct2DSurface;
            if (control != null && control._isRenderInitialized)
            {
                control.SyncBrushesWithShapes();
                control.SyncGeometriesWithShapes();
            }
            control?.RequestRender();
        }


        public static readonly DependencyProperty AxisTransformProperty =
            DependencyProperty.Register("AxisTransform", typeof(Wpf.ScaleTransform), typeof(Direct2DSurface));

        public static readonly DependencyProperty RenderOriginProperty =
            DependencyProperty.Register("RenderOrigin", typeof(ShapeRenderOrigin), typeof(Direct2DSurface), new PropertyMetadata(ShapeRenderOrigin.Center));

        public static readonly DependencyProperty SelectedShapeProperty =
            DependencyProperty.Register("SelectedShape", typeof(IShape), typeof(Direct2DSurface), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        public static readonly DependencyProperty IsMouseWheelZoomEnabledProperty =
            DependencyProperty.Register("IsMouseWheelZoomEnabled", typeof(bool), typeof(Direct2DSurface), new PropertyMetadata(false));

        public static readonly DependencyProperty IsPanningEnabledProperty =
            DependencyProperty.Register("IsPanningEnabled", typeof(bool), typeof(Direct2DSurface));

        public static readonly DependencyProperty UseRealizationsProperty =
            DependencyProperty.Register("UseRealizations", typeof(bool), typeof(Direct2DSurface), new PropertyMetadata(true));

        public static readonly DependencyProperty RequestRerenderProperty =
            DependencyProperty.Register("RequestRerender", typeof(bool), typeof(Direct2DSurface), new PropertyMetadata(new PropertyChangedCallback(OnRequestRerender)));

        private static void OnRequestRerender(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Direct2DSurface;
            control?.RequestRender();
        }

        public bool RequestRerender
        {
            get { return (bool)GetValue(RequestRerenderProperty); }
            set { SetValue(RequestRerenderProperty, value); }
        }

        public IEnumerable<IShape> Shapes
        {
            get { return (IEnumerable<IShape>)GetValue(ShapesProperty); }
            set { SetValue(ShapesProperty, value); }
        }

        public IShape SelectedShape
        {
            get { return (IShape)GetValue(SelectedShapeProperty); }
            set { SetValue(SelectedShapeProperty, value); }
        }

        public bool IsMouseWheelZoomEnabled
        {
            get { return (bool)GetValue(IsMouseWheelZoomEnabledProperty); }
            set { SetValue(IsMouseWheelZoomEnabledProperty, value); }
        }

        public bool IsPanningEnabled
        {
            get { return (bool)GetValue(IsPanningEnabledProperty); }
            set { SetValue(IsPanningEnabledProperty, value); }
        }

        public Wpf.ScaleTransform AxisTransform
        {
            get { return (Wpf.ScaleTransform)GetValue(AxisTransformProperty); }
            set { SetValue(AxisTransformProperty, value); }
        }

        /// <summary>
        /// What placement to use when rendering Shapes, center or top left corner
        /// </summary>
        public ShapeRenderOrigin RenderOrigin
        {
            get { return (ShapeRenderOrigin)GetValue(RenderOriginProperty); }
            set { SetValue(RenderOriginProperty, value); }
        }

        public bool UseRealizations
        {
            get { return (bool)GetValue(UseRealizationsProperty); }
            set { SetValue(UseRealizationsProperty, value); }
        }

        #endregion

        public Direct2DSurface()
        {
            InitializeComponent();
            _createdGeometries = new Dictionary<int, BaseGeometry>();
            _brushResources = new Dictionary<Wpf.Color, SolidColorBrush>();

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        /// <summary>
        /// Are Geometry Realizations valid for this OS version
        /// </summary>
        public bool IsRealizationValid { get; private set; }

        /// <summary>
        /// Are Geometry Realizations enabled and also valid to use
        /// </summary>
        public bool GeometryRealizationsEnabled => UseRealizations && IsRealizationValid;

        /// <summary>
        /// Request a render of the geometries defined in the Shapes DP.
        /// </summary>
        public void RequestRender()
        {
            InteropImage.RequestRender();
        }

        protected override Wpf.HitTestResult HitTestCore(Wpf.PointHitTestParameters hitTestParameters)
        {
            return new Wpf.PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isRenderInitialized)
            {
                return;
            }

            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                //the window owner for the D3DImage is the main window handle
                InteropImage.WindowOwner = new WindowInteropHelper(parentWindow).Handle;
                //callback for when a render is requested
                InteropImage.OnRender = Render;

                _isRenderInitialized = true;

                //request one frame to be rendered            
                InteropImage.RequestRender();

                if (_d2dFactory != null)
                {
                    //create the default line stroke style
                    _lineStrokeStyle = new StrokeStyle(_d2dFactory, new StrokeStyleProperties
                    {
                        LineJoin = LineJoin.Round,
                        StartCap = CapStyle.Round,
                        EndCap = CapStyle.Round
                    });

                    SyncBrushesWithShapes();
                    SyncGeometriesWithShapes();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _context.Dispose();
                DisposeDeviceResources();

                _lineStrokeStyle.Dispose();
                _isRenderInitialized = false;
            }

            _disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        private void InitializeRenderer(IntPtr handle)
        {
            bool syncShapes = false;

            //if not null dispose the render target
            if (_context != null)
            {
                _context.Dispose();
                DisposeDeviceResources();

                syncShapes = true;
            }

            if (InteropImage.PixelHeight == 0 && InteropImage.PixelWidth == 0)
            {
                SetInteropImagePixelSize();
            }

            //create the direct3d 11 device and query interface for DXGI
            var comObject = new ComObject(handle);
            var resource = comObject.QueryInterface<SharpDX.DXGI.Resource>();

            if (_d2dFactory == null)
            {
                _d2dFactory = new Factory1();
            }

            //get a Texture2D resource from Direct3D11 to render to (back buffer)
            var texture = resource.QueryInterface<SharpDX.Direct3D11.Texture2D>();

            //from the texture create a new surface to use as a render target
            using (var surface = texture.QueryInterface<SharpDX.DXGI.Surface1>())
            {
                var properties = new RenderTargetProperties
                {
                    DpiX = 96,
                    DpiY = 96,
                    MinLevel = FeatureLevel.Level_DEFAULT,
                    PixelFormat = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                    Type = RenderTargetType.Default,
                    Usage = RenderTargetUsage.None
                };

                var renderTarget = new RenderTarget(_d2dFactory, surface, properties);
                _context = renderTarget.QueryInterface<DeviceContext1>();
            }

            comObject.Dispose();
            texture.Dispose();

            _renderRequiresInit = false;

            //check if realizations are allowed (windows 8+)
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
            {
                IsRealizationValid = true;
            }
            else
            {
                IsRealizationValid = false;
            }

            //resync shapes if needed
            if (syncShapes)
            {
                SyncBrushesWithShapes();
                SyncGeometriesWithShapes();
            }
        }

        private void DisposeDeviceResources()
        {
            foreach (var geometry in _createdGeometries)
            {
                geometry.Value.Dispose();
            }
            foreach (var brush in _brushResources)
            {
                brush.Value.Dispose();
            }

            _createdGeometries.Clear();
            _brushResources.Clear();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetInteropImagePixelSize();

            _renderRequiresInit = true;
            InteropImage.RequestRender();
        }

        private void SetInteropImagePixelSize()
        {
            double dpiScale = 1.0; // default value for 96 dpi

            // determine DPI
            // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
            PresentationSource presentationSource = PresentationSource.FromVisual(this);
            var hwndTarget = presentationSource?.CompositionTarget as HwndTarget;
            if (hwndTarget != null)
            {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            int surfWidth = (int)(ImageContainer.ActualWidth < 0 ? 0 : Math.Ceiling(ImageContainer.ActualWidth * dpiScale));
            int surfHeight = (int)(ImageContainer.ActualHeight < 0 ? 0 : Math.Ceiling(ImageContainer.ActualHeight * dpiScale));

            // notify the D3D11Image and the DxRendering component of the pixel size desired for the DirectX rendering.
            InteropImage.SetPixelSize(surfWidth, surfHeight);

            //make sure the resources are created
            SyncGeometriesWithShapes();
            SyncBrushesWithShapes();
        }

        private void Render(IntPtr resourcePointer, bool isNewSurface)
        {
            if (_context == null || _renderRequiresInit || isNewSurface || _context.IsDisposed)
            {
                InitializeRenderer(resourcePointer);
            }

            if (_context == null || Shapes == null)
            {
                return;
            }

            _context.BeginDraw();
            _context.Clear(Color.Transparent);

            //render the geometries
            foreach (var shape in Shapes)
            {
                //get the path geometry for the shape                
                var pathGeometry = _createdGeometries[shape.GeometryHash];

                //get the fill and stroke brushes
                var fillBrush = _brushResources[shape.FillColor];
                var strokeBrush = _brushResources[shape.StrokeColor];
                var selectedBrush = _brushResources[shape.SelectedColor];

                var vectorShape = shape as VectorShape;
                if (vectorShape != null)
                {
                    var transform = pathGeometry.GetRenderTransform(vectorShape.Scaling, vectorShape.PixelXLocation, vectorShape.PixelYLocation, vectorShape.Rotation, RenderOrigin);
                    if (GeometryRealizationsEnabled)
                    {
                        _context.Transform = transform;

                        //render the fill realization
                        _context.DrawGeometryRealization(pathGeometry.FilledRealization, shape.IsSelected ? selectedBrush : fillBrush);

                        //render the stroke realization
                        _context.DrawGeometryRealization(pathGeometry.StrokedRealization, strokeBrush);
                    }
                    else
                    {
                        var transformedGeometry = new TransformedGeometry(_d2dFactory, pathGeometry.Geometry, transform);

                        //render the fill color
                        _context.FillGeometry(transformedGeometry, shape.IsSelected ? selectedBrush : fillBrush);

                        //render the geometry
                        _context.DrawGeometry(transformedGeometry, strokeBrush, shape.StrokeWidth);
                    }
                }
                else
                {
                    //render the line geometry
                    //lines dont have a set point, it has a series of node points which define the line shape
                    //translating here isnt needed
                    _context.Transform = Matrix3x2.Identity;
                    _context.DrawGeometry(pathGeometry.Geometry, shape.IsSelected ? selectedBrush : strokeBrush, shape.StrokeWidth, _lineStrokeStyle);
                }
            }
            _context.EndDraw();
        }

        private void SyncGeometriesWithShapes()
        {
            if (_context?.Factory == null || Shapes == null)
            {
                return;
            }            

            foreach (var shape in Shapes)
            {
                if (_createdGeometries.ContainsKey(shape.GeometryHash)) continue;

                //vector not created, make it here and store for later
                var geometry = CreateGeometry(shape);
                if (geometry != null)
                {                        
                    if (!_createdGeometries.ContainsKey(geometry.GeometryHash))
                    {
                        _createdGeometries.Add(geometry.GeometryHash, geometry);
                    }
                }
            }

            //get list of geometries that are no longer in the Shapes collection, and delete them
            foreach (var geoHash in _createdGeometries.Keys)
            {
                if (Shapes.All(s => s.GeometryHash != geoHash))
                {
                    _createdGeometries[geoHash].Dispose();
                    _createdGeometries.Remove(geoHash);
                }
            }
        }

        private BaseGeometry CreateGeometry(IShape shape)
        {
            if (!shape.IsValid)
            {
                return null;
            }

            var vectorShape = shape as VectorShape;
            if (vectorShape != null)
            {
                var geometry = new PathGeometry(_context.Factory);
                var sink = geometry.Open();
                VectorGeometryHelper helper = new VectorGeometryHelper(sink);
                var commands = VectorGraphicParser.ParsePathData(vectorShape.GeometryPath);
                helper.Execute(commands);
                sink.Close();

                var shapeGeometry = new GeometryPath(vectorShape.GeometryPath, geometry);
                if (GeometryRealizationsEnabled)
                {
                    shapeGeometry.CreateRealizations(_context);
                }

                vectorShape.GeometryHash = shapeGeometry.GeometryHash;
                return shapeGeometry;
            }

            var lineShape = shape as LineShape;
            if (lineShape != null)
            {
                var geometry = new PathGeometry(_context.Factory);
                var sink = geometry.Open();

                //first node is the starting point
                var startingPoint = lineShape.GetStartingPoint();
                sink.BeginFigure(startingPoint.ToRawVector2(), FigureBegin.Filled);
                sink.AddLines(lineShape.GetConnectingPoints().ToRawVector2Array());
                sink.EndFigure(lineShape.IsLineClosed ? FigureEnd.Closed : FigureEnd.Open);
                sink.Close();

                var lineGeometry = new LineGeometry(lineShape.LineNodes, geometry);
                lineShape.GeometryHash = lineGeometry.GeometryHash;
                return lineGeometry;
            }

            return null;
        }

        private void SyncBrushesWithShapes()
        {
            if (_context?.Factory == null || Shapes == null)
            {
                return;
            }

            //add any missing brushes
            foreach (var instance in Shapes)
            {
                foreach (var color in instance.GetColorsToCache())
                {
                    if (_brushResources.All(b => b.Key != color))
                    {
                        //color missing, add it
                        var solidBrush = new SolidColorBrush(_context, color.ToDirect2dColor());
                        _brushResources.Add(color, solidBrush);
                    }
                }
            }

            var colorsToDelete = new List<Wpf.Color>();

            //delete any brushes not in use anymore
            foreach (var color in _brushResources.Keys)
            {
                bool colorFound = Shapes.Any(instance => instance.GetColorsToCache().Contains(color));
                if (!colorFound)
                {
                    colorsToDelete.Add(color);
                }
            }

            //remove brushes to be deleted
            foreach (var color in colorsToDelete)
            {
                var brush = _brushResources[color];
                _brushResources.Remove(color);
                brush.Dispose();
            }
        }

        private void ImageContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsMouseWheelZoomEnabled)
            {
                return;
            }

            double x = e.GetPosition(ImageContainer).X;
            double y = e.GetPosition(ImageContainer).Y;
            Zoom(e.Delta, new Point(x, y));
        }

        private void Zoom(int zoomValue, Point pointToScaleAbout)
        {
            if (!IsMouseWheelZoomEnabled)
            {
                return;
            }

            Wpf.Matrix imageZoom = ImageContainer.RenderTransform.Value;

            if (zoomValue > 0)
            {
                imageZoom.ScaleAtPrepend(ZOOM_IN_FACTOR, ZOOM_IN_FACTOR, pointToScaleAbout.X, pointToScaleAbout.Y); // Scale + about current point                    
            }
            else
            {
                imageZoom.ScaleAtPrepend(ZOOM_OUT_FACTOR, ZOOM_OUT_FACTOR, pointToScaleAbout.X, pointToScaleAbout.Y);
                if (imageZoom.M22 < 1 || imageZoom.M11 < 1)   // If scale value of zoom in either dimension is under 1, reset to identity
                {
                    imageZoom.SetIdentity();
                }
            }

            ImageContainer.RenderTransform = new Wpf.MatrixTransform(imageZoom);
        }

        private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsPanningEnabled)
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;

                Wpf.Matrix workPieceImageZoom = ImageContainer.RenderTransform.Value;

                Point currentMousePoint = e.GetPosition(ImageContainer);
                Vector dragOffset = currentMousePoint - _mouseMoveStartPoint;

                //smoothing / scaling factor for pan movements
                double scaleMultiplier = 5 * Math.Log10(1.0) + 1;
                workPieceImageZoom.Translate(dragOffset.X * scaleMultiplier, dragOffset.Y * scaleMultiplier);

                ImageContainer.RenderTransform = new Wpf.MatrixTransform(workPieceImageZoom);
            }
        }

        private void ImageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsPanningEnabled)
            {
                _mouseMoveStartPoint = e.GetPosition(InteropHost);
            }
        }

        private void ImageContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPanning && Shapes != null)
            {
                var mousePosition = e.GetPosition(InteropHost);
                var testPoint = new Vector2((float)mousePosition.X, (float)mousePosition.Y);
                IShape selectedShape = null;

                //do a hit test to see what shape is being clicked on
                foreach (var shape in Shapes)
                {
                    var pathGeometry = _createdGeometries[shape.GeometryHash];

                    var translation = Matrix3x2.Identity;
                    if (pathGeometry is GeometryPath)
                    {
                        var vectorShape = shape as VectorShape;
                        if (vectorShape != null)
                        {
                            translation = pathGeometry.GetRenderTransform(vectorShape.Scaling, vectorShape.PixelXLocation, vectorShape.PixelYLocation, vectorShape.Rotation, RenderOrigin);

                            if (pathGeometry.Geometry.FillContainsPoint(testPoint, translation, 4f))
                            {
                                var previousSelectedShape = Shapes.FirstOrDefault(s => s.IsSelected);
                                if (previousSelectedShape != null)
                                {
                                    previousSelectedShape.IsSelected = false;
                                }

                                shape.IsSelected = true;
                                selectedShape = shape;
                            }
                            else
                            {
                                shape.IsSelected = false;
                            }
                        }
                    }
                    else if (pathGeometry is LineGeometry)
                    {
                        if (pathGeometry.Geometry.StrokeContainsPoint(testPoint, shape.StrokeWidth, _lineStrokeStyle, translation, 4f))
                        {
                            var previousSelectedShape = Shapes.FirstOrDefault(s => s.IsSelected);
                            if (previousSelectedShape != null)
                            {
                                previousSelectedShape.IsSelected = false;
                            }

                            shape.IsSelected = true;
                            selectedShape = shape;
                        }
                        else
                        {
                            shape.IsSelected = false;
                        }
                    }
                }

                SelectedShape = selectedShape;
                InteropImage.RequestRender();
            }

            _isPanning = false;
        }

        private void InteropImage_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is bool) || !(e.OldValue is bool))
            {
                return;
            }

            if (!(bool)e.NewValue && (bool)e.OldValue)
            {
                _renderRequiresInit = true;

            }
            else
            {
                InteropImage.RequestRender();
            }
        }
    }
}
