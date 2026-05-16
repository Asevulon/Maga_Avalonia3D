using System.Numerics;

namespace ModelStub
{
    public class Surfaces
    {
        static private List<Vector3> PointGen(int pointCount, Vector2 min, Vector2 max, Func<float,float,float> functor)
        {
            var points = new List<Vector3>();
            Random random = new Random();

            float xRange = max.X - min.X;
            float yRange = max.Y - min.Y;

            for (int i = 0; i < pointCount; i++)
            {
                float x = (float)(random.NextDouble() * xRange + min.X);
                float y = (float)(random.NextDouble() * yRange + min.Y);

                float z = functor(x, y);

                if (float.IsNaN(z)) continue;

                points.Add(new Vector3(x, y, z));
            }

            return points;
        }

        static public List<Vector3> Gaussian(int pointCount, Vector2 min, Vector2 max, float variance, float expected)
        {
            float c2 = 2 * variance * variance;

            return PointGen(pointCount, min, max, (x, y) => {
                float distanceSquared = (x - expected) * (x - expected) + (y - expected) * (y - expected);
                return (float)Math.Exp(-distanceSquared / c2);
            });
        }
        static public List<Vector3> Gaussian(int pointCount) =>
            Gaussian(pointCount, new Vector2(-2, -2), new Vector2(2, 2), 1f, 0);

        static public List<Vector3> Saddle(int pointCount, Vector2 min, Vector2 max)
        {
            return PointGen(pointCount, min, max, (x, y) => x * x - y * y);
        }
        static public List<Vector3> Saddle(int pointCount) => 
            Saddle(pointCount, new Vector2(-2, -2), new Vector2(2, 2));

        static public List<Vector3> Ripple(int pointCount, Vector2 min, Vector2 max, float freq, float amp)
        {
            return PointGen(pointCount, min, max, (x, y) => {
                float r = MathF.Sqrt(x * x + y * y);
                if (r < 1e-6f) return amp;
                return amp * MathF.Sin(freq * r) / (freq * r);
            });
        }
        static public List<Vector3> Ripple(int pointCount) =>
            Ripple(pointCount, new Vector2(-5, -5), new Vector2(5, 5), 3.0f, 1.5f);
    }
}
