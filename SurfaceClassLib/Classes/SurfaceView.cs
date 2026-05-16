using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SurfaceLib
{
    public class SurfaceView : UserControl
    {
        private readonly SurfaceRenderer _surfaceRenderer;
        private readonly CameraController _cameraController;
        private readonly DispatcherTimer _renderTimer;

        // Свойства для настройки внешнего вида (пробрасываются в рендерер)
        public Vector3 ClearColor
        {
            get => _surfaceRenderer.ClearColor;
            set => _surfaceRenderer.ClearColor = value;
        }

        public Vector3 LightPosition
        {
            get => _surfaceRenderer.LightPosition;
            set => _surfaceRenderer.LightPosition = value;
        }

        public Vector3 LightColor
        {
            get => _surfaceRenderer.LightColor;
            set => _surfaceRenderer.LightColor = value;
        }

        public Vector3 SurfaceColor
        {
            get => _surfaceRenderer.SurfaceColor;
            set => _surfaceRenderer.SurfaceColor = value;
        }

        public Vector3 AxesColor
        {
            get => _surfaceRenderer.AxesColor;
            set => _surfaceRenderer.AxesColor = value;
        }

        public bool ShowAxes
        {
            get => _surfaceRenderer.ShowAxes;
            set => _surfaceRenderer.ShowAxes = value;
        }

        private int _digits = 2;
        public int Digits
        {
            get => _digits;
            set => _digits = value;
        }

        // Открытый доступ к контроллеру камеры для внешнего управления
        public CameraController Camera => _cameraController;

        // Элементы интерфейса для подписей осей
        private readonly Canvas _canvas;
        private readonly TextBlock _xCaption = new TextBlock();
        private readonly TextBlock _yCaption = new TextBlock();
        private readonly TextBlock _zCaption = new TextBlock();
        private readonly TextBlock _0Caption = new TextBlock();

        public SurfaceView()
        {
            _surfaceRenderer = new SurfaceRenderer();
            _cameraController = new CameraController();

            // Начальные настройки рендерера
            _surfaceRenderer.ClearColor = new Vector3(0.1f, 0.1f, 0.1f);
            _surfaceRenderer.LightPosition = new Vector3(3.0f, 3.0f, 3.0f);
            _surfaceRenderer.LightColor = new Vector3(1.0f, 1.0f, 1.0f);
            _surfaceRenderer.SurfaceColor = new Vector3(0.3f, 0.6f, 1.0f);
            _surfaceRenderer.AxesColor = new Vector3(1.0f, 1.0f, 0);
            _surfaceRenderer.ShowAxes = true;

            // Компоновка
            _canvas = new Canvas
            {
                Children =
                {
                    _surfaceRenderer,
                    _xCaption,
                    _yCaption,
                    _zCaption,
                    _0Caption
                },
                Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(0, 0, 0, 0)),
            };

            Content = _canvas;

            // Автоматическая подгонка размеров
            this.LayoutUpdated += (sender, e) =>
            {
                _canvas.Width = Bounds.Width;
                _canvas.Height = Bounds.Height;
                _surfaceRenderer.Width = Bounds.Width;
                _surfaceRenderer.Height = Bounds.Height;
            };

            // Таймер для регулярного обновления матрицы камеры и подписей
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _renderTimer.Tick += OnRenderTimerTick;
            _renderTimer.Start();
        }

        // Загрузка точек поверхности (единственный способ обновить данные)
        public void SetSurfacePoints(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return;

            // Вычисляем границы и позиционируем камеру
            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);
            var minZ = points.Min(p => p.Z);
            var maxZ = points.Max(p => p.Z);

            var target = new Vector3((maxX + minX) / 2, (maxY + minY) / 2, (maxZ + minZ) / 2);
            _cameraController.Target = target;

            var minV = new Vector3(minX, minY, minZ);
            var maxV = new Vector3(maxX, maxY, maxZ);
            var dist = maxV - minV;
            _cameraController.MaxDistance = MathF.Sqrt(dist.X * dist.X + dist.Y * dist.Y + dist.Z * dist.Z) * 10.0f;

            // Источник света помещаем высоко над поверхностью
            var lightPos = target with { Z = maxZ * 10 };
            _surfaceRenderer.LightPosition = lightPos;

            // Передаём точки в рендерер
            _surfaceRenderer.SetSurfacePoints(points);
        }

        // Каждый кадр передаём актуальную матрицу вида и обновляем подписи осей
        private void OnRenderTimerTick(object sender, EventArgs e)
        {
            if (!_surfaceRenderer.IsInitialized)
                return;

            // Передаём матрицу вида от камеры
            _surfaceRenderer.UseCustomViewMatrix = true;
            _surfaceRenderer.CustomViewMatrix = _cameraController.GetViewMatrix();
            _surfaceRenderer.RequestNextFrameRendering();

            // Обновляем экранные координаты подписей осей
            var captions = _surfaceRenderer.AxisCaptions;
            if (ShowAxes && captions != null)
            {
                _0Caption.IsVisible = true;
                _xCaption.IsVisible = true;
                _yCaption.IsVisible = true;
                _zCaption.IsVisible = true;

                string formatted = string.Format(
                    $"{{0:F{_digits}}} {{1:F{_digits}}} {{2:F{_digits}}}",
                    captions[0].world.X, captions[0].world.Y, captions[0].world.Z);
                _0Caption.Text = formatted;
                Canvas.SetLeft(_0Caption, captions[0].screen.X - _0Caption.Bounds.Width / 2);
                Canvas.SetTop(_0Caption, captions[0].screen.Y);

                _xCaption.Text = string.Format($"{{0:F{_digits}}}", captions[1].world.X);
                Canvas.SetLeft(_xCaption, captions[1].screen.X);
                Canvas.SetTop(_xCaption, captions[1].screen.Y);

                _yCaption.Text = string.Format($"{{0:F{_digits}}}", captions[2].world.Y);
                Canvas.SetLeft(_yCaption, captions[2].screen.X - _yCaption.Bounds.Width);
                Canvas.SetTop(_yCaption, captions[2].screen.Y);

                _zCaption.Text = string.Format($"{{0:F{_digits}}}", captions[3].world.Z);
                Canvas.SetLeft(_zCaption, captions[3].screen.X - _zCaption.Bounds.Width);
                Canvas.SetTop(_zCaption, captions[3].screen.Y);
            }
            else
            {
                _0Caption.IsVisible = false;
                _xCaption.IsVisible = false;
                _yCaption.IsVisible = false;
                _zCaption.IsVisible = false;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _renderTimer.Stop();
            _renderTimer.Tick -= OnRenderTimerTick;
            base.OnDetachedFromVisualTree(e);
        }

        // Удобный метод для сброса камеры к исходным параметрам
        public void ResetCamera()
        {
            _cameraController.Reset();
        }
    }
}