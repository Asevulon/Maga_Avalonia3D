using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Avalonia.OpenGL.GlConsts;

namespace SurfaceLib
{
    /// <summary>
    /// Класс родитель, который определяет общие для OpenGl-отрисовки интерфейсы
    /// </summary>
    public abstract class OpenGlCommonBase : OpenGlControlBase
    {
        // Специализированные индентификаторы для OpenGl сущностей
        protected int _shaderProgram;
        protected int _model;
        protected int _view;
        protected int _projection;
        protected string _glShaderVersion = "";

        // Шейдеры распологаются в ресурсах библиотеки
        /// <summary>
        /// Имя ресурса вершинного шейдера
        /// </summary>
        protected virtual string VertexShaderResource => "SurfaceClassLib.Shaders.basic.vert";
        /// <summary>
        /// Имя ресурса фрагментного шейдера
        /// </summary>
        protected virtual string FragmentShaderResource => "SurfaceClassLib.Shaders.basic.frag";

        /// <summary>
        /// Функция для инициализации OpenGl контекста
        /// </summary>
        /// <param name="gl"></param>
        protected override void OnOpenGlInit(GlInterface gl)
        {
            // Стандартная инициализация
            base.OnOpenGlInit(gl);

            // Ожидаем завершения инициализации
            while (gl.GetError() != GL_NO_ERROR) { }
            GlCheckError(gl, "Wait for context init");

            // Получаем версию OpenGl
            string versionString = gl.GetString(GL_VERSION).ToString();
            _glShaderVersion = DetermineShaderVersion(versionString, gl);

            // Подготавливаем шейдеры
            ConfigureShaders(gl);
            
            //
            CreateGeometry(gl);

            gl.Enable(GL_DEPTH_TEST);
            GlCheckError(gl, "Init");
        }

        /// <summary>
        /// Вызывается при запросе отрисовке
        /// </summary>
        /// <param name="gl"></param>
        /// <param name="fb"></param>
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

        /// <summary>
        /// Подготовка шейдеров
        /// </summary>
        /// <param name="gl"></param>
        /// <exception cref="Exception">Ошибка на уровне OpenGl</exception>
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

        /// <summary>
        /// Очистка шейдеров
        /// </summary>
        /// <param name="gl"></param>
        private void CleanupShaders(GlInterface gl)
        {
            gl.UseProgram(0);
            gl.DeleteProgram(_shaderProgram);
            gl.DeleteShader(_fragmentShader);
            gl.DeleteShader(_vertexShader);
        }

        /// <summary>
        /// Определяет под какую версию компилировать шейдер
        /// </summary>
        /// <param name="versionString">Версия OpenGl</param>
        /// <param name="gl"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Загружает текст шейдера из ресурсов
        /// </summary>
        /// <param name="resourceName">Имя ресурса</param>
        /// <returns>Тест шейдера</returns>
        /// <exception cref="ArgumentException">
        /// Возникает если <paramref name="resourceName"/> не найден в ресурсах
        /// </exception>
        protected string LoadShaderFromResource(string resourceName)
        {
            var assembly = typeof(OpenGlCommonBase).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new ArgumentException($"Embedded resource not found: {resourceName}, existing resources: {string.Join(' ', assembly.GetManifestResourceNames())}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Передает матрицу в контекст OpenGl
        /// </summary>
        /// <param name="gl"></param>
        /// <param name="location">Идентификатор матрицы, например, _model</param>
        /// <param name="matrix">Матрица</param>
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

        /// <summary>
        /// Наглядное представление ошибки, которая возникла на уровне OpenGl
        /// </summary>
        /// <param name="gl"></param>
        /// <param name="what">Действие, после которого вызываем проверку</param>
        /// <param name="lineNumber">Место вызова</param>
        /// <param name="caller">Кто вызвал</param>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Переводит код ошибок OpenGl в наглядный вид
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <returns></returns>
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