using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SharpDX;
using VectorGraphicsHelper;
using WpfDirect2d.Shapes;
using Point = System.Windows.Point;
using Wpf = System.Windows.Media;
using SharpDX.Direct2D1;

namespace WpfDirect2d
{
    /// <summary>
    /// Interaction logic for Direct2dSurface.xaml
    /// </summary>
    public partial class Direct2dSurface : UserControl
    {
        private const double ZOOM_IN_FACTOR = 1.1;
        private const double ZOOM_OUT_FACTOR = 0.9;

        private bool _isRenderInitialized;
        private DeviceContext1 _context;

        private Point _mouseMoveStartPoint;
        private bool _isPanning;
        private Factory1 _d2dFactory;

        private bool _renderRequiresInit;
        private readonly List<BaseGeometry> _createdGeometries;
        private readonly Dictionary<Wpf.Color, SolidColorBrush> _brushResources;

        #region Dependency Properties

        public static readonly DependencyProperty ShapesProperty =
            DependencyProperty.Register("Shapes", typeof(IEnumerable<IShape>), typeof(Direct2dSurface), new PropertyMetadata(new PropertyChangedCallback(OnShapesChanged)));

        private static void OnShapesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Direct2dSurface;
            if (control != null && control._isRenderInitialized)
            {
                control.SyncBrushesWithShapes();
                control.SyncGeometriesWithShapes();
            }
            control?.RequestRender();
        }

        public static readonly DependencyProperty SelectedShapeProperty =
            DependencyProperty.Register("SelectedShape", typeof(IShape), typeof(Direct2dSurface), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        public static readonly DependencyProperty IsMouseWheelZoomEnabledProperty =
            DependencyProperty.Register("IsMouseWheelZoomEnabled", typeof(bool), typeof(Direct2dSurface), new PropertyMetadata(false));

        public static readonly DependencyProperty IsPanningEnabledProperty =
            DependencyProperty.Register("IsPanningEnabled", typeof(bool), typeof(Direct2dSurface));

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

        #endregion

        public Direct2dSurface()
        {
            InitializeComponent();
            _createdGeometries = new List<BaseGeometry>();
            _brushResources = new Dictionary<Wpf.Color, SolidColorBrush>();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        /// <summary>
        /// Request a render of the geometries defined in the Shapes DP
        /// </summary>
        public void RequestRender()
        {
            InteropImage.RequestRender();
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

                SyncBrushesWithShapes();
                SyncGeometriesWithShapes();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _context.Dispose();
            foreach (var geometry in _createdGeometries)
            {
                geometry.Dispose();
            }
            foreach (var brush in _brushResources)
            {
                brush.Value.Dispose();
            }

            _createdGeometries.Clear();
            _brushResources.Clear();

            _isRenderInitialized = false;
        }

        private void InitializeRenderer(IntPtr handle)
        {
            //if not null dispose the render target
            _context?.Dispose();

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
        }

        private void Render(IntPtr resourcePointer, bool isNewSurface)
        {
            if (_context == null || _renderRequiresInit || isNewSurface)
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
                var pathGeometry = _createdGeometries.FirstOrDefault(g => g.IsGeometryForShape(shape));
                if (pathGeometry != null)
                {
                    //get the fill and stroke brushes
                    var fillBrush = _brushResources[shape.FillColor];
                    var strokeBrush = _brushResources[shape.StrokeColor];
                    var selectedBrush = _brushResources[shape.SelectedColor];

                    //translate the location by the pixel location of the geometry                        
                    //then scale the geometry by its scaling factor
                    //then scale the geometry by the zoom scale factor (wpf will do transform for zooming, not needed here)
                    //then translate by the pan translation amount (wpf will do the transform for panning, not needed here)

                    var vectorShape = shape as VectorShape;
                    if (vectorShape != null)
                    {
                        _context.Transform = Matrix3x2.Translation(vectorShape.PixelXLocation, vectorShape.PixelYLocation)
                            * Matrix3x2.Scaling(vectorShape.Scaling);

                        //render the fill color
                        _context.FillGeometry(pathGeometry.Geometry, shape.IsSelected ? selectedBrush : fillBrush);
                    }
                    else
                    {
                        _context.Transform = Matrix3x2.Identity;
                    }

                    //render the geometry
                    _context.DrawGeometry(pathGeometry.Geometry, shape.IsSelected ? selectedBrush : strokeBrush, shape.StrokeWidth);
                }
            }
            _context.EndDraw();
        }

        private void SyncGeometriesWithShapes()
        {
            if (_context == null || _context.Factory == null || Shapes == null)
            {
                return;
            }

            var geometriesToAdd = new List<BaseGeometry>();

            foreach (var shape in Shapes)
            {
                if (_createdGeometries.All(g => !g.IsGeometryForShape(shape)))
                {
                    //vector not created, make it here and store for later
                    var geometry = CreateGeometry(shape);
                    if (geometry != null)
                    {
                        geometriesToAdd.Add(geometry);
                    }
                }
            }

            //get list of geometries that are no longer in the Shapes collection, and delete them
            foreach (var geometryToDelete in _createdGeometries.Where(geometry => Shapes.All(s => !geometry.IsGeometryForShape(s))).ToList())
            {
                geometryToDelete.Dispose();
                _createdGeometries.Remove(geometryToDelete);
            }

            //add the new geometries
            _createdGeometries.AddRange(geometriesToAdd);
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
                return new GeometryPath(vectorShape.GeometryPath, geometry);
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
                return new LineGeometry(lineShape.LineNodes, geometry);
            }

            return null;
        }

        private void SyncBrushesWithShapes()
        {
            if (_context == null || _context.Factory == null || Shapes == null)
            {
                return;
            }

            //add any missing brushes
            foreach (var instance in Shapes)
            {
                if (_brushResources.All(b => b.Key != instance.FillColor))
                {
                    //color missing, add it
                    var solidBrush = new SolidColorBrush(_context, instance.FillColor.ToDirect2dColor());
                    _brushResources.Add(instance.FillColor, solidBrush);
                }

                if (_brushResources.All(b => b.Key != instance.StrokeColor))
                {
                    //color missing, add it
                    var solidBrush = new SolidColorBrush(_context, instance.StrokeColor.ToDirect2dColor());
                    _brushResources.Add(instance.StrokeColor, solidBrush);
                }

                if (_brushResources.All(b => b.Key != instance.SelectedColor))
                {
                    //color missing, add it
                    var solidBrush = new SolidColorBrush(_context, instance.SelectedColor.ToDirect2dColor());
                    _brushResources.Add(instance.SelectedColor, solidBrush);
                }
            }

            var colorsToDelete = new List<Wpf.Color>();

            //delete any brushes not in use anymore
            foreach (var color in _brushResources.Keys)
            {
                bool colorFound = Shapes.Any(instance => instance.FillColor == color || instance.StrokeColor == color || instance.SelectedColor == color);
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

            //ZoomScale = workPieceImageZoom.M11;
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
            if (!IsPanningEnabled)
            {
                return;
            }

            if (!_isPanning)
            {
                var mousePosition = e.GetPosition(InteropHost);
                var testPoint = new Vector2((float)mousePosition.X, (float)mousePosition.Y);
                IShape selectedShape = null;

                //do a hit test to see what shape is being clicked on
                foreach (var shape in Shapes)
                {
                    var pathGeometry = _createdGeometries.FirstOrDefault(g => g.IsGeometryForShape(shape));
                    if (pathGeometry != null)
                    {
                        var translation = Matrix3x2.Identity;
                        var vectorShape = shape as VectorShape;
                        if (vectorShape != null)
                        {
                            translation = Matrix3x2.Translation(vectorShape.PixelXLocation, vectorShape.PixelYLocation) * Matrix3x2.Scaling(vectorShape.Scaling);
                        }

                        if (pathGeometry.Geometry.FillContainsPoint(testPoint, translation, 4f) || pathGeometry.Geometry.StrokeContainsPoint(testPoint, shape.StrokeWidth))
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
    }
}
