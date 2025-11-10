using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace Maga_Avalonia3D.Classes
{
    public enum PrimitiveType
    {
        Cube,
        Sphere,
        Pyramid
    }

    public struct PrimitiveInstance
    {
        public PrimitiveType Type;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 Color;
    }

    public class SceneRenderer : OpenGlCommonBase
    {
        private List<PrimitiveInstance> _primitives = new();
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private readonly Dictionary<PrimitiveType, (int vao, int vertexCount)> _geometryCache = new();
        private int _width;
        private int _height;

        private int _colorRLocation = -1;
        private int _colorGLocation = -1;
        private int _colorBLocation = -1;

        private Vector3 _clearColor = new(0.1f, 0.1f, 0.1f);

        public Vector3 ClearColor
        {
            get => _clearColor;
            set => _clearColor = value;
        }

        protected override string VertexShaderResource => "Maga_Avalonia3D.Shaders.SceneRenderer.vert";
        protected override string FragmentShaderResource => "Maga_Avalonia3D.Shaders.SceneRenderer.frag";

        public void SetScene(List<PrimitiveInstance> primitives)
        {
            _primitives = primitives;
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);

            _colorRLocation = gl.GetUniformLocationString(_shaderProgram, "colorR");
            _colorGLocation = gl.GetUniformLocationString(_shaderProgram, "colorG");
            _colorBLocation = gl.GetUniformLocationString(_shaderProgram, "colorB");

            GlCheckError(gl, "Get color uniform locations");
        }

        protected override void CreateGeometry(GlInterface gl)
        {
            _geometryCache[PrimitiveType.Cube] = CreateCubeGeometry(gl);
            _geometryCache[PrimitiveType.Sphere] = CreateSphereGeometry(gl, 32, 32);
            _geometryCache[PrimitiveType.Pyramid] = CreatePyramidGeometry(gl);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            int width = (int)Bounds.Width;
            int height = (int)Bounds.Height;

            gl.Viewport(0, 0, width, height);
            gl.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, 1.0f);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            // Убедимся, что глубина работает
            gl.Enable(GL_DEPTH_TEST);
            //gl.DepthFunc(GL_LEQUAL);
            //gl.Disable(GL_CULL_FACE); // временно отключаем отсечение

            gl.UseProgram(_shaderProgram);
            UpdateUniforms(gl, width, height);
            DrawGeometry(gl);

            GlCheckError(gl, "OnOpenGlRender");
        }

        protected override void DrawGeometry(GlInterface gl)
        {
            var aspect = _width / (float)_height;
            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, 0.1f, 100f);
            _viewMatrix = Matrix4x4.CreateLookAt(
                new Vector3(0, 0, 5),
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0)
            );

            foreach (var prim in _primitives)
            {
                if (!_geometryCache.TryGetValue(prim.Type, out var geo))
                    continue;

                var model = Matrix4x4.CreateScale(prim.Scale) *
                            Matrix4x4.CreateFromYawPitchRoll(prim.Rotation.Y, prim.Rotation.X, prim.Rotation.Z) *
                            Matrix4x4.CreateTranslation(prim.Position);

                SetUniformMatrix4(gl, _model, model);
                SetUniformMatrix4(gl, _view, _viewMatrix);
                SetUniformMatrix4(gl, _projection, _projectionMatrix);

                gl.Uniform1f(_colorRLocation, prim.Color.X);
                gl.Uniform1f(_colorGLocation, prim.Color.Y);
                gl.Uniform1f(_colorBLocation, prim.Color.Z);

                gl.BindVertexArray(geo.vao);
                gl.DrawArrays(GL_TRIANGLES, 0, geo.vertexCount);
            }
        }

        protected override void CleanupGeometry(GlInterface gl)
        {
            foreach (var (_, geometry) in _geometryCache)
            {
                gl.DeleteVertexArray(geometry.vao);
            }
            _geometryCache.Clear();
        }

        private (int vao, int vertexCount) CreateCubeGeometry(GlInterface gl)
        {
            var vertices = new List<float>();
            var addFace = new Action<float, float, float, float, float, float, float, float, float, float, float, float>((x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3) =>
            {
                // Нормаль вычисляется как cross((p1-p0), (p2-p0))
                var e1 = new Vector3(x1 - x0, y1 - y0, z1 - z0);
                var e2 = new Vector3(x2 - x0, y2 - y0, z2 - z0);
                var normal = Vector3.Normalize(Vector3.Cross(e1, e2));

                vertices.AddRange(new[] { x0, y0, z0, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { x1, y1, z1, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { x2, y2, z2, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { x0, y0, z0, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { x2, y2, z2, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { x3, y3, z3, normal.X, normal.Y, normal.Z });
            });

            // front
            addFace(-0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f);
            // back
            addFace(-0.5f, -0.5f, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, -0.5f, -0.5f);
            // left
            addFace(-0.5f, -0.5f, -0.5f, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, -0.5f);
            // right
            addFace(0.5f, -0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0.5f);
            // top
            addFace(-0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f);
            // bottom
            addFace(-0.5f, -0.5f, -0.5f, 0.5f, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, -0.5f, -0.5f, 0.5f);

            int vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            int vbo = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, vbo);
            unsafe
            {
                fixed (float* ptr = vertices.ToArray())
                    gl.BufferData(GL_ARRAY_BUFFER, vertices.Count * sizeof(float), (nint)ptr, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 6 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, 6 * sizeof(float), 3 * sizeof(float));
            gl.EnableVertexAttribArray(1);

            gl.BindVertexArray(0);

            return (vao, vertices.Count / 6);
        }

        private (int vao, int vertexCount) CreatePyramidGeometry(GlInterface gl)
        {
            // Квадратная пирамида без основания (только 4 боковые грани)
            var vertices = new List<float>();

            var apex = new Vector3(0, 0.5f, 0);
            var baseVerts = new[]
            {
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f)
            };

            // Добавляем 4 треугольника (боковые грани)
            for (int i = 0; i < 4; i++)
            {
                var v0 = apex;
                var v1 = baseVerts[i];
                var v2 = baseVerts[(i + 1) % 4];

                // Нормаль для каждой грани
                var e1 = v1 - v0;
                var e2 = v2 - v0;
                var normal = Vector3.Normalize(Vector3.Cross(e1, e2));

                vertices.AddRange(new[] { v0.X, v0.Y, v0.Z, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z });
                vertices.AddRange(new[] { v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z });
            }

            int vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            int vbo = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, vbo);
            unsafe
            {
                fixed (float* ptr = vertices.ToArray())
                    gl.BufferData(GL_ARRAY_BUFFER, vertices.Count * sizeof(float), (nint)ptr, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 6 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, 6 * sizeof(float), 3 * sizeof(float));
            gl.EnableVertexAttribArray(1);

            gl.BindVertexArray(0);

            return (vao, vertices.Count / 6);
        }

        private (int vao, int vertexCount) CreateSphereGeometry(GlInterface gl, int stacks, int slices)
        {
            var vertices = new List<float>();

            for (int i = 0; i <= stacks; i++)
            {
                float phi = MathF.PI * i / stacks;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                for (int j = 0; j <= slices; j++)
                {
                    float theta = 2 * MathF.PI * j / slices;
                    float sinTheta = MathF.Sin(theta);
                    float cosTheta = MathF.Cos(theta);

                    float x = sinPhi * cosTheta;
                    float y = cosPhi;
                    float z = sinPhi * sinTheta;

                    // Нормаль совпадает с позицией для сферы
                    vertices.AddRange(new[] { x, y, z, x, y, z });
                }
            }

            var indices = new List<uint>();
            for (int i = 0; i < stacks; i++)
            {
                for (int j = 0; j < slices; j++)
                {
                    uint p0 = (uint)(i * (slices + 1) + j);
                    uint p1 = (uint)(i * (slices + 1) + (j + 1));
                    uint p2 = (uint)((i + 1) * (slices + 1) + j);
                    uint p3 = (uint)((i + 1) * (slices + 1) + (j + 1));

                    // Два треугольника на четырёхугольник
                    indices.AddRange(new uint[] { p0, p2, p1 });
                    indices.AddRange(new uint[] { p1, p2, p3 });
                }
            }

            // Преобразуем индексы в плоский массив вершин (no VBO indexing)
            var indexedVertices = new List<float>();
            foreach (uint idx in indices)
            {
                int baseIdx = (int)idx * 6;
                for (int k = 0; k < 6; k++)
                    indexedVertices.Add(vertices[baseIdx + k]);
            }

            int vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            int vbo = gl.GenBuffer();
            gl.BindBuffer(GL_ARRAY_BUFFER, vbo);
            unsafe
            {
                fixed (float* ptr = indexedVertices.ToArray())
                    gl.BufferData(GL_ARRAY_BUFFER, indexedVertices.Count * sizeof(float), (nint)ptr, GL_STATIC_DRAW);
            }

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 6 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, 6 * sizeof(float), 3 * sizeof(float));
            gl.EnableVertexAttribArray(1);

            gl.BindVertexArray(0);

            return (vao, indexedVertices.Count / 6);
        }

        protected override void UpdateUniforms(GlInterface gl, int width, int height)
        {
            _width = width;
            _height = height;
        }
    }
}