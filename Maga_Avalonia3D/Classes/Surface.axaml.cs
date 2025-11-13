using Avalonia;
using Avalonia.Controls;
using Maga_Avalonia3D.Classes;
using System.Collections.Generic;
using System.Numerics;

namespace Maga_Avalonia3D;

public partial class Surface : UserControl
{
    private SceneRenderer _sceneRenderer;

    public Surface()
    {
        InitializeComponent();

        _sceneRenderer = new SceneRenderer();
        _sceneRenderer.Width = canvas.Width;
        _sceneRenderer.Height = canvas.Height;
        _sceneRenderer.ClearColor = new Vector3(0.1f, 0.1f, 0.1f);
        _sceneRenderer.LightPosition = new Vector3(3.0f, 3.0f, 3.0f);
        _sceneRenderer.LightColor = new Vector3(1.0f, 1.0f, 1.0f);

        var primitives = new List<PrimitiveInstance>
        {
            // Пол
            new() { Type = PrimitiveType.Plane, Position = new(0, -0.5f, 0), Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0.3f, 0.3f, 0.3f) },

            // Объекты
            new() { Type = PrimitiveType.Cube,    Position = new(-2, 0, 0), Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(1, 0, 0) },
            new() { Type = PrimitiveType.Sphere,  Position = new(0, 0, 0),  Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0, 1, 0) },
            new() { Type = PrimitiveType.Pyramid, Position = new(2, 0, 0),  Rotation = new(0, 0, 0), Scale = new(1, 1, 1), Color = new(0, 0, 1) }
        };

        _sceneRenderer.SetScene(primitives);
        canvas.Children.Add(_sceneRenderer);
    }
}