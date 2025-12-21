using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maga_Avalonia3D.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        List<Vector3> points = new List<Vector3>();

        int num = 20;
        float A1 = 1.0f;
        float A2 = 1.1f;
        float k1 = 0.5f; 
        float k2 = 1.0f;
        float k3 = 0.4f;
        float k4 = 1.1f;

        for (int i = 0; i < num; i++)
        {
            for (int j = 0; j < num; j++)
            {
                float w1 = A1 * MathF.Cos(k1 * i + k2 * j);
                float w2 = A2 * MathF.Cos(k3 * i + k4 * j);
                points.Add(new Vector3(i, j, w1 - w2));
            }
        }

        surface.SurfacePoints = points;
    }
}
