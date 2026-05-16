using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace SurfaceLib
{
    // Добавляем недостающие константы OpenGL ES
    internal static class GlExtensions
    {
        public const int GL_UNSIGNED_INT = 0x1405;
        public const int GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const int GL_LINES = 0x0001;
    }

    public class SurfaceRenderer : OpenGlCommonBase
    {
        // Входные данные поверхности
        private List<Vector3> _surfacePoints = new();

        // Сгенерированная геометрия
        private List<Vector3> _vertices = new();
        private List<Vector3> _normals = new();
        private List<uint> _indices = new();

        // OpenGL ресурсы
        private int _vao;
        private int _vboVertices;
        private int _vboNormals;
        private int _vboIndices;

        // Матрицы
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;

        // Параметры освещения
        private Vector3 _clearColor = new(0.1f, 0.1f, 0.1f);
        private Vector3 _lightPosition = new(3.0f, 3.0f, 3.0f);
        private Vector3 _lightColor = new(1.0f, 1.0f, 1.0f);
        private Vector3 _surfaceColor = new(0.7f, 0.7f, 1.0f);

        // Uniform locations для освещения
        private int _lightPosR = -1, _lightPosG = -1, _lightPosB = -1;
        private int _lightColorR = -1, _lightColorG = -1, _lightColorB = -1;
        private int _objectColorR = -1, _objectColorG = -1, _objectColorB = -1;

        // Размеры для проекции
        private int _width;
        private int _height;

        // Флаг инициализации
        private bool _isInitialized;

        // Флаг невалидных данных
        private bool _geometryDirty = false;

        // Поддержка кастомной матрицы вида
        private bool _useCustomViewMatrix;
        private Matrix4x4 _customViewMatrix;

        // Параметры осей
        public bool ShowAxes { get; set; } = true;
        public Vector3 AxesColor { get; set; } = new Vector3(0.8f, 0.8f, 0.8f); // Светло-серый по умолчанию

        // OpenGL ресурсы для осей
        private int _axesVao;
        private int _axesVboVertices;
        private int _axesShaderProgram;
        private int _axesVertexShader;
        private int _axesFragmentShader;

        // Uniform locations для осей
        private int _axesModelLoc;
        private int _axesViewLoc;
        private int _axesProjectionLoc;
        private int _axisColorR = -1;
        private int _axisColorG = -1;
        private int _axisColorB = -1;

        // Флаг для отслеживания необходимости пересоздания геометрии осей
        private bool _axesGeometryDirty = false;

        private (float minX, float maxX, float minY, float maxY, float minZ, float maxZ) _AxisBounds;
        private (Vector3 world, Vector2 screen)[] _axisCaptions;

        public (Vector3 world, Vector2 screen)[] AxisCaptions
        {
            get => _axisCaptions;
        }

        public bool IsInitialized => _isInitialized;
        public Vector3 ClearColor
        {
            get => _clearColor;
            set => _clearColor = value;
        }

        public Vector3 LightPosition
        {
            get => _lightPosition;
            set => _lightPosition = value;
        }

        public Vector3 LightColor
        {
            get => _lightColor;
            set => _lightColor = value;
        }

        public Vector3 SurfaceColor
        {
            get => _surfaceColor;
            set => _surfaceColor = value;
        }

        public bool UseCustomViewMatrix
        {
            get => _useCustomViewMatrix;
            set => _useCustomViewMatrix = value;
        }

        public Matrix4x4 CustomViewMatrix
        {
            get => _customViewMatrix;
            set => _customViewMatrix = value;
        }

        protected override string VertexShaderResource => "SurfaceClassLib.Shaders.SurfaceRenderer.vert";
        protected override string FragmentShaderResource => "SurfaceClassLib.Shaders.SurfaceRenderer.frag";

        protected virtual string AxesVertexShaderResource => "SurfaceClassLib.Shaders.Axes.vert";
        protected virtual string AxesFragmentShaderResource => "SurfaceClassLib.Shaders.Axes.frag";

        // Вспомогательные функции для расчета границ
        private float GetLowerBound(float x)
        {
            if (float.IsNaN(x) || float.IsInfinity(x))
                return 0;

            if (Math.Abs(x) < 0.0001f)
                return 0;

            int order = (int)Math.Floor(Math.Log10(Math.Abs(x)));

            // Пробуем порядки от текущего вниз
            for (int currentOrder = order; currentOrder >= order - 10; currentOrder--)
            {
                foreach (float k in new float[] { 5, 2, 1 })
                {
                    float step = k * (float)Math.Pow(10, currentOrder);
                    float bound = (float)Math.Floor(x / step) * step;
                    if (bound <= x)
                    {
                        return bound;
                    }
                }
            }

            // Резервный вариант
            return (float)Math.Floor(x);
        }

        private float GetUpperBound(float x)
        {
            if (float.IsNaN(x) || float.IsInfinity(x))
                return 0;

            if (Math.Abs(x) < 0.0001f)
                return 0;

            int order = (int)Math.Floor(Math.Log10(Math.Abs(x)));

            // Пробуем порядки от текущего вниз
            for (int currentOrder = order; currentOrder >= order - 10; currentOrder--)
            {
                foreach (float k in new float[] { 5, 2, 1 })
                {
                    float step = k * (float)Math.Pow(10, currentOrder);
                    float bound = (float)Math.Ceiling(x / step) * step;
                    if (bound >= x)
                    {
                        return bound;
                    }
                }
            }

            // Резервный вариант
            return (float)Math.Ceiling(x);
        }

        // Рассчитываем границы осей
        private void CalculateAxisBounds()
        {
            if (_surfacePoints == null || _surfacePoints.Count == 0)
                return;

            // Находим границы данных
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var point in _surfacePoints)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxZ = Math.Max(maxZ, point.Z);
            }

            // Получаем границы осей 
            float axisMinX = (minX);
            float axisMaxX = (maxX);
            float axisMinY = (minY);
            float axisMaxY = (maxY);
            float axisMinZ = (minZ);
            float axisMaxZ = (maxZ);

            _AxisBounds = (axisMinX, axisMaxX, axisMinY, axisMaxY, axisMinZ, axisMaxZ);

            Console.WriteLine($"[Axes] X: [{axisMinX:F6}, {axisMaxX:F6}], " +
                             $"Y: [{axisMinY:F6}, {axisMaxY:F6}], " +
                             $"Z: [{axisMinZ:F6}, {axisMaxZ:F6}]");
        }

        public void SetSurfacePoints(List<Vector3> points)
        {
            if (points == null || points.Count < 3)
                return;

            _surfacePoints = points;

            // Рассчитываем границы осей
            CalculateAxisBounds();
            RegenerateMesh();

            // Помечаем, что геометрию осей нужно пересоздать
            _axesGeometryDirty = true;
            _geometryDirty = true;

            // Если контекст уже инициализирован, запрашиваем перерисовку
            if (_isInitialized)
            {
                RequestNextFrameRendering();
            }
        }

        private void UpdateGeometryBuffers(GlInterface gl)
        {
            // Удаляем старые буферы и VAO
            if (_vao != 0) gl.DeleteVertexArray(_vao);
            if (_vboVertices != 0) gl.DeleteBuffer(_vboVertices);
            if (_vboNormals != 0) gl.DeleteBuffer(_vboNormals);
            if (_vboIndices != 0) gl.DeleteBuffer(_vboIndices);

            // Создаём новые на основе актуальных _vertices, _normals, _indices
            CreateGeometryBuffers(gl);
        }

        private void RegenerateMesh()
        {
            if (_surfacePoints.Count < 3)
                return;

            // 1. Проекция на XY-плоскость
            var projectedPoints = new List<Vector2>();
            foreach (var p in _surfacePoints)
            {
                projectedPoints.Add(new Vector2(p.X, p.Y));
            }

            // 2. Delaunay триангуляция в 2D
            var triangles = DelaunayTriangulation(projectedPoints);

            // 3. Восстановление 3D геометрии
            _vertices.Clear();
            _indices.Clear();

            foreach (var tri in triangles)
            {
                uint baseIndex = (uint)_vertices.Count;

                // Добавляем вершины треугольника
                _vertices.Add(_surfacePoints[tri.Item1]);
                _vertices.Add(_surfacePoints[tri.Item2]);
                _vertices.Add(_surfacePoints[tri.Item3]);

                // Добавляем индексы
                _indices.Add(baseIndex);
                _indices.Add(baseIndex + 1);
                _indices.Add(baseIndex + 2);
            }

            // 4. Вычисление нормалей
            ComputeNormals();
        }

        private void ComputeNormals()
        {
            _normals.Clear();
            _normals.AddRange(new Vector3[_vertices.Count]);

            // Вычисляем нормали для каждого треугольника
            for (int i = 0; i < _indices.Count; i += 3)
            {
                int i0 = (int)_indices[i];
                int i1 = (int)_indices[i + 1];
                int i2 = (int)_indices[i + 2];

                var v0 = _vertices[i0];
                var v1 = _vertices[i1];
                var v2 = _vertices[i2];

                var edge1 = v1 - v0;
                var edge2 = v2 - v0;
                var normal = Vector3.Cross(edge1, edge2);

                if (normal != Vector3.Zero)
                {
                    normal = Vector3.Normalize(normal);

                    // Накапливаем нормали для сглаживания
                    _normals[i0] += normal;
                    _normals[i1] += normal;
                    _normals[i2] += normal;
                }
            }

            // Нормализуем итоговые нормали
            for (int i = 0; i < _normals.Count; i++)
            {
                if (_normals[i] != Vector3.Zero)
                    _normals[i] = Vector3.Normalize(_normals[i]);
            }
        }

        private List<(int, int, int)> DelaunayTriangulation(List<Vector2> points)
        {
            var triangles = new List<(int, int, int)>();

            if (points.Count < 3)
                return triangles;

            // Простой инкрементальный алгоритм Delaunay
            // Находим bounding box
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var p in points)
            {
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }

            // Создаем супер-треугольник
            float dx = maxX - minX;
            float dy = maxY - minY;
            float delta = Math.Max(dx, dy) * 10.0f;

            var superA = new Vector2(minX - delta, minY - delta);
            var superB = new Vector2(maxX + delta, minY - delta);
            var superC = new Vector2((minX + maxX) / 2, maxY + delta);

            // Добавляем супер-треугольник
            triangles.Add((points.Count, points.Count + 1, points.Count + 2));

            // Основной алгоритм
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var badTriangles = new List<int>();

                // Находим треугольники, чьи окружности содержат точку
                for (int j = 0; j < triangles.Count; j++)
                {
                    var (a, b, c) = triangles[j];
                    var pa = GetPoint(a, points, superA, superB, superC);
                    var pb = GetPoint(b, points, superA, superB, superC);
                    var pc = GetPoint(c, points, superA, superB, superC);

                    if (PointInCircumcircle(p, pa, pb, pc))
                    {
                        badTriangles.Add(j);
                    }
                }

                // Находим внешние ребра
                var polygon = new List<(int, int)>();

                foreach (var badIdx in badTriangles)
                {
                    var (a, b, c) = triangles[badIdx];
                    var edges = new[] { (a, b), (b, c), (c, a) };

                    foreach (var (e1, e2) in edges)
                    {
                        bool shared = false;
                        foreach (var otherIdx in badTriangles)
                        {
                            if (otherIdx == badIdx) continue;
                            var (oa, ob, oc) = triangles[otherIdx];
                            var otherEdges = new[] { (oa, ob), (ob, oc), (oc, oa) };

                            foreach (var (oe1, oe2) in otherEdges)
                            {
                                if ((e1 == oe1 && e2 == oe2) || (e1 == oe2 && e2 == oe1))
                                {
                                    shared = true;
                                    break;
                                }
                            }
                            if (shared) break;
                        }

                        if (!shared)
                        {
                            polygon.Add((e1, e2));
                        }
                    }
                }

                // Удаляем плохие треугольники
                foreach (var idx in badTriangles.OrderByDescending(x => x))
                {
                    triangles.RemoveAt(idx);
                }

                // Создаем новые треугольники
                foreach (var (e1, e2) in polygon)
                {
                    triangles.Add((e1, e2, i));
                }
            }

            // Удаляем треугольники, связанные с супер-треугольником
            triangles.RemoveAll(t =>
                t.Item1 >= points.Count ||
                t.Item2 >= points.Count ||
                t.Item3 >= points.Count);

            return triangles;
        }

        private Vector2 GetPoint(int index, List<Vector2> points, Vector2 superA, Vector2 superB, Vector2 superC)
        {
            if (index < points.Count)
                return points[index];
            if (index == points.Count)
                return superA;
            if (index == points.Count + 1)
                return superB;
            return superC;
        }

        private bool PointInCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Вычисляем определитель для проверки принадлежности точки окружности
            float ax = a.X - p.X;
            float ay = a.Y - p.Y;
            float bx = b.X - p.X;
            float by = b.Y - p.Y;
            float cx = c.X - p.X;
            float cy = c.Y - p.Y;

            float ab = ax * ax + ay * ay;
            float bc = bx * bx + by * by;
            float ca = cx * cx + cy * cy;

            float det = ax * (by * ca - cy * bc) -
                       ay * (bx * ca - cx * bc) +
                       ab * (bx * cy - cx * by);

            return det > 0;
        }

        private void CreateGeometryBuffers(GlInterface gl)
        {
            // VAO
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // VBO для вершин
            _vboVertices = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, _vboVertices);
            if (_vertices.Count > 0)
            {
                unsafe
                {
                    fixed (Vector3* ptr = _vertices.ToArray())
                        gl.BufferData(GL_ARRAY_BUFFER, _vertices.Count * sizeof(Vector3), (nint)ptr, GL_STATIC_DRAW);
                }
            }
            else
            {
                gl.BufferData(GL_ARRAY_BUFFER, 0, 0, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 0, 0);
            gl.EnableVertexAttribArray(0);

            // VBO для нормалей
            _vboNormals = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, _vboNormals);
            if (_normals.Count > 0)
            {
                unsafe
                {
                    fixed (Vector3* ptr = _normals.ToArray())
                        gl.BufferData(GL_ARRAY_BUFFER, _normals.Count * sizeof(Vector3), (nint)ptr, GL_STATIC_DRAW);
                }
            }
            else
            {
                gl.BufferData(GL_ARRAY_BUFFER, 0, 0, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, 0, 0);
            gl.EnableVertexAttribArray(1);

            // VBO для индексов
            _vboIndices = gl.GenBuffer();
            gl.BindBuffer(GlExtensions.GL_ELEMENT_ARRAY_BUFFER, _vboIndices);
            if (_indices.Count > 0)
            {
                unsafe
                {
                    fixed (uint* ptr = _indices.ToArray())
                        gl.BufferData(GlExtensions.GL_ELEMENT_ARRAY_BUFFER, _indices.Count * sizeof(uint), (nint)ptr, GL_STATIC_DRAW);
                }
            }
            else
            {
                gl.BufferData(GlExtensions.GL_ELEMENT_ARRAY_BUFFER, 0, 0, GL_STATIC_DRAW);
            }

            gl.BindVertexArray(0);
        }

        private void CreateAxesGeometry(GlInterface gl)
        {
            if (_surfacePoints == null || _surfacePoints.Count == 0)
                return;

            // Вершины для осей начинаются в минимальных точках 
            var axisVertices = new Vector3[]
            {
                // Ось X: от (minX, minY, minZ) до (maxX, minY, minZ)
                new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.minZ),
                new Vector3(_AxisBounds.maxX, _AxisBounds.minY, _AxisBounds.minZ),
                
                // Ось Y: от (minX, minY, minZ) до (minX, maxY, minZ)
                new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.minZ),
                new Vector3(_AxisBounds.minX, _AxisBounds.maxY, _AxisBounds.minZ),
                
                // Ось Z: от (minX, minY, minZ) до (minX, minY, maxZ)
                new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.minZ),
                new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.maxZ)
            };

            // VAO для осей
            _axesVao = gl.GenVertexArray();
            gl.BindVertexArray(_axesVao);

            // VBO для вершин осей
            _axesVboVertices = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, _axesVboVertices);
            unsafe
            {
                fixed (Vector3* ptr = axisVertices)
                    gl.BufferData(GL_ARRAY_BUFFER, axisVertices.Length * sizeof(Vector3), (nint)ptr, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 0, 0);
            gl.EnableVertexAttribArray(0);

            gl.BindVertexArray(0);
            GlCheckError(gl, "CreateAxesGeometry");
        }

        private void ConfigureAxesShaders(GlInterface gl)
        {
            // Загружаем шейдеры из ресурсов
            var vertexSource = LoadShaderFromResource(AxesVertexShaderResource);
            var fragmentSource = LoadShaderFromResource(AxesFragmentShaderResource);

            // Компилируем вершинный шейдер
            _axesVertexShader = gl.CreateShader(GL_VERTEX_SHADER);
            GlCheckError(gl, "Create axes vertex shader");

            var res = gl.CompileShaderAndGetError(_axesVertexShader, vertexSource);
            if (res != null) throw new Exception("Axes vertex shader compile error: " + res);
            GlCheckError(gl, "Compile axes vertex shader");

            // Компилируем фрагментный шейдер
            _axesFragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
            GlCheckError(gl, "Create axes fragment shader");

            res = gl.CompileShaderAndGetError(_axesFragmentShader, fragmentSource);
            if (res != null) throw new Exception("Axes fragment shader compile error: " + res);
            GlCheckError(gl, "Compile axes fragment shader");

            // Создаем программу шейдеров
            _axesShaderProgram = gl.CreateProgram();
            GlCheckError(gl, "Create axes shader program");

            gl.AttachShader(_axesShaderProgram, _axesVertexShader);
            GlCheckError(gl, "Attach axes vertex shader");

            gl.AttachShader(_axesShaderProgram, _axesFragmentShader);
            GlCheckError(gl, "Attach axes fragment shader");

            gl.LinkProgram(_axesShaderProgram);
            GlCheckError(gl, "Link axes shader program");

            // Получаем uniform locations
            _axesModelLoc = gl.GetUniformLocationString(_axesShaderProgram, "model");
            _axesViewLoc = gl.GetUniformLocationString(_axesShaderProgram, "view");
            _axesProjectionLoc = gl.GetUniformLocationString(_axesShaderProgram, "projection");
            _axisColorR = gl.GetUniformLocationString(_axesShaderProgram, "axisColorR");
            _axisColorG = gl.GetUniformLocationString(_axesShaderProgram, "axisColorG");
            _axisColorB = gl.GetUniformLocationString(_axesShaderProgram, "axisColorB");

            GlCheckError(gl, "ConfigureAxesShaders");
        }

        private void RecreateAxesGeometry(GlInterface gl)
        {
            // Очищаем старую геометрию осей
            if (_axesVao != 0)
            {
                gl.DeleteVertexArray(_axesVao);
                gl.DeleteBuffer(_axesVboVertices);
                _axesVao = 0;
                _axesVboVertices = 0;
            }

            // Создаем новую геометрию осей
            CreateAxesGeometry(gl);
            GlCheckError(gl, "RecreateAxesGeometry");
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);

            // Получаем uniform locations для освещения
            _lightPosR = gl.GetUniformLocationString(_shaderProgram, "lightPosR");
            _lightPosG = gl.GetUniformLocationString(_shaderProgram, "lightPosG");
            _lightPosB = gl.GetUniformLocationString(_shaderProgram, "lightPosB");

            _lightColorR = gl.GetUniformLocationString(_shaderProgram, "lightColorR");
            _lightColorG = gl.GetUniformLocationString(_shaderProgram, "lightColorG");
            _lightColorB = gl.GetUniformLocationString(_shaderProgram, "lightColorB");

            _objectColorR = gl.GetUniformLocationString(_shaderProgram, "objectColorR");
            _objectColorG = gl.GetUniformLocationString(_shaderProgram, "objectColorG");
            _objectColorB = gl.GetUniformLocationString(_shaderProgram, "objectColorB");

            GlCheckError(gl, "Get lighting uniform locations");

            // Инициализация осей
            if (ShowAxes)
            {
                CreateAxesGeometry(gl);
                ConfigureAxesShaders(gl);
                _axesGeometryDirty = false;
            }

            _isInitialized = true;

            // Если есть точки, создаем геометрию
            if (_surfacePoints.Count > 0)
            {
                RegenerateMesh();
                // Рассчитываем границы осей 
                CalculateAxisBounds();
            }
        }

        protected override void CreateGeometry(GlInterface gl)
        {
            if (_surfacePoints.Count > 0 && _vertices.Count == 0)
            {
                RegenerateMesh();
            }

            CreateGeometryBuffers(gl);
            _geometryDirty = false;

            GlCheckError(gl, "CreateGeometry");
        }

        protected override void DrawGeometry(GlInterface gl)
        {
            if (_indices.Count == 0 || !_isInitialized)
                return;

            // Рисуем геометрию
            gl.BindVertexArray(_vao);
            gl.DrawElements(GL_TRIANGLES, _indices.Count, GlExtensions.GL_UNSIGNED_INT, 0);
            gl.BindVertexArray(0);

            GlCheckError(gl, "DrawGeometry");
        }

        protected override void UpdateUniforms(GlInterface gl, int width, int height)
        {
            _width = width;
            _height = height;

            // Матрицы
            var aspect = _width / (float)_height;
            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, 0.1f, 100f);

            // Используем кастомную матрицу вида, если она задана
            if (_useCustomViewMatrix)
            {
                _viewMatrix = _customViewMatrix;
            }
            else
            {
                _viewMatrix = Matrix4x4.CreateLookAt(
                    new Vector3(0, 0, 5),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0)
                );
            }

            var modelMatrix = Matrix4x4.Identity;

            int modelLoc = gl.GetUniformLocationString(_shaderProgram, "model");
            int viewLoc = gl.GetUniformLocationString(_shaderProgram, "view");
            int projectionLoc = gl.GetUniformLocationString(_shaderProgram, "projection");

            SetUniformMatrix4(gl, modelLoc, modelMatrix);
            SetUniformMatrix4(gl, viewLoc, _viewMatrix);
            SetUniformMatrix4(gl, projectionLoc, _projectionMatrix);

            // Параметры освещения
            gl.Uniform1f(_lightPosR, _lightPosition.X);
            gl.Uniform1f(_lightPosG, _lightPosition.Y);
            gl.Uniform1f(_lightPosB, _lightPosition.Z);

            gl.Uniform1f(_lightColorR, _lightColor.X);
            gl.Uniform1f(_lightColorG, _lightColor.Y);
            gl.Uniform1f(_lightColorB, _lightColor.Z);

            gl.Uniform1f(_objectColorR, _surfaceColor.X);
            gl.Uniform1f(_objectColorG, _surfaceColor.Y);
            gl.Uniform1f(_objectColorB, _surfaceColor.Z);
        }

        private void DrawAxes(GlInterface gl, int width, int height)
        {
            if (_axesVao == 0 || _axesShaderProgram == 0)
                return;

            gl.UseProgram(_axesShaderProgram);

            // Обновляем uniform переменные для осей
            UpdateAxesUniforms(gl, width, height);

            // Устанавливаем цвет осей
            gl.Uniform1f(_axisColorR, AxesColor.X);
            gl.Uniform1f(_axisColorG, AxesColor.Y);
            gl.Uniform1f(_axisColorB, AxesColor.Z);

            // Рисуем оси
            gl.BindVertexArray(_axesVao);
            gl.DrawArrays(GlExtensions.GL_LINES, 0, 6); // 3 линии по 2 вершины
            gl.BindVertexArray(0);

            GlCheckError(gl, "DrawAxes");
        }

        private void UpdateAxesUniforms(GlInterface gl, int width, int height)
        {
            // Матрицы для осей (используем те же, что и для поверхности)
            var aspect = width / (float)height;
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, 0.1f, 100f);

            Matrix4x4 viewMatrix;
            if (_useCustomViewMatrix)
            {
                viewMatrix = _customViewMatrix;
            }
            else
            {
                viewMatrix = Matrix4x4.CreateLookAt(
                    new Vector3(0, 0, 5),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0)
                );
            }

            var modelMatrix = Matrix4x4.Identity;

            SetUniformMatrix4(gl, _axesModelLoc, modelMatrix);
            SetUniformMatrix4(gl, _axesViewLoc, viewMatrix);
            SetUniformMatrix4(gl, _axesProjectionLoc, projectionMatrix);

            _axisCaptions = AxesToScreen(width, height);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            int width = (int)Bounds.Width;
            int height = (int)Bounds.Height;

            gl.Viewport(0, 0, width, height);

            gl.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, 1.0f);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            // Пересоздаем геометрию осей, если она "грязная"
            if (_axesGeometryDirty && ShowAxes && _isInitialized)
            {
                RecreateAxesGeometry(gl);
                _axesGeometryDirty = false;
            }

            if (_geometryDirty && _isInitialized)
            {
                UpdateGeometryBuffers(gl);
                _geometryDirty = false;
            }

            // Сначала рисуем поверхность
            if (_indices.Count > 0 && _isInitialized)
            {
                gl.UseProgram(_shaderProgram);
                UpdateUniforms(gl, width, height);
                DrawGeometry(gl);
            }

            // Затем рисуем оси, если они включены
            if (ShowAxes && _axesVao != 0)
            {
                DrawAxes(gl, width, height);
            }

            GlCheckError(gl, "OnOpenGlRender");
        }

        protected override void CleanupGeometry(GlInterface gl)
        {
            if (_vao != 0)
            {
                gl.DeleteVertexArray(_vao);
                _vao = 0;
            }

            if (_vboVertices != 0)
            {
                gl.DeleteBuffer(_vboVertices);
                _vboVertices = 0;
            }

            if (_vboNormals != 0)
            {
                gl.DeleteBuffer(_vboNormals);
                _vboNormals = 0;
            }

            if (_vboIndices != 0)
            {
                gl.DeleteBuffer(_vboIndices);
                _vboIndices = 0;
            }

            // Очищаем ресурсы осей
            if (_axesVao != 0)
            {
                gl.DeleteVertexArray(_axesVao);
                _axesVao = 0;
            }

            if (_axesVboVertices != 0)
            {
                gl.DeleteBuffer(_axesVboVertices);
                _axesVboVertices = 0;
            }

            // Удаляем шейдеры осей
            if (_axesShaderProgram != 0)
            {
                gl.UseProgram(0);
                gl.DeleteProgram(_axesShaderProgram);
                gl.DeleteShader(_axesFragmentShader);
                gl.DeleteShader(_axesVertexShader);
                _axesShaderProgram = 0;
            }

            _axesGeometryDirty = false;
            _isInitialized = false;
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            base.OnOpenGlDeinit(gl);
            CleanupGeometry(gl);
        }

        private (Vector3 world, Vector2 screen)[] AxesToScreen(int width, int height)
        {
            // Применяем матрицы преобразования
            var modelView = Matrix4x4.Identity * _viewMatrix;
            var mvp = modelView * _projectionMatrix;

            (Vector3 worldPoint, Vector2 screenPoint)[] points =
            {
                (new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.minZ), new Vector2()),
                (new Vector3(_AxisBounds.maxX, _AxisBounds.minY, _AxisBounds.minZ), new Vector2()),
                (new Vector3(_AxisBounds.minX, _AxisBounds.maxY, _AxisBounds.minZ), new Vector2()),
                (new Vector3(_AxisBounds.minX, _AxisBounds.minY, _AxisBounds.maxZ), new Vector2()),
            };

            for (var i = 0; i < points.Length; i++)
            {
                var p4 = new Vector4(points[i].worldPoint, 1.0f);
                var transformed = Vector4.Transform(p4, mvp);

                if(Math.Abs(transformed.W) > float.Epsilon)
                {
                    transformed.X /= transformed.W;
                    transformed.Y /= transformed.W;
                    transformed.Z /= transformed.W;
                }

                points[i].screenPoint.X = ((transformed.X + 1.0f) * 0.5f) * width;
                points[i].screenPoint.Y = ((1.0f - transformed.Y) * 0.5f) * height;
            }

            return points;
        }
                
    }
}