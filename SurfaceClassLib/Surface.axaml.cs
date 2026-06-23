using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Reactive;
using System.Runtime.CompilerServices;

namespace SurfaceLib;

/// <summary>
/// Класс для отрисовки поверхностей с высокоуровневым API
/// </summary>
public partial class Surface : UserControl
{
    // Низкоуровневый бэкэнд
    private SurfaceView _surfaceView;

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<Color> SurfaceColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(SurfaceColor), Colors.AliceBlue);
    /// <summary>
    /// Цвет поверхности
    /// </summary>
    public Color SurfaceColor
    {
        get => GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<Color> AxesColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(AxesColor), Colors.Gold);

    /// <summary>
    /// Цвет осей координат
    /// </summary>
    public Color AxesColor
    {
        get => GetValue(AxesColorProperty);
        set => SetValue(AxesColorProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<Color> AxesCaptionColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(AxesCaptionColor), Colors.White);
    /// <summary>
    /// Цвет подписей к осям координат
    /// </summary>
    public Color AxesCaptionColor
    {
        get => GetValue(AxesCaptionColorProperty);
        set => SetValue(AxesCaptionColorProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<bool> ShowAxesProperty =
    AvaloniaProperty.Register<Surface, bool>(nameof(ShowAxes), true);

    /// <summary>
    /// Показывать ли оси координат
    /// </summary>
    public bool ShowAxes
    {
        get => GetValue(ShowAxesProperty);
        set => SetValue(ShowAxesProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<Color> ClearColorProperty =
        AvaloniaProperty.Register<Surface, Color>(nameof(ClearColor), Colors.Black);

    /// <summary>
    /// Цвет фона
    /// </summary>
    public Color ClearColor
    {
        get => GetValue(ClearColorProperty);
        set => SetValue(ClearColorProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<Color> LightColorProperty =
    AvaloniaProperty.Register<Surface, Color>(nameof(LightColor), Colors.White);

    /// <summary>
    /// Цвет источника освещения
    /// </summary>
    public Color LightColor
    {
        get => GetValue(LightColorProperty);
        set => SetValue(LightColorProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<double> LightPositionXProperty =
    AvaloniaProperty.Register<Surface, double>(nameof(LightPositionX), 0.0);

    /// <summary>
    /// Позиция света, координата X
    /// </summary>
    public double LightPositionX
    {
        get => GetValue(LightPositionXProperty);
        set => SetValue(LightPositionXProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<double> LightPositionYProperty =
        AvaloniaProperty.Register<Surface, double>(nameof(LightPositionY), 0.0);

    /// <summary>
    /// Позиция света, координата Y
    /// </summary>
    public double LightPositionY
    {
        get => GetValue(LightPositionYProperty);
        set => SetValue(LightPositionYProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<double> LightPositionZProperty =
        AvaloniaProperty.Register<Surface, double>(nameof(LightPositionZ), 10.0);

    /// <summary>
    /// Позиция света, координата Z
    /// </summary>
    public double LightPositionZ
    {
        get => GetValue(LightPositionZProperty);
        set => SetValue(LightPositionZProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<int> DigitsProperty =
        AvaloniaProperty.Register<Surface, int>(nameof(DigitsProperty), 2);

    /// <summary>
    /// Количество символов после запятой у подписей к осям координат
    /// </summary>
    public int Digits
    {
        get => GetValue(DigitsProperty);
        set => SetValue(DigitsProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<float>YawProperty = 
        AvaloniaProperty.Register<Surface, float>(nameof(YawProperty ),0);

    /// <summary>
    /// Поворот вокруг центральной вертикальной оси (ось Z)
    /// </summary>
    public float Yaw
    {
        get => GetValue(YawProperty);
        set => SetValue(YawProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<float>PitchProperty = 
        AvaloniaProperty.Register<Surface, float>(nameof(PitchProperty ),0);

    /// <summary>
    /// Наклон поверхности вокруг оси X
    /// </summary>
    public float Pitch
    {
        get => GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<float> DistanceProperty =
        AvaloniaProperty.Register<Surface, float>(nameof(DistanceProperty), 1.1f);

    /// <summary>
    /// Расстояние до центра сцены, на который смотрит камера
    /// </summary>
    public float Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    /// <summary>
    /// Регистрация свойства в Avalonia API и установка значения по умолчанию
    /// </summary>
    public static readonly StyledProperty<List<Vector3>> PointsProperty =
        AvaloniaProperty.Register<Surface, List<Vector3>>(nameof(PointsProperty), new List<Vector3>());

    /// <summary>
    /// Точки поверхности
    /// </summary>
    public List<Vector3> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>
    /// Конструктор
    /// </summary>
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

    /// <summary>
    /// Региструет действие, которое нужно совершить при обновлении свойства,
    /// передает данные к низкоуровнему обработчику
    /// </summary>
    private void RegisterPropertyChangedCallbacks()
    {
        // Регистрируем обработчики изменений для всех свойств
        this.GetObservable(SurfaceColorProperty).Subscribe(_ => _surfaceView.SurfaceColor = ColorToVector3(SurfaceColor));
        this.GetObservable(AxesColorProperty).Subscribe(_ => _surfaceView.AxesColor = ColorToVector3(AxesColor));
        this.GetObservable(AxesCaptionColorProperty).Subscribe(_ => _surfaceView.AxesCaptionColor = AxesCaptionColor);
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

    /// <summary>
    /// Преобразует Avalonia.Media.Color к OpenGl формату
    /// </summary>
    /// <param name="c">Цвет</param>
    /// <returns>Цвет в OpenGl формате</returns>
    Vector3 ColorToVector3(Color c) => new Vector3(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);
}
