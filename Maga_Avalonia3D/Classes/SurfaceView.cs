using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
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

        public CameraController Camera => _cameraController;
        Canvas _canvas;
        TextBlock _xCaption = new TextBlock();
        TextBlock _yCaption = new TextBlock();
        TextBlock _zCaption = new TextBlock();
        TextBlock _0Caption = new TextBlock();

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
                Background = new SolidColorBrush(new Color(0,0,0,0)),
            };

            Content = _canvas;

            this.LayoutUpdated += (sender, e) =>
            {
                _canvas.Width = Bounds.Width;
                _canvas.Height = Bounds.Height;
                _surfaceRenderer.Width = Bounds.Width;
                _surfaceRenderer.Height = Bounds.Height;
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

                var caps = _surfaceRenderer.AxisCaptions;
                if (ShowAxes && caps != null)
                {
                    _0Caption.IsVisible = true;
                    _xCaption.IsVisible = true;
                    _yCaption.IsVisible = true;
                    _zCaption.IsVisible = true;


                    _0Caption.Text = $"{caps[0].world.X} {caps[0].world.Y} {caps[0].world.Z}";
                    Canvas.SetLeft(_0Caption, caps[0].screen.X);
                    Canvas.SetTop(_0Caption, caps[0].screen.Y);

                    _xCaption.Text = caps[1].world.X.ToString();
                    Canvas.SetLeft(_xCaption, caps[1].screen.X);
                    Canvas.SetTop(_xCaption, caps[1].screen.Y);

                    _yCaption.Text = caps[2].world.Y.ToString();
                    Canvas.SetLeft(_yCaption, caps[2].screen.X);
                    Canvas.SetTop(_yCaption, caps[2].screen.Y);

                    _zCaption.Text = caps[3].world.Z.ToString();
                    Canvas.SetLeft(_zCaption, caps[3].screen.X);
                    Canvas.SetTop(_zCaption, caps[3].screen.Y);
                }
                else
                {
                    _0Caption.IsVisible = false;
                    _xCaption.IsVisible = false;
                    _yCaption.IsVisible = false;
                    _zCaption.IsVisible = false;
                }
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