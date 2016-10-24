using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using SharpDX;
using SharpDX.Direct2D1;
using VectorGraphicsHelper;
using WpfDirect2d.Shapes;

namespace WpfDirect2d
{
    /// <summary>
    /// Interaction logic for Direct2dSurface.xaml
    /// </summary>
    public partial class Direct2dSurface : UserControl
    {
        private bool _renderRequiresInit;
        private Factory _d2dFactory;
        private RenderTarget _renderTarget;
        List<RenderedGeometryPath> _createdGeometries;
        Dictionary<Color, SolidColorBrush> _brushResources;

        #region Dependency Properties

        public static readonly DependencyProperty ShapesProperty =
            DependencyProperty.Register("Shapes", typeof(IEnumerable<VectorShape>), typeof(Direct2dSurface));

        public IEnumerable<VectorShape> Shapes
        {
            get { return (IEnumerable<VectorShape>)GetValue(ShapesProperty); }
            set { SetValue(ShapesProperty, value); }
        }

        #endregion

        public Direct2dSurface()
        {
            InitializeComponent();
            _createdGeometries = new List<RenderedGeometryPath>();
            _brushResources = new Dictionary<Color, SolidColorBrush>();

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                //the window owner for the D3DImage is the main window handle
                InteropImage.WindowOwner = new WindowInteropHelper(parentWindow).Handle;
                //callback for when a render is requested
                InteropImage.OnRender = Render;
            }
        }

        private void InitializeRenderer(IntPtr handle)
        {
            //if not null dispose the render target
            _renderTarget?.Dispose();

            //create the direct3d 11 device and query interface for DXGI
            var comObject = new ComObject(handle);
            var resource = comObject.QueryInterface<SharpDX.DXGI.Resource>();
            _d2dFactory = new Factory();

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

                _renderTarget = new RenderTarget(_d2dFactory, surface, properties);
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

            int surfWidth = (int)(ImageHost.ActualWidth < 0 ? 0 : Math.Ceiling(ImageHost.ActualWidth * dpiScale));
            int surfHeight = (int)(ImageHost.ActualHeight < 0 ? 0 : Math.Ceiling(ImageHost.ActualHeight * dpiScale));

            // notify the D3D11Image and the DxRendering component of the pixel size desired for the DirectX rendering.
            InteropImage.SetPixelSize(surfWidth, surfHeight);

            _renderRequiresInit = true;
            InteropImage.RequestRender();
        }

        private void Render(IntPtr resourcePointer, bool b)
        {
            if (_renderTarget == null || _renderRequiresInit)
            {
                InitializeRenderer(resourcePointer);
            }

            if (_renderTarget == null)
            {
                return;
            }

            _renderTarget.Clear(Color.Transparent);

            //check if any new shapes need to be created / disposed
            SyncGeometriesWithShapes();

            //check if any new brushes need to be created / disposed
            SyncBrushesWithShapes();

            _renderTarget.BeginDraw();

            //render the geometries
            foreach (var shape in Shapes)
            {
                //get the path geometry for the shape
                var pathGeometry = _createdGeometries.FirstOrDefault(g => g.GeometryPath == shape.GeometryPath);                
                if (pathGeometry != null)
                {
                    foreach (var shapeInstance in shape.ShapeInstances)
                    {
                        //get the fill and stroke brushes
                        var fillBrush = _brushResources[shapeInstance.FillColor];
                        var strokeBrush = _brushResources[shapeInstance.StrokeColor];

                        _renderTarget.Transform = Matrix3x2.Translation(shapeInstance.PixelXLocation, shapeInstance.PixelYLocation);
                        //render the fill color
                        _renderTarget.FillGeometry(pathGeometry.Geometry, fillBrush);
                        //render the geometry
                        _renderTarget.DrawGeometry(pathGeometry.Geometry, strokeBrush, shapeInstance.StrokeWidth);
                    }                    
                }                
            }

            _renderTarget.EndDraw();
        }

        private void SyncGeometriesWithShapes()
        {
            var geometriesToAdd = new List<RenderedGeometryPath>();

            foreach (var shape in Shapes)
            {
                if (_createdGeometries.All(g => g.GeometryPath != shape.GeometryPath))
                {
                    //vector not created, make it here and store for later
                    var geometry = new PathGeometry(_d2dFactory);
                    var sink = geometry.Open();
                    VectorGeometryHelper helper = new VectorGeometryHelper(sink);
                    var commands = VectorGraphicParser.ParsePathData(shape.GeometryPath);
                    helper.Execute(commands);
                    sink.Close();

                    geometriesToAdd.Add(new RenderedGeometryPath(shape.GeometryPath, geometry));
                }
            }

            //get list of geometries that are no longer in the Shapes collection, and delete them
            foreach (var geometryToDelete in _createdGeometries.Where(geometry => Shapes.All(s => s.GeometryPath != geometry.GeometryPath)).ToList())
            {
                geometryToDelete.Dispose();
                _createdGeometries.Remove(geometryToDelete);
            }

            //add the new geometries
            _createdGeometries.AddRange(geometriesToAdd);
        }

        private void SyncBrushesWithShapes()
        {
            //add any missing brushes
            foreach (var instance in Shapes.SelectMany(shape => shape.ShapeInstances))
            {
                if (_brushResources.All(b => b.Key != instance.FillColor))
                {
                    //color missing, add it
                    var solidBrush = new SolidColorBrush(_renderTarget, instance.FillColor);
                    _brushResources.Add(instance.FillColor, solidBrush);
                }

                if (_brushResources.All(b => b.Key != instance.StrokeColor))
                {
                    //color missing, add it
                    var solidBrush = new SolidColorBrush(_renderTarget, instance.FillColor);
                    _brushResources.Add(instance.StrokeColor, solidBrush);
                }
            }

            var colorsToDelete = new List<Color>();

            //delete any brushes not in use anymore
            foreach (var color in _brushResources.Keys)
            {
                bool colorFound = (from shape in Shapes
                                   from instance in shape.ShapeInstances.Where(instance => instance.FillColor == color || instance.StrokeColor == color)
                                   select shape).Any();

                if (colorFound)
                {
                    colorsToDelete.Add(color);
                }
            }

            //remove brushes to be deleted
            foreach (var color in colorsToDelete)
            {
                _brushResources.Remove(color);
            }
        }
    }
}
