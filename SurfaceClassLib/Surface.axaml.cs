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

    public Surface()
    {
        InitializeComponent();

        // Создаем SurfaceView вместо SurfaceRenderer
        _surfaceView = new SurfaceView();

        // Подписываемся на изменения свойств
        RegisterPropertyChangedCallbacks();

        // Генерируем точки для гауссова купола
        var gaussianPoints = GenerateGaussianDomePoints(200);

        // Устанавливаем точки поверхности через SurfaceView
        _surfaceView.SetSurfacePoints(gaussianPoints);

        // Добавляем SurfaceView в UI вместо SurfaceRenderer
        grid.Children.Add(_surfaceView);

        grid.LayoutUpdated += (sender, e) =>
        {
            _surfaceView.Width = grid.Bounds.Width;
            _surfaceView.Height = grid.Bounds.Height;
        };

    }

    public List<Vector3> SurfacePoints
    {
        set => _surfaceView.SetSurfacePoints(value);
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
    }

    Vector3 ColorToVector3(Color c) => new Vector3(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);

    private List<Vector3> GenerateGaussianDomePoints(int pointCount)
    {
        var points = new List<Vector3>();
        Random random = new Random();

        for (int i = 0; i < pointCount; i++)
        {
            // Генерируем случайные точки в квадрате [-2, 2] x [-2, 2]
            float x = (float)(random.NextDouble() * 4 - 2);
            float y = (float)(random.NextDouble() * 4 - 2);

            // Вычисляем высоту по гауссовой функции
            float distanceSquared = x * x + y * y;
            float z = (float)Math.Exp(-distanceSquared * 0.5f) * 1.5f;

            points.Add(new Vector3(x, y, z));
        }

        return points;
    }
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