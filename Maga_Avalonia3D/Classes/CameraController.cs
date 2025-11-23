using System;
using System.Numerics;
using Avalonia;

namespace Maga_Avalonia3D.Classes
{
    public class CameraController
    {
        // Параметры камеры ДЛЯ СИСТЕМЫ КООРДИНАТ Z-UP
        public Vector3 Position { get; private set; } = new Vector3(0, -5, 0); // Начальная позиция: сзади сцены
        public Vector3 Target { get; set; } = new Vector3(0, 0, 0);           // Центр сцены
        public Vector3 Up { get; set; } = new Vector3(0, 0, 1);               // ОСЬ Z ВВЕРХ - ключевое изменение!

        // Параметры вращения для Z-up системы
        public float Yaw { get; set; } = 0;      // Вращение вокруг вертикальной оси Z (влево/вправо)
        public float Pitch { get; set; } = 0;    // Вращение вокруг горизонтальной оси X (вверх/вниз)
        public float Distance { get; set; } = 5f; // Расстояние до цели

        // Ограничения
        public float MinDistance { get; set; } = 1.0f;
        public float MaxDistance { get; set; } = 20.0f;
        public float MaxPitch { get; set; } = MathF.PI / 2 * 0.9f; // Не позволять переворачиваться

        // Состояние для вращения мыши
        private bool _isRotating;
        private Point _lastMousePosition;

        public Matrix4x4 GetViewMatrix()
        {
            // В системе Z-up:
            // - Ось Z: вертикаль (вверх/вниз)
            // - Ось Y: глубина (ближе/дальше)
            // - Ось X: горизонталь (влево/вправо)

            // Вычисляем направление камеры на основе углов
            // Yaw - вращение вокруг Z (вертикальная ось)
            // Pitch - вращение вокруг X (горизонтальная ось)

            var direction = new Vector3(
                MathF.Cos(Yaw) * MathF.Cos(Pitch),  // X: влево/вправо
                -MathF.Sin(Yaw) * MathF.Cos(Pitch), // Y: вперед/назад (инвертировано для правильного направления)
                MathF.Sin(Pitch)                    // Z: вверх/вниз
            );

            // Вычисляем позицию камеры
            Position = Target + direction * Distance;

            // Для отладки:
            Console.WriteLine($"[CAMERA Z-up] Pos: {Position.X:F2}, {Position.Y:F2}, {Position.Z:F2}, " +
                             $"Dir: {direction.X:F2}, {direction.Y:F2}, {direction.Z:F2}, " +
                             $"Yaw: {Yaw:F2}, Pitch: {Pitch:F2}");

            return Matrix4x4.CreateLookAt(Position, Target, Up);
        }

        // Начало вращения
        public void StartRotation(Point mousePosition)
        {
            _isRotating = true;
            _lastMousePosition = mousePosition;
            Console.WriteLine($"[CAMERA Z-up] Rotation started at: {mousePosition.X}, {mousePosition.Y}");
        }

        // Обновление вращения для Z-up системы
        public void UpdateRotation(Point mousePosition)
        {
            if (!_isRotating)
                return;

            var delta = new Point(
                mousePosition.X - _lastMousePosition.X,
                mousePosition.Y - _lastMousePosition.Y
            );

            // Чувствительность вращения
            const float sensitivity = 0.005f;

            // Вращение вокруг вертикальной оси Z (влево/вправо)
            Yaw += (float)delta.X * sensitivity;

            // Вращение вокруг горизонтальной оси X (вверх/вниз)
            Pitch += (float)delta.Y * sensitivity;

            // Ограничения по Pitch (не позволяем камере перевернуться)
            Pitch = Math.Clamp(Pitch, -MaxPitch, MaxPitch);

            // Нормализуем Yaw для предотвращения переполнения
            if (Yaw > MathF.PI * 2)
                Yaw -= MathF.PI * 2;
            else if (Yaw < -MathF.PI * 2)
                Yaw += MathF.PI * 2;

            _lastMousePosition = mousePosition;

            Console.WriteLine($"[CAMERA Z-up] Yaw: {Yaw:F2}, Pitch: {Pitch:F2}, Delta: {delta.X:F1}, {delta.Y:F1}");
        }

        // Завершение вращения
        public void EndRotation()
        {
            _isRotating = false;
            Console.WriteLine("[CAMERA Z-up] Rotation ended");
        }

        // Обработка колеса мыши
        public void HandleWheelDelta(double delta)
        {
            // Приближение/отдаление вдоль оси Y (глубина)
            Distance -= (float)delta * 0.5f;
            Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
            Console.WriteLine($"[CAMERA Z-up] Distance changed to: {Distance:F2}");
        }

        // Сброс камеры в исходное положение ДЛЯ Z-UP СИСТЕМЫ
        public void Reset()
        {
            Yaw = 0;
            Pitch = 0;
            Distance = 5f;
            Position = new Vector3(0, -5, 0); // Сзади сцены, смотрит вперед
            Console.WriteLine("[CAMERA Z-up] Camera reset to default Z-up position");
        }

        public bool IsRotating => _isRotating;
    }
}