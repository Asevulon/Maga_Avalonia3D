using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Reactive;

namespace SurfaceLib;

public partial class Surface : UserControl
{
    private SurfaceView _surfaceView;

    public static readonly StyledProperty<Color> SurfaceColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(SurfaceColor), Colors.AliceBlue);
    public Color SurfaceColor
    {
        get => GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }

    public static readonly StyledProperty<Color> AxesColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(AxesColor), Colors.Gold);
    public Color AxesColor
    {
        get => GetValue(AxesColorProperty);
        set => SetValue(AxesColorProperty, value);
    }

    public static readonly StyledProperty<bool> ShowAxesProperty =
    AvaloniaProperty.Register<Surface, bool>(nameof(ShowAxes), true);
    public bool ShowAxes
    {
        get => GetValue(ShowAxesProperty);
        set => SetValue(ShowAxesProperty, value);
    }

    public static readonly StyledProperty<Color> ClearColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(ClearColor), Colors.Black);
    public Color ClearColor
    {
        get => GetValue(ClearColorProperty);
        set => SetValue(ClearColorProperty, value);
    }

    public static readonly StyledProperty<Color> LightColorProperty =
    AvaloniaProperty.Register<Surface, Color>(nameof(LightColor), Colors.White);
    public Color LightColor
    {
        get => GetValue(LightColorProperty);
        set => SetValue(LightColorProperty, value);
    }

    public static readonly StyledProperty<double> LightPositionXProperty =
    AvaloniaProperty.Register<Surface, double>(nameof(LightPositionX), 0.0);
    public double LightPositionX
    {
        get => GetValue(LightPositionXProperty);
        set => SetValue(LightPositionXProperty, value);
    }

    public static readonly StyledProperty<double> LightPositionYProperty =
        AvaloniaProperty.Register<Surface, double>(nameof(LightPositionY), 0.0);
    public double LightPositionY
    {
        get => GetValue(LightPositionYProperty);
        set => SetValue(LightPositionYProperty, value);
    }

    public static readonly StyledProperty<double> LightPositionZProperty =
        AvaloniaProperty.Register<Surface, double>(nameof(LightPositionZ), 3.0);
    public double LightPositionZ
    {
        get => GetValue(LightPositionZProperty);
        set => SetValue(LightPositionZProperty, value);
    }

    public static readonly StyledProperty<int> DigitsProperty =
        AvaloniaProperty.Register<Surface, int>(nameof(DigitsProperty), 2);
    public int Digits
    {
        get => GetValue(DigitsProperty);
        set => SetValue(DigitsProperty, value);
    }

    public static readonly StyledProperty<float>YawProperty = 
        AvaloniaProperty.Register<Surface, float>(nameof(YawProperty ),0);
    public float Yaw
    {
        get => GetValue(YawProperty);
        set => SetValue(YawProperty, value);
    }

    public static readonly StyledProperty<float>PitchProperty = 
        AvaloniaProperty.Register<Surface, float>(nameof(PitchProperty ),0);
    public float Pitch
    {
        get => GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    public static readonly StyledProperty<float> DistanceProperty =
        AvaloniaProperty.Register<Surface, float>(nameof(DistanceProperty), 1);
    public float Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    public static readonly StyledProperty<List<Vector3>> PointsProperty =
        AvaloniaProperty.Register<Surface, List<Vector3>>(nameof(PointsProperty), new List<Vector3>());
    public List<Vector3> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }


    public Surface()
    {
        InitializeComponent();

        // Создаем SurfaceView вместо SurfaceRenderer
        _surfaceView = new SurfaceView();

        // Подписываемся на изменения свойств
        RegisterPropertyChangedCallbacks();

        // Добавляем SurfaceView в UI вместо SurfaceRenderer
        grid.Children.Add(_surfaceView);

        grid.LayoutUpdated += (sender, e) =>
        {
            _surfaceView.Width = grid.Bounds.Width;
            _surfaceView.Height = grid.Bounds.Height;
        };

    }

    private void RegisterPropertyChangedCallbacks()
    {
        // Регистрируем обработчики изменений для всех свойств
        this.GetObservable(SurfaceColorProperty).Subscribe(_ => _surfaceView.SurfaceColor = ColorToVector3(SurfaceColor));
        this.GetObservable(AxesColorProperty).Subscribe(_ => _surfaceView.AxesColor = ColorToVector3(AxesColor));
        this.GetObservable(ShowAxesProperty).Subscribe(_ => _surfaceView.ShowAxes = ShowAxes);
        this.GetObservable(ClearColorProperty).Subscribe(_ => _surfaceView.ClearColor = ColorToVector3(ClearColor));
        this.GetObservable(LightColorProperty).Subscribe(_ => _surfaceView.LightColor = ColorToVector3(LightColor));
        this.GetObservable(LightPositionXProperty).Subscribe(_ => _surfaceView.LightPosition = new Vector3((float)LightPositionX, (float)LightPositionY, (float)LightPositionZ));
        this.GetObservable(LightPositionYProperty).Subscribe(_ => _surfaceView.LightPosition = new Vector3((float)LightPositionX, (float)LightPositionY, (float)LightPositionZ));
        this.GetObservable(LightPositionZProperty).Subscribe(_ => _surfaceView.LightPosition = new Vector3((float)LightPositionX, (float)LightPositionY, (float)LightPositionZ));
        this.GetObservable(DigitsProperty).Subscribe(_ => _surfaceView.Digits = Digits);
        this.GetObservable(YawProperty).Subscribe(_ => _surfaceView.Camera.Yaw = Yaw);
        this.GetObservable(PitchProperty).Subscribe(_ => _surfaceView.Camera.Pitch = Pitch);
        this.GetObservable(DistanceProperty).Subscribe(_ => _surfaceView.Camera.Distance = Distance);
        this.GetObservable(PointsProperty).Subscribe(_ => _surfaceView.SetSurfacePoints(Points));
    }

    Vector3 ColorToVector3(Color c) => new Vector3(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);
}

//using Avalonia;
//using Avalonia.Controls;
//using Maga_Avalonia3D.Classes;
//using System.Collections.Generic;
//using System.Numerics;

//namespace Maga_Avalonia3D;

//public partial class Surface : UserControl
//{
//    private SceneRenderer _sceneRenderer;

//    public Surface()
//    {
//        InitializeComponent();

//        _sceneRenderer = new SceneRenderer();
//        _sceneRenderer.Width = canvas.Width;
//        _sceneRenderer.Height = canvas.Height;
//        _sceneRenderer.ClearColor = new Vector3(0.1f, 0.1f, 0.1f);
//        _sceneRenderer.LightPosition = new Vector3(3.0f, 3.0f, 3.0f);
//        _sceneRenderer.LightColor = new Vector3(1.0f, 1.0f, 1.0f);

//        var primitives = new List<PrimitiveInstance>
//        {
//            // Пол
//            new() { Type = PrimitiveType.Plane, Position = new(0, -0.5f, 0), Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0.3f, 0.3f, 0.3f) },

//            // Объекты
//            new() { Type = PrimitiveType.Cube,    Position = new(-2, 0, 0), Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(1, 0, 0) },
//            new() { Type = PrimitiveType.Sphere,  Position = new(0, 0, 0),  Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0, 1, 0) },
//            new() { Type = PrimitiveType.Pyramid, Position = new(2, 0, 0),  Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0, 0, 1) }
//        };

//        _sceneRenderer.SetScene(primitives);
//        canvas.Children.Add(_sceneRenderer);
//    }
//}