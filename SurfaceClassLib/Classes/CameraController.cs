using System;
using System.Numerics;

namespace SurfaceLib
{
    public class CameraController
    {
        // Параметры камеры для системы координат Z-UP
        public Vector3 Target { get; set; } = new Vector3(0, 0, 0);   // центр сцены
        public Vector3 Up { get; set; } = new Vector3(0, 0, 1);       // ось Z вверх

        private float _yaw = 0f;
        private float _pitch = 0f;
        private float _distance = 5f;

        public float Yaw
        {
            get => _yaw;
            set
            {
                _yaw = value;
                // Нормализуем в диапазон [-2π, 2π] для предотвращения переполнения
                if (_yaw > MathF.PI * 2) _yaw -= MathF.PI * 2;
                else if (_yaw < -MathF.PI * 2) _yaw += MathF.PI * 2;
            }
        }

        public float Pitch
        {
            get => _pitch;
            set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
        }

        public float Distance
        {
            get =>  Math.Clamp(_distance, MinDistance, MaxDistance);
            set => _distance = value;
        }

        // Ограничения
        public float MinDistance { get; set; } = 0.1f;
        public float MaxDistance { get; set; } = 20.0f;
        public float MaxPitch { get; set; } = MathF.PI / 2 * 0.9f;   // почти 90°, чтобы не переворачиваться

        public Matrix4x4 GetViewMatrix()
        {
            // Вычисляем направление камеры в системе Z-up
            var direction = new Vector3(
                MathF.Cos(Yaw) * MathF.Cos(Pitch),    // X: влево/вправо
                -MathF.Sin(Yaw) * MathF.Cos(Pitch),   // Y: вперёд/назад (инвертировано)
                MathF.Sin(Pitch)                      // Z: вверх/вниз
            );

            // Позиция камеры = цель + направление * расстояние
            var position = Target + direction * Distance;

            return Matrix4x4.CreateLookAt(position, Target, Up);
        }

        public void Reset()
        {
            Yaw = 0f;
            Pitch = 0f;
            Distance = 5f;
            Console.WriteLine("[CAMERA Z-up] Сброс в начальное положение");
        }
    }
}