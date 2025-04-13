using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using Avalonia.Media;
using static Avalonia.OpenGL.GlConsts;
using System.Numerics;
using System.Runtime.InteropServices;
using System;

namespace Maga_Avalonia3D;
internal class GlColor
{
    private const float normalizeValue = 255f;

    private float r = 0;
    private float g = 0;
    private float b = 0;
    private float a = 0;
    public GlColor() { }
    public GlColor(float pr, float pg, float pb, float pa)
    {
        r = pr;
        g = pg;
        b = pb;
        a = pa;
    }
    public GlColor(Color c)
    {
        r = c.R;
        g = c.G;
        b = c.B;
        a = c.A;
        Normalize(normalizeValue);
    }
    private void Normalize(float n)
    {
        r = r / n;
        g = g / n;
        b = b / n;
        a = a / n;
    }

    public static implicit operator GlColor(Color color) => new GlColor(color);
    public static implicit operator Color(GlColor c) => new Color((byte)c.AN, (byte)c.RN, (byte)c.GN, (byte)c.BN);
    public static implicit operator string(GlColor c) => new string($"GlColor({c.r}, {c.g}, {c.b}, {c.a}) -> RGBA");

    public float R { set => r = value; get => r; }
    public float G { set => g = value; get => g; }
    public float B { set => b = value; get => b; }
    public float A { set => a = value; get => a; }
    public float RN { get => r * normalizeValue; }
    public float GN { get => g * normalizeValue; }
    public float BN { get => b * normalizeValue; }
    public float AN { get => a * normalizeValue; }
}

internal class OpenGlControl : OpenGlControlBase
{
    struct GlPoint
    {
        public float x;
        public float y;
        public float z;
        public float r;
        public float g;
        public float b;
        public GlPoint(Vector3 p, GlColor c)
        {
            x = p.X;
            y = p.Y;
            z = p.Z;
            r = c.R;
            g = c.G;
            b = c.B;
        }
        public GlPoint(float x, float y, float z, float r, float g, float b)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }

    //OpenGL fields
    int _vbo; // vertex buffer object
    int _vao; // vertex array object
    int _shaderProram;
    int _fragmentShader;
    int _vertexShader;

    //Control fields
    private GlColor _background = new GlColor();
    public GlColor Background
    {
        set => _background = value;
        get => _background;
    }
    public void SetBackground(Avalonia.Media.Color color) => _background = color;
    public void SetBackground(GlColor color) => _background = color;

    //Base GL Functions
    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        ConfigureShaders(gl);
        CreateVertexBuffer(gl);
        GlCheckError(gl, "Init");
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
        gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        gl.DeleteBuffer(_vbo);
        gl.DeleteVertexArray(_vao);
        gl.DeleteProgram(_shaderProram);
        gl.DeleteShader(_fragmentShader);
        gl.DeleteShader(_vertexShader);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        int width = (int)Bounds.Width;
        int height = (int)Bounds.Height;

        gl.ClearColor(_background.R, _background.G, _background.B, _background.A);
        gl.Clear(GL_COLOR_BUFFER_BIT);

        gl.UseProgram(_shaderProram);
        gl.Viewport(0, 0, width, height);

        gl.DrawArrays(GL_TRIANGLES, 0, (nint)3);

        GlCheckError(gl, "OnOpenGlRender");
    }

    //User Draw Functions
    protected void CreateVertexBuffer(GlInterface gl)
    {
        GlPoint[] vertices = new GlPoint[3]
        {
            new GlPoint(-0.5f, -0.5f, 0.0f, 1.0f, 0.0f, 0.0f),
            new GlPoint( 0.5f, -0.5f, 0.0f, 0.0f, 1.0f, 0.0f),
            new GlPoint( 0.0f,  0.5f, 0.0f, 0.0f, 0.0f, 1.0f)
        };

        int glPointBitSize = Marshal.SizeOf<GlPoint>();
        int verticesBitSize = glPointBitSize * vertices.Length;
        _vbo = gl.GenBuffer();
        gl.BindBuffer(GL_ARRAY_BUFFER, _vbo);

        unsafe
        {
            fixed (void* pVertices = vertices)
            {
                gl.BufferData(GL_ARRAY_BUFFER, verticesBitSize, (nint)pVertices, GL_STATIC_DRAW);
            }
        }

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, glPointBitSize, nint.Zero);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, glPointBitSize, 3 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        GlCheckError(gl, "CreateVertexBuffer");
    }

    void ConfigureShaders(GlInterface gl)
    {
        var v = gl.GetString(GL_VERSION);

        _shaderProram = gl.CreateProgram();
        
        _vertexShader = gl.CreateShader(GL_VERTEX_SHADER);
        GlCheckError(gl, "Create vertex shader");
        var res = gl.CompileShaderAndGetError(_vertexShader, VertexShaderSource);
        if (res != null) throw new Exception("Vertex shader compile error: " + res);
        GlCheckError(gl, "Compile vertex shader");
        gl.AttachShader(_shaderProram, _vertexShader);

        _fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
        GlCheckError(gl, "Create fragment shader");
        res = gl.CompileShaderAndGetError(_fragmentShader, FragmentShaderSource);
        if (res != null) throw new Exception("Fragment shader compile error: " + res);
        GlCheckError(gl, "Compile fragment shader");
        gl.AttachShader(_shaderProram, _fragmentShader);
        GlCheckError(gl, "Attach fragment shader");

        gl.LinkProgram(_shaderProram);

        GlCheckError(gl, "ConfigureShaders");
    }

    void GlCheckError(GlInterface gl, string what = "no info")
    {
        int error = gl.GetError();
        if (error != GL_NO_ERROR) throw new Exception("GL task failed: " + what + $", ErrorCode {error}");
    }

    string GlVersionSource
    {
        get
        {
            //GlVersion glVersion = this.GlVersion;
            //int version = (glVersion.Type == GlProfileType.OpenGL ?
            //RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120 :
            //100);
            //return "#version " + version + "\n";
            return "#version 300 es";
        }
    }
    string VertexShaderSource => GlVersionSource + @" 
    precision mediump float;
    layout (location = 0) in vec3 aPos;
    layout (location = 1) in vec3 aColor;
    out vec3 ourColor;

    void main()
    {
        gl_Position = vec4(aPos, 1.0);
        ourColor = aColor;
    }";
    string FragmentShaderSource => GlVersionSource + @"
    precision mediump float;
    in vec3 ourColor;    
    out vec4 FragColor;
    
    void main()
    {
        FragColor = vec4(ourColor, 1.0);
    }";
}
