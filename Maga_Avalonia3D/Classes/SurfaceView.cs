using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maga_Avalonia3D.Classes
{
    public class SurfaceView : UserControl
    {
        private readonly SurfaceRenderer _surfaceRenderer;
        private readonly CameraController _cameraController;
        private readonly DispatcherTimer _renderTimer;

        // Свойства для настройки
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

        public CameraController Camera => _cameraController;

        public SurfaceView()
        {
            // Создаем дочерние компоненты
            _surfaceRenderer = new SurfaceRenderer();
            _cameraController = new CameraController();

            // Настраиваем SurfaceRenderer
            _surfaceRenderer.ClearColor = new Vector3(0.1f, 0.1f, 0.1f);
            _surfaceRenderer.LightPosition = new Vector3(3.0f, 3.0f, 3.0f);
            _surfaceRenderer.LightColor = new Vector3(1.0f, 1.0f, 1.0f);
            _surfaceRenderer.SurfaceColor = new Vector3(0.3f, 0.6f, 1.0f);
            _surfaceRenderer.AxesColor = new Vector3(1.0f, 1.0f, 0);
            _surfaceRenderer.ShowAxes = true;

            // Добавляем SurfaceRenderer в визуальное дерево
            Content = new Grid
            {
                Children =
                {
                    _surfaceRenderer
                },
                Background = new SolidColorBrush(new Color(0,0,0,0))
            };

            // Подписываемся на события ввода
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
            this.PointerWheelChanged += OnPointerWheelChanged;

            // Создаем таймер для обновления рендера (60 FPS)
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _renderTimer.Tick += OnRenderTimerTick;
            _renderTimer.Start();
        }

        // Установка точек поверхности
        public void SetSurfacePoints(List<Vector3> points)
        {
            _surfaceRenderer.SetSurfacePoints(points);
        }

        // Обработка событий ввода
        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(this);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _cameraController.StartRotation(point);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(this);

            if (_cameraController.IsRotating)
            {
                _cameraController.UpdateRotation(point);
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                _cameraController.EndRotation();
                e.Handled = true;
            }
        }

        private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            _cameraController.HandleWheelDelta(e.Delta.Y);
            e.Handled = true;
        }

        // Обработчик таймера для обновления рендера
        private void OnRenderTimerTick(object sender, EventArgs e)
        {
            // Проверяем инициализацию через публичное свойство
            if (_surfaceRenderer.IsInitialized)
            {
                // Получаем матрицу вида от камеры
                var viewMatrix = _cameraController.GetViewMatrix();

                // Устанавливаем кастомную матрицу вида
                _surfaceRenderer.UseCustomViewMatrix = true;
                _surfaceRenderer.CustomViewMatrix = viewMatrix;

                // Запрашиваем перерисовку
                _surfaceRenderer.RequestNextFrameRendering();
            }
        }

        // Очистка при удалении
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _renderTimer.Stop();
            _renderTimer.Tick -= OnRenderTimerTick;
            base.OnDetachedFromVisualTree(e);
        }

        // Сброс камеры
        public void ResetCamera()
        {
            _cameraController.Reset();
        }
    }
}