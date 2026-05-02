using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace EFramework.Runtime.Utils
{
    public static class RandomHelper
    {
        public static int RandomSeed;
        public static Random Random = new();

        public static void InitSeed()
        {
            InitSeed((int)DateTime.Now.Ticks);
        }

        public static void InitSeed(int seed)
        {
            RandomSeed = seed;
            Random = new Random(RandomSeed);
        }

        public static int Next(int min, int max)
        {
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
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
            return mean + std * randStdNormal;                                                    // random normal(mean,stdDev^2)
        }


        // 根据Count 生成更为均匀 的随机数。符合正态分布
        public static List<int> RandomIndexByNormalDistribution(List<double> weights, int count)
        {
            // Convert weights to double and normalize
            double sum = weights.Sum();
            var wtp = weights.Select(x => x / sum).ToList();

            List<int> result = new();
            var p = wtp.Select(x => NextNormal(1.0 / x, 1.0 / x / 3.0)).ToList();

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
            return UnityEngine.Random.insideUnitCircle * radius;
        }

        //在一个椭圆形范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInEllipse(float radiusX, float radiusY)
        {
            float t = 2f * UnityEngine.Mathf.PI * UnityEngine.Random.value;
            float u = UnityEngine.Random.value + UnityEngine.Random.value;
            float r = (u > 1) ? 2 - u : u;
            return new UnityEngine.Vector3(r * UnityEngine.Mathf.Cos(t) * radiusX, r * UnityEngine.Mathf.Sin(t) * radiusY, 0);
        }

        //在一个球形范围内获取随机点
        public static UnityEngine.Vector3 RandomPointInSphere(float radius)
        {
            return UnityEngine.Random.insideUnitSphere * radius;
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
