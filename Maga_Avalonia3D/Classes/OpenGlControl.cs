using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using Avalonia.Media;
using static Avalonia.OpenGL.GlConsts;
using System.Numerics;
using System.Runtime.InteropServices;
using System;
using System.ComponentModel;
using Avalonia.OpenGL.Egl;
using System.Runtime.CompilerServices;

namespace Maga_Avalonia3D;
internal unsafe class GlColor
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

internal static class MyGlConsts
{
    public const int GL_UNSIGNED_INT = 0x1405;
    public const int GL_CONTEXT_PROFILE_MASK = 0x9126;
    public const int GL_INVALID_ENUM = 1280;
    public const int GL_INVALID_VALUE = 1281;
    public const int GL_INVALID_OPERATION = 1282;
    public const int GL_STACK_OVERFLOW = 1283;
    public const int GL_STACK_UNDERFLOW = 1284;
    public const int GL_OUT_OF_MEMORY = 1285;
    public const int GL_INVALID_FRAMEBUFFER_OPERATION = 1286;
}
internal class OpenGlControl : OpenGlControlBase, INotifyPropertyChanged
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
    int _ebo; // element buffer object
    int _shaderProgram;
    int _fragmentShader;
    int _vertexShader;
    int _model;
    int _view;
    int _projection;
    string _glShaderVersion = "";

    float _rotation = 0f;

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

        string versionString = gl.GetString(GL_VERSION).ToString();
        _glShaderVersion = DetermineShaderVersion(versionString, gl);

        ConfigureShaders(gl);
        CreateVertexBuffer(gl);

        gl.Enable(GL_DEPTH_TEST);

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
        gl.DeleteProgram(_shaderProgram);
        gl.DeleteShader(_fragmentShader);
        gl.DeleteShader(_vertexShader);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        int width = (int)Bounds.Width;
        int height = (int)Bounds.Height;

        // Рассчитываем соотношение сторон
        float aspectRatio = (float)width / height;
        float projWidth = 6.0f;
        float projHeight = projWidth / aspectRatio; // Корректируем высоту относительно ширины

        gl.ClearColor(_background.R, _background.G, _background.B, _background.A);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        gl.UseProgram(_shaderProgram);
        gl.Viewport(0, 0, width, height);

        float radius = 5;
        float camX = radius * MathF.Cos(_rotation);
        float camY = 1f;
        float camZ = radius * MathF.Sin(_rotation);

        Vector3 cameraPos = new Vector3(camX, camY, camZ);
        Vector3 cameraTarget = new Vector3(0, 0, 0);
        Vector3 cameraUpVector = Vector3.UnitY;

        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4x4 projection = Matrix4x4.CreateOrthographic(
            projWidth,
            projHeight,
            0.1f,
            10.0f
        );
        Matrix4x4 view = Matrix4x4.CreateLookAt(
            cameraPos,
            cameraTarget,
            cameraUpVector
        );

        unsafe
        {
            gl.UniformMatrix4fv(_model, 1, false, &model);
            gl.UniformMatrix4fv(_view, 1, false, &view);
            gl.UniformMatrix4fv(_projection, 1, false, &projection);
        }

        gl.BindVertexArray(_vao);
        gl.DrawElements(GL_TRIANGLES, 36, MyGlConsts.GL_UNSIGNED_INT, 0);

        GlCheckError(gl, "OnOpenGlRender");
    }

    protected void CreateVertexBuffer(GlInterface gl)
    {
        //Создали объект массива вершин
        //Специальный объект, который хранит состояние всех связанных с ним VBO и EBO,
        //а также настройки атрибутов вершин (как интерпретировать данные в буферах).
        //В конце надо привязать аттрибуты
        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);
        GlCheckError(gl, "Create VAO 1");

        //Создали буффер вершин
        //Это специальный буфер в видеопамяти, куда загружаются данные о вершинах — в твоём случае это массив vertices,
        //содержащий координаты точек и цвета.

        // Вершины куба (8 точек)
        GlPoint[] vertices = new GlPoint[8]
        {
            new GlPoint(-1.0f, -1.0f, -1.0f, 1.0f, 0.0f, 0.0f), // 0
            new GlPoint( 1.0f, -1.0f, -1.0f, 0.0f, 1.0f, 0.0f), // 1
            new GlPoint( 1.0f,  1.0f, -1.0f, 0.0f, 0.0f, 1.0f), // 2
            new GlPoint(-1.0f,  1.0f, -1.0f, 1.0f, 1.0f, 0.0f), // 3
            new GlPoint(-1.0f, -1.0f,  1.0f, 1.0f, 0.0f, 1.0f), // 4
            new GlPoint( 1.0f, -1.0f,  1.0f, 0.0f, 1.0f, 1.0f), // 5
            new GlPoint( 1.0f,  1.0f,  1.0f, 0.5f, 0.5f, 0.5f), // 6
            new GlPoint(-1.0f,  1.0f,  1.0f, 1.0f, 0.5f, 0.2f)  // 7
        };

        uint[] indices =
        {
            // Передняя грань
            0, 1, 2,  2, 3, 0,
            // Задняя грань
            4, 5, 6,  6, 7, 4,
            // Верхняя грань
            3, 2, 6,  6, 7, 3,
            // Нижняя грань
            0, 1, 5,  5, 4, 0,
            // Левая грань
            0, 3, 7,  7, 4, 0,
            // Правая грань
            1, 2, 6,  6, 5, 1
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
        GlCheckError(gl, "Create VBO");

        //Создали буффер индексов(элементов)
        //Буфер, который хранит индексы вершин из VBO, определяющие,
        //как вершины соединяются в примитивы (треугольники).

        int indicesBitSize = sizeof(uint) * indices.Length;
        _ebo = gl.GenBuffer();
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);
        unsafe
        {
            fixed (void* pIndices = indices)
            {
                gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, indicesBitSize, (nint)pIndices, GL_STATIC_DRAW);
            }
        }
        GlCheckError(gl, "Create EBO");

        //Привяжем аттрибуты к VAO

        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, glPointBitSize, nint.Zero);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, glPointBitSize, 3 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        GlCheckError(gl, "Create VAO 2");
    }

    void ConfigureShaders(GlInterface gl)
    {
        var v = gl.GetString(GL_VERSION);
        Console.WriteLine(v);

        _shaderProgram = gl.CreateProgram();
        
        _vertexShader = gl.CreateShader(GL_VERTEX_SHADER);
        GlCheckError(gl, "Create vertex shader");
        var res = gl.CompileShaderAndGetError(_vertexShader, VertexShaderSource);
        if (res != null) throw new Exception("Vertex shader compile error: " + res);
        GlCheckError(gl, "Compile vertex shader");
        gl.AttachShader(_shaderProgram, _vertexShader);

        _fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
        GlCheckError(gl, "Create fragment shader");
        res = gl.CompileShaderAndGetError(_fragmentShader, FragmentShaderSource);
        if (res != null) throw new Exception("Fragment shader compile error: " + res);
        GlCheckError(gl, "Compile fragment shader");
        gl.AttachShader(_shaderProgram, _fragmentShader);
        GlCheckError(gl, "Attach fragment shader");

        gl.LinkProgram(_shaderProgram);

        _model = gl.GetUniformLocationString(_shaderProgram, "model");
        _view = gl.GetUniformLocationString(_shaderProgram, "view");
        _projection = gl.GetUniformLocationString(_shaderProgram, "projection");

        GlCheckError(gl, "ConfigureShaders");
    }

    void GlCheckError(GlInterface gl, string what = "no info", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
    {
        int error = gl.GetError();
        if (error != GL_NO_ERROR)
        {
            var translation  = TranslateGlError(error);
            var line = string.Format("GL task \"" + what + $"\" failed with error {error} \"{translation}\" at line {lineNumber} called by {caller}\n");
            Console.WriteLine(line);
            System.Diagnostics.Debugger.Break();
            throw new Exception(line);
        }
    }
    private string TranslateGlError(int code)
    {
        string line = null;
        switch(code)
        {
            case GL_NO_ERROR:
                line = "GL_NO_ERROR";
                break;
            case GL_INVALID_ENUM:
                line = "GL_INVALID_ENUM";
                break;
            case GL_INVALID_VALUE:
                line = "GL_INVALID_VALUE";
                break;
            case GL_INVALID_OPERATION:
                line = "GL_INVALID_OPERATION";
                break;
            case GL_STACK_OVERFLOW:
                line = "GL_STACK_OVERFLOW";
                break;
            case GL_STACK_UNDERFLOW:
                line = "GL_STACK_UNDERFLOW";
                break;
            case GL_OUT_OF_MEMORY:
                line = "GL_OUT_OF_MEMORY";
                break;
            case GL_INVALID_FRAMEBUFFER_OPERATION:
                line = "GL_INVALID_FRAMEBUFFER_OPERATION";
                break;
        }
        return line;
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
    string VertexShaderSource => _glShaderVersion + @" 
    precision mediump float;
    layout (location = 0) in vec3 aPos;
    layout (location = 1) in vec3 aColor;
    out vec3 ourColor;
    uniform mat4 model;
    uniform mat4 view;
    uniform mat4 projection;
    void main()
    {
        gl_Position = projection * view * model * vec4(aPos, 1.0);
        ourColor = aColor;
    }";
    string FragmentShaderSource => _glShaderVersion + @"
    precision mediump float;
    in vec3 ourColor;    
    out vec4 FragColor;
    
    void main()
    {
        FragColor = vec4(ourColor, 1.0);
    }";

    private string DetermineShaderVersion(string versionString, GlInterface gl)
    {
        bool isOpenGLES = versionString.Contains("OpenGL ES");
        int major = 3;
        int minor = 3;

        // Парсинг основной и минорной версии
        var match = System.Text.RegularExpressions.Regex.Match(versionString, @"(\d+)(?:\.(\d+))?");
        if (match.Success)
        {
            major = int.Parse(match.Groups[1].Value);
            minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        }

        // Обработка для macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !isOpenGLES)
        {
            // Проверяем Core Profile
            var profile = gl.GetString(GlConsts.GL_CONTEXT_PROFILE_MASK).ToString();
            bool isCoreProfile = profile.Contains("CORE");

            if (major < 3 || (major == 3 && minor < 2))
                return "#version 150 core"; // Fallback для старых версий
            else
                return $"#version {major}{minor}0{(isCoreProfile ? " core" : "")}";
        }

        return isOpenGLES
            ? $"#version {major}{minor}0 es"
            : $"#version {major}{minor}0";
    }

    public double Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation != value)
            {
                _rotation = (float)value;
                OnPropertyChanged(nameof(Rotation));
                RequestNextFrameRendering();
            }
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
