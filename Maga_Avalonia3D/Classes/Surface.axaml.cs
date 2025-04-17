using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using System;
namespace Maga_Avalonia3D;


public partial class Surface : UserControl
{
    SurfaceOpenGl surfaceOpenGl;
    Slider rotationSlider;

    public Surface()
    {
        InitializeComponent();

        surfaceOpenGl = new SurfaceOpenGl();
        surfaceOpenGl.Width = canvas.Width;
        surfaceOpenGl.Height = canvas.Height;
        surfaceOpenGl.Background = Colors.DimGray;

        TextBlock textBlock = new TextBlock();
        textBlock.Text = surfaceOpenGl.Background.ToString();
        textBlock.Foreground = new SolidColorBrush(Colors.White);

        rotationSlider = new Slider();
        rotationSlider.Minimum = 0;
        rotationSlider.Maximum = 2 * Math.PI;
        rotationSlider.Width = 250;

        this.Bind(
           RotationProperty,
           new Binding(nameof(SurfaceOpenGl.Rotation))
           {
               Source = surfaceOpenGl,
               Mode = BindingMode.TwoWay
           }
       );

        rotationSlider.Bind(
            Slider.ValueProperty,
            new Binding(nameof(Rotation))
            {
                Source = this,
                Mode = BindingMode.TwoWay
            }
        );

        canvas.Children.Add(surfaceOpenGl);
        canvas.Children.Add(textBlock);
        canvas.Children.Add(rotationSlider);
    }

    class SurfaceOpenGl : OpenGlControl { }

    // Регистрация StyledProperty для Rotation
    public static readonly StyledProperty<double> RotationProperty =
        AvaloniaProperty.Register<Surface, double>(
            nameof(Rotation),
            defaultBindingMode: BindingMode.TwoWay // Для двусторонней привязки
        );

    public double Rotation
    {
        get => GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }
}