using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using System;
using static Avalonia.OpenGL.GlConsts;
namespace Maga_Avalonia3D;

public partial class Surface : UserControl
{
    public Surface()
    {
        InitializeComponent();

        SurfaceOpenGl surfaceOpenGl = new SurfaceOpenGl();
        surfaceOpenGl.Width=canvas.Width;
        surfaceOpenGl.Height=canvas.Height;

        TextBlock textBlock = new TextBlock();
        textBlock.Text = "Это нарисовано средствами OpenGL";
        textBlock.Foreground = new SolidColorBrush(Colors.White);
        canvas.Children.Add(surfaceOpenGl);
        canvas.Children.Add(textBlock);
    }

    class SurfaceOpenGl : OpenGlControlBase
    {
        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            base.OnOpenGlDeinit(gl);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            gl.ClearColor(0.5f, 0.5f, 0, 1);
            gl.Clear(GL_COLOR_BUFFER_BIT);

            gl.Viewport(0, 0, 10, 10);
        }
    }

}