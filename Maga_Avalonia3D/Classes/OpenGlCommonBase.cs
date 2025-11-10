using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Avalonia.OpenGL.GlConsts;

namespace Maga_Avalonia3D.Classes
{
    public abstract class OpenGlCommonBase : OpenGlControlBase
    {
        // OpenGL resources
        protected int _shaderProgram;
        protected int _model;
        protected int _view;
        protected int _projection;
        protected string _glShaderVersion = "";

        protected virtual string VertexShaderResource => "Maga_Avalonia3D.Shaders.basic.vert";
        protected virtual string FragmentShaderResource => "Maga_Avalonia3D.Shaders.basic.frag";

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);

            while (gl.GetError() != GL_NO_ERROR) { }
            GlCheckError(gl, "Wait for context init");

            string versionString = gl.GetString(GL_VERSION).ToString();
            _glShaderVersion = DetermineShaderVersion(versionString, gl);

            ConfigureShaders(gl);
            CreateGeometry(gl);

            gl.Enable(GL_DEPTH_TEST);
            GlCheckError(gl, "Init");
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            int width = (int)Bounds.Width;
            int height = (int)Bounds.Height;

            gl.Viewport(0, 0, width, height);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            gl.UseProgram(_shaderProgram);
            UpdateUniforms(gl, width, height);
            DrawGeometry(gl);

            GlCheckError(gl, "OnOpenGlRender");
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            base.OnOpenGlDeinit(gl);
            CleanupGeometry(gl);
            CleanupShaders(gl);
        }

        protected abstract void CreateGeometry(GlInterface gl);
        protected abstract void DrawGeometry(GlInterface gl);
        protected virtual void UpdateUniforms(GlInterface gl, int width, int height) { }
        protected virtual void CleanupGeometry(GlInterface gl) { }

        private void ConfigureShaders(GlInterface gl)
        {
            _vertexShader = gl.CreateShader(GL_VERTEX_SHADER);
            GlCheckError(gl, "Create vertex shader");

            var vertexSource = LoadShaderFromResource(VertexShaderResource);
            var res = gl.CompileShaderAndGetError(_vertexShader, vertexSource);
            if (res != null) throw new Exception("Vertex shader compile error: " + res);
            GlCheckError(gl, "Compile vertex shader");

            _fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
            GlCheckError(gl, "Create fragment shader");

            var fragmentSource = LoadShaderFromResource(FragmentShaderResource);
            res = gl.CompileShaderAndGetError(_fragmentShader, fragmentSource);
            if (res != null) throw new Exception("Fragment shader compile error: " + res);
            GlCheckError(gl, "Compile fragment shader");

            _shaderProgram = gl.CreateProgram();
            GlCheckError(gl, "Create shader program");

            gl.AttachShader(_shaderProgram, _vertexShader);
            GlCheckError(gl, "Attach vertex shader");

            gl.AttachShader(_shaderProgram, _fragmentShader);
            GlCheckError(gl, "Attach fragment shader");

            gl.LinkProgram(_shaderProgram);
            GlCheckError(gl, "Link shader program");

            _model = gl.GetUniformLocationString(_shaderProgram, "model");
            _view = gl.GetUniformLocationString(_shaderProgram, "view");
            _projection = gl.GetUniformLocationString(_shaderProgram, "projection");
            GlCheckError(gl, "ConfigureShaders");
        }

        private void CleanupShaders(GlInterface gl)
        {
            gl.UseProgram(0);
            gl.DeleteProgram(_shaderProgram);
            gl.DeleteShader(_fragmentShader);
            gl.DeleteShader(_vertexShader);
        }

        private string DetermineShaderVersion(string versionString, GlInterface gl)
        {
            bool isOpenGLES = versionString.Contains("OpenGL ES");
            int major = 3;
            int minor = 3;

            var match = System.Text.RegularExpressions.Regex.Match(versionString, @"(\d+)(?:\.(\d+))?");
            if (match.Success)
            {
                major = int.Parse(match.Groups[1].Value);
                minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            }

            return isOpenGLES
                ? $"#version {major}{minor}0 es"
                : $"#version {major}{minor}0";
        }

        protected string LoadShaderFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new ArgumentException($"Embedded resource not found: {resourceName}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected void SetUniformMatrix4(GlInterface gl, int location, in System.Numerics.Matrix4x4 matrix)
        {
            unsafe
            {
                fixed (void* ptr = &matrix)
                {
                    gl.UniformMatrix4fv(location, 1, false, ptr);
                }
            }
        }

        private int _vertexShader;
        private int _fragmentShader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void GlCheckError(GlInterface gl, string what = "no info", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            int error = gl.GetError();
            if (error != GL_NO_ERROR)
            {
                var translation = TranslateGlError(error);
                var message = $"GL task \"{what}\" failed with error {error} \"{translation}\" at line {lineNumber} called by {caller}";
                Console.WriteLine(message);
                throw new Exception(message);
            }
        }

        private static string TranslateGlError(int code)
        {
            return code switch
            {
                GL_NO_ERROR => "GL_NO_ERROR",
                GL_INVALID_ENUM => "GL_INVALID_ENUM",
                GL_INVALID_VALUE => "GL_INVALID_VALUE",
                GL_INVALID_OPERATION => "GL_INVALID_OPERATION",
                GL_STACK_OVERFLOW => "GL_STACK_OVERFLOW",
                GL_STACK_UNDERFLOW => "GL_STACK_UNDERFLOW",
                GL_OUT_OF_MEMORY => "GL_OUT_OF_MEMORY",
                GL_INVALID_FRAMEBUFFER_OPERATION => "GL_INVALID_FRAMEBUFFER_OPERATION",
                _ => "Unknown error"
            };
        }
    }
}