using System;
using System.Numerics;

namespace SurfaceLib
{
    /// <summary>
    /// Класс управления камерой
    /// Предоставляет интерфейсы для настройки положения камеры
    /// Формирует матрицу вида
    /// </summary>
    public class CameraController
    {
        /// <summary>
        /// Центр сцены, на него направлена камера
        /// </summary>
        public Vector3 Target { get; set; } = new Vector3(0, 0, 0);

        /// <summary>
        /// Направление оси Z, она задает направление "вверх"
        /// </summary>
        public Vector3 Up { get; set; } = new Vector3(0, 0, 1);

        private float _yaw = 0f;
        private float _pitch = 0f;
        private float _distance = 5f;
        private float _absolute_distance = 1f;

        /// <summary>
        /// Порот вокруг оси Z
        /// Вращает поверхность вокруг цетнтральной вертикальной оси
        /// </summary>
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

        /// <summary>
        /// Поворот вокруг оси X.
        /// Наклоняет поверхность относительно горизонта.
        /// </summary>
        public float Pitch
        {
            get => _pitch;
            set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
        }

        /// <summary>
        /// Расстояние от камеры до центра (относительное)
        /// </summary>
        public float Distance
        {
            get =>  Math.Clamp(_distance, MinDistance, MaxDistance);
            set => _distance = value;
        }

        /// <summary>
        /// Соотвествует длине диагонали трехмерной сцены.
        /// Расстояние от камеры до цетра сцены определяется как AbsoluteDistance * Distance.
        /// </summary>
        public float AbsoluteDistance
        {
            get => _absolute_distance;
            set => _absolute_distance = value;
        }

        // Ограничения
        public float MinDistance { get; set; } = 0.1f;
        public float MaxDistance { get; set; } = 5.0f;
        public float MaxPitch { get; set; } = MathF.PI / 2 * 0.9f;   // почти 90°, чтобы не переворачиваться

        /// <summary>
        /// Вычисляет видовую матрицу
        /// </summary>
        /// <returns>Видовая матрица</returns>
        public Matrix4x4 GetViewMatrix()
        {
            // Вычисляем направление камеры в системе Z-up
            var direction = new Vector3(
                MathF.Cos(Yaw) * MathF.Cos(Pitch),    // X: влево/вправо
                -MathF.Sin(Yaw) * MathF.Cos(Pitch),   // Y: вперёд/назад (инвертировано)
                MathF.Sin(Pitch)                      // Z: вверх/вниз
            );

            // Позиция камеры = цель + направление * расстояние
            var position = Target + direction * Distance * AbsoluteDistance;

            return Matrix4x4.CreateLookAt(position, Target, Up);
        }

        /// <summary>
        /// Сброс положения камеры
        /// </summary>
        public void Reset()
        {
            Yaw = 0f;
            Pitch = 0f;
            Distance = 5f;
        }
    }
}