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
        private const float DEFAULT_ZOOM_FACTOR = 0.08f;

        private bool _isRenderInitialized;
        private DeviceContext1 _context;

        private float _scaleFactor;
        private Matrix3x2 _zoomScaleTransform;
        private Point _mouseMoveStartPoint;
        private Matrix3x2 _panTranslateMatrix;
        private bool _isPanning;
        private Vector _lastPanDragOffset;
        private Factory1 _d2dFactory;

        private bool _renderRequiresInit;
        private readonly List<IGeometryPath> _createdGeometries;
        private readonly Dictionary<Wpf.Color, SolidColorBrush> _brushResources;

        #region Dependency Properties

        public static readonly DependencyProperty ShapesProperty =
            DependencyProperty.Register("Shapes", typeof(IEnumerable<VectorShape>), typeof(Direct2dSurface), new PropertyMetadata(new PropertyChangedCallback(OnShapesChanged)));

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

        public static readonly DependencyProperty IsMouseWheelZoomEnabledProperty =
            DependencyProperty.Register("IsMouseWheelZoomEnabled", typeof(bool), typeof(Direct2dSurface), new PropertyMetadata(false));

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register("ZoomFactor", typeof(float), typeof(Direct2dSurface), new PropertyMetadata(DEFAULT_ZOOM_FACTOR));

        public static readonly DependencyProperty IsPanningEnabledProperty =
            DependencyProperty.Register("IsPanningEnabled", typeof(bool), typeof(Direct2dSurface));                

        public IEnumerable<VectorShape> Shapes
        {
            get { return (IEnumerable<VectorShape>)GetValue(ShapesProperty); }
            set { SetValue(ShapesProperty, value); }
        }

        public bool IsMouseWheelZoomEnabled
        {
            get { return (bool)GetValue(IsMouseWheelZoomEnabledProperty); }
            set { SetValue(IsMouseWheelZoomEnabledProperty, value); }
        }

        public float ZoomFactor
        {
            get { return (float)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
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
            _createdGeometries = new List<IGeometryPath>();
            _brushResources = new Dictionary<Wpf.Color, SolidColorBrush>();
            ResetPanTranslation();
            ResetZoomScale();

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

            ResetZoomScale();
            ResetPanTranslation();

            _isRenderInitialized = false;
        }

        private void InitializeRenderer(IntPtr handle)
        {
            //if not null dispose the render target
            _context?.Dispose();

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
            using (var surface = texture.QueryInterface<SharpDX.DXGI.Surface>())
            {
                var properties = new RenderTargetProperties
                {
                    DpiX = 96,
                    DpiY = 96,
                    MinLevel = FeatureLevel.Level_DEFAULT,
                    PixelFormat = new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied),
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

            _renderRequiresInit = true;
            InteropImage.RequestRender();
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
                var pathGeometry = _createdGeometries.FirstOrDefault(g => g.Path == shape.GeometryPath);
                if (pathGeometry != null)
                {
                    foreach (var shapeInstance in shape.ShapeInstances)
                    {
                        //get the fill and stroke brushes
                        var fillBrush = _brushResources[shapeInstance.FillColor];
                        var strokeBrush = _brushResources[shapeInstance.StrokeColor];

                        //translate the location by the pixel location of the geometry                        
                        //then scale the geometry by its scaling factor
                        //then scale the geometry by the zoom scale factor
                        //then translate by the pan translation amount

                        _context.Transform = Matrix3x2.Translation(shapeInstance.PixelXLocation, shapeInstance.PixelYLocation)
                            * Matrix3x2.Scaling(shapeInstance.Scaling)
                            * _zoomScaleTransform
                            * _panTranslateMatrix;
                        
                        if (pathGeometry.GeometryType == GeometryType.Realization)
                        {
                            var geometry = (GeometryRealizationPath)pathGeometry;

                            //render the fill color
                            _context.DrawGeometryRealization(geometry.FilledGeometry, fillBrush);
                            //render the geometry
                            _context.DrawGeometryRealization(geometry.StrokedGeometry, strokeBrush);
                        }
                        else
                        {
                            var geometry = (GeometryPath)pathGeometry;

                            //render the fill color
                            _context.FillGeometry(geometry.Geometry, fillBrush);
                            //render the geometry
                            _context.DrawGeometry(geometry.Geometry, strokeBrush, shapeInstance.StrokeWidth);
                        }
                    }
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

            var geometriesToAdd = new List<IGeometryPath>();

            foreach (var shape in Shapes)
            {
                if (_createdGeometries.All(g => g.Path != shape.GeometryPath))
                {
                    //vector not created, make it here and store for later
                    var geometry = new PathGeometry(_context.Factory);
                    var sink = geometry.Open();
                    VectorGeometryHelper helper = new VectorGeometryHelper(sink);
                    var commands = VectorGraphicParser.ParsePathData(shape.GeometryPath);
                    helper.Execute(commands);
                    sink.Close();

                    if (shape.IsGeometryRealized)
                    {
                        float tolerance = 0.5f;
                        foreach (var shapeInstance in shape.ShapeInstances)
                        {
                            var fillRealization = new GeometryRealization(_context, geometry, tolerance);
                            var strokeRealization = new GeometryRealization(_context, geometry, tolerance, shapeInstance.StrokeWidth, null);
                            geometriesToAdd.Add(new GeometryRealizationPath(shape.GeometryPath, fillRealization, strokeRealization));
                        }
                    }
                    else
                    {
                        geometriesToAdd.Add(new GeometryPath(shape.GeometryPath, geometry));
                    }
                }
            }

            //get list of geometries that are no longer in the Shapes collection, and delete them
            foreach (var geometryToDelete in _createdGeometries.Where(geometry => Shapes.All(s => s.GeometryPath != geometry.Path)).ToList())
            {
                geometryToDelete.Dispose();
                _createdGeometries.Remove(geometryToDelete);
            }

            //add the new geometries
            _createdGeometries.AddRange(geometriesToAdd);
        }

        private void SyncBrushesWithShapes()
        {
            if (_context == null || _context.Factory == null || Shapes == null)
            {
                return;
            }

            //add any missing brushes
            foreach (var instance in Shapes.SelectMany(shape => shape.ShapeInstances))
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
            }

            var colorsToDelete = new List<Wpf.Color>();

            //delete any brushes not in use anymore
            foreach (var color in _brushResources.Keys)
            {
                bool colorFound = (from shape in Shapes
                                   from instance in shape.ShapeInstances.Where(instance => instance.FillColor == color || instance.StrokeColor == color)
                                   select shape).Any();

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

            //get the point the mouse is at to zoom about
            var screenPosition = e.GetPosition(InteropHost);
            Vector2 scalePoint = new Vector2((float)screenPosition.X, (float)screenPosition.Y);

            if (e.Delta < 0)
            {
                Zoom(-ZoomFactor, scalePoint);
            }
            else
            {
                Zoom(ZoomFactor, scalePoint);
            }
        }

        private void Zoom(float zoomFactor, Vector2 zoomPoint)
        {
            _scaleFactor += zoomFactor;
            _zoomScaleTransform = Matrix3x2.Scaling(_scaleFactor, _scaleFactor, zoomPoint);
            InteropImage.RequestRender();
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

                //get the current offset from the inital mouse left down point and the current move position
                Point currentMousePoint = e.GetPosition(InteropHost);
                Vector dragOffset = currentMousePoint - _mouseMoveStartPoint;
                //add to this offset the last saved drag offset, so the rendering does not start from the inital point again
                dragOffset = _lastPanDragOffset + dragOffset;
                _panTranslateMatrix = Matrix3x2.Translation((float)dragOffset.X, (float)dragOffset.Y);

                InteropImage.RequestRender();
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

            if (_isPanning)
            {
                //save the last translation values for future panning operations
                _lastPanDragOffset = new Vector(_panTranslateMatrix.TranslationVector.X, _panTranslateMatrix.TranslationVector.Y);
            }
            else
            {
                var mousePosition = e.GetPosition(InteropHost);
                var testPoint = new Vector2((float)mousePosition.X, (float)mousePosition.Y);

                //do a hit test to see what shape is being clicked on
                foreach (var shape in Shapes)
                {
                    var pathGeometry = _createdGeometries.OfType<GeometryPath>().FirstOrDefault(g => g.Path == shape.GeometryPath);
                    foreach (var shapeInstance in shape.ShapeInstances)
                    {
                        if (!pathGeometry.Geometry.FillContainsPoint(testPoint))
                        {
                            break;
                        }
                    }
                }
            }

            _isPanning = false;
        }

        private void ResetZoomScale()
        {
            _zoomScaleTransform = Matrix3x2.Identity;
            _scaleFactor = 1;
        }

        private void ResetPanTranslation()
        {
            _panTranslateMatrix = Matrix3x2.Identity;
            _lastPanDragOffset = new Vector();
        }
    }
}
