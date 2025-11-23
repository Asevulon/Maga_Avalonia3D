using Avalonia.Controls;
using Maga_Avalonia3D.Classes;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maga_Avalonia3D;

public partial class Surface : UserControl
{
    private SurfaceView _surfaceView;

    public Surface()
    {
        InitializeComponent();

        // Создаем SurfaceView вместо SurfaceRenderer
        _surfaceView = new SurfaceView();
        _surfaceView.Width = canvas.Width;
        _surfaceView.Height = canvas.Height;

        // Настройка параметров через SurfaceView (он проксирует к SurfaceRenderer)
        _surfaceView.ClearColor = new Vector3(0.1f, 0.1f, 0.1f);
        _surfaceView.LightPosition = new Vector3(0f, 0f, 3.0f);
        _surfaceView.LightColor = new Vector3(1.0f, 1.0f, 1.0f);
        _surfaceView.SurfaceColor = new Vector3(0.3f, 0.6f, 1.0f); // Голубой цвет для купола

        // Генерируем точки для гауссова купола
        var gaussianPoints = GenerateGaussianDomePoints(200);

        // Устанавливаем точки поверхности через SurfaceView
        _surfaceView.SetSurfacePoints(gaussianPoints);

        // Добавляем SurfaceView в UI вместо SurfaceRenderer
        canvas.Children.Add(_surfaceView);
    }

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