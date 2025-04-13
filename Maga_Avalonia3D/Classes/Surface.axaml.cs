using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
namespace Maga_Avalonia3D;


public partial class Surface : UserControl
{
    public Surface()
    {
        InitializeComponent();

        SurfaceOpenGl surfaceOpenGl = new SurfaceOpenGl();
        surfaceOpenGl.Width=canvas.Width;
        surfaceOpenGl.Height=canvas.Height;
        surfaceOpenGl.Background = Colors.DimGray;

        TextBlock textBlock = new TextBlock();
        textBlock.Text = surfaceOpenGl.Background;
        textBlock.Foreground = new SolidColorBrush(Colors.White);
        canvas.Children.Add(surfaceOpenGl);
        canvas.Children.Add(textBlock);
    }

    class SurfaceOpenGl : OpenGlControl
    {

    }
}