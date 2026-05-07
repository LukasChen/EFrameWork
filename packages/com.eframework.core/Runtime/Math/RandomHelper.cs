using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace EFramework.Runtime.Math
{
    public static class RandomHelper
    {
        public static int RandomSeed;
        public static Random Random = new();

        public static void InitSeed()
        {
            InitSeed(unchecked((int)DateTime.UtcNow.Ticks));
        }

        public static void InitSeed(int seed)
        {
            RandomSeed = seed;
            Random = new Random(RandomSeed);
        }

        public static int Next(int min, int max)
        {
            if (min >= max)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max must be greater than min.");
            }

            return Random.Next(min, max);
        }

        /// <summary>
        ///     随机整数
        /// </summary>
        /// <param name="min">最小值,包括在内</param>
        /// <param name="max">最大值,包括在内</param>
        /// <returns></returns>
        public static int NextRange(int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max must be greater than or equal to min.");
            }

            if (max == int.MaxValue)
            {
                long range = (long)max - min + 1;
                return (int)System.Math.Floor(min + Random.NextDouble() * range);
            }

            return Random.Next(min, max + 1);
        }

        public static float NextFloat()
        {
            return (float)Random.NextDouble();
        }

        public static float NextFloat(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        public static double NextNormal(double mean, double std)
        {
            // 使用 Box-Muller 变换生成正态分布的随机数
            double u1 = 1.0 - Random.NextDouble(); // uniform(0,1] random doubles
            double u2 = 1.0 - Random.NextDouble();
            double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Sin(2.0 * System.Math.PI * u2); // random normal(0,1)
            return mean + std * randStdNormal;                                                    // random normal(mean,stdDev^2)
        }


        // 根据Count 生成更为均匀 的随机数。符合正态分布
        public static List<int> RandomIndexByNormalDistribution(List<double> weights, int count)
        {
            List<int> result = new();
            if (weights == null || weights.Count == 0 || count <= 0)
            {
                return result;
            }

            // Convert weights to double and normalize
            double sum = weights.Where(weight => weight > 0).Sum();
            if (sum <= 0)
            {
                return result;
            }

            var wtp = weights.Select(x => x > 0 ? x / sum : 0).ToList();

            var p = wtp.Select(x => x > 0 ? NextNormal(1.0 / x, 1.0 / x / 3.0) : double.PositiveInfinity).ToList();

            for (int i = 0; i < count; i++)
            {
                int minj = p.IndexOf(p.Min());
                result.Add(minj);

                double minp = p[minj];
                p = p.Select(x => x - minp).ToList();
                p[minj] = NextNormal(1.0 / wtp[minj], 1.0 / wtp[minj] / 3.0);
            }

            return result;
        }

        //在一个圆形范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInCircle(float radius)
        {
            float angle = NextFloat(0f, UnityEngine.Mathf.PI * 2f);
            float distance = UnityEngine.Mathf.Sqrt(NextFloat()) * radius;
            return new UnityEngine.Vector3(UnityEngine.Mathf.Cos(angle) * distance, UnityEngine.Mathf.Sin(angle) * distance, 0f);
        }

        //在一个椭圆形范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInEllipse(float radiusX, float radiusY)
        {
            float angle = NextFloat(0f, UnityEngine.Mathf.PI * 2f);
            float distance = UnityEngine.Mathf.Sqrt(NextFloat());
            return new UnityEngine.Vector3(distance * UnityEngine.Mathf.Cos(angle) * radiusX, distance * UnityEngine.Mathf.Sin(angle) * radiusY, 0f);
        }

        //在一个球形范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInSphere(float radius)
        {
            for (int i = 0; i < 32; i++)
            {
                var point = new UnityEngine.Vector3(
                    NextFloat(-1f, 1f),
                    NextFloat(-1f, 1f),
                    NextFloat(-1f, 1f));

                if (point.sqrMagnitude <= 1f)
                {
                    return point * radius;
                }
            }

            return UnityEngine.Vector3.zero;
        }

        //在水平方向一定范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInRangeX(float range)
        {
            float x = NextFloat(-range, range);
            return new UnityEngine.Vector3(x, 0, 0);
        }

        //在垂直方向一定范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInRangeY(float range)
        {
            float y = NextFloat(-range, range);
            return new UnityEngine.Vector3(0, y, 0);
        }

        //在 XY 平面内一定范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInRangeXY(float rangeX, float rangeY)
        {
            float x = NextFloat(-rangeX, rangeX);
            float y = NextFloat(-rangeY, rangeY);
            return new UnityEngine.Vector3(x, y, 0);
        }
    }
}
