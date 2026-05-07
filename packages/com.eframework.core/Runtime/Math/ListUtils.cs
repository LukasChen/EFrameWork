using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.Math
{
    public static class ListUtils
    {
        public static List<T> Clone<T>(this List<T> list)
        {
            return list == null ? null : new List<T>(list);
        }

        public static List<T> DeepClone<T>(this List<T> list) where T : ICloneable
        {
            if (list == null) return null;

            List<T> clonedList = new List<T>(list.Count);
            foreach (T item in list)
            {
                clonedList.Add((T)item.Clone());
            }

            return clonedList;
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = RandomHelper.Random.Next(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        public static T GetRandomElement<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[RandomHelper.Random.Next(0, list.Count)];
        }

        public static int GetRandomIndex<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return -1;
            return RandomHelper.Random.Next(0, list.Count);
        }

        public static int GetRandomIndexExcept<T>(this IList<T> list, int exceptIndex)
        {
            if (list == null || list.Count == 0) return -1;
            if (exceptIndex < 0 || exceptIndex >= list.Count) return GetRandomIndex(list);
            if (list.Count == 1) return -1;

            int index = RandomHelper.Random.Next(0, list.Count - 1);
            if (index >= exceptIndex) index++;
            return index;
        }

        public static T GetRandomElementByProbability<T>(this IList<T> list, int[] probability)
        {
            if (list == null || probability == null || list.Count != probability.Length)
            {
                return default;
            }

            int index = RandomIndexByProbability(probability);
            return index >= 0 && index < list.Count ? list[index] : default;
        }

        public static T GetRandomElementByProbability<T>(this IList<T> list, float[] probability)
        {
            if (list == null || probability == null || list.Count != probability.Length)
            {
                return default;
            }

            int index = RandomIndexByProbability(probability);
            return index >= 0 && index < list.Count ? list[index] : default;
        }

        /// <summary>
        ///     Vector2Int权重随机选择
        ///     x为元素Id，y为权重
        /// </summary>
        /// <param name="vector2Ints"></param>
        /// <returns></returns>
        public static int GenElementByVector2Int(this List<Vector2Int> vector2Ints)
        {
            if (vector2Ints == null || vector2Ints.Count == 0)
            {
                return default;
            }

            float sum = 0, factor = 0;
            int count = vector2Ints.Count;
            for (int i = 0; i < count; i++)
            {
                if (vector2Ints[i].y > 0)
                {
                    sum += vector2Ints[i].y;
                }
            }

            if (sum <= 0)
            {
                return default;
            }

            float r = RandomHelper.NextFloat(0, sum);
            for (int i = 0; i < count; i++)
            {
                if (vector2Ints[i].y <= 0)
                {
                    continue;
                }

                factor += vector2Ints[i].y;
                if (r <= factor) return vector2Ints[i].x;
            }

            return default;
        }

        // 计算正态分布的概率密度函数 (PDF)
        public static double CalculatePdf(float x, float mean, float standardDeviation)
        {
            if (standardDeviation <= 0)
            {
                return 0;
            }

            return 1 / (System.Math.Sqrt(2 * System.Math.PI) * standardDeviation) * System.Math.Exp(-(System.Math.Pow(x - mean, 2) / (2 * System.Math.Pow(standardDeviation, 2))));
        }

        // 从列表中随机选择一个元素，根据正态分布概率
        public static T GetRandomElementByNormalDistribution<T>(this List<T> list, float mean, float std)
        {
            if (list == null || list.Count == 0 || std <= 0)
            {
                return default;
            }

            // 计算每个元素的累积概率
            int listCount = list.Count;
            double[] probabilities = new double[listCount];
            double cumulativeProb = 0f;
            for (int i = 0; i < listCount; i++)
            {
                cumulativeProb += CalculatePdf((float)i / listCount, mean, std);
                probabilities[i] = cumulativeProb;
            }

            // 生成一个随机数
            double randomValue = RandomHelper.Random.NextDouble() * cumulativeProb;

            // 找到对应的索引
            for (int i = 0; i < listCount; i++)
            {
                if (randomValue <= probabilities[i])
                    return list[i];
            }

            return default;
        }

        public static int RandomIndexByProbability(float[] probability)
        {
            if (probability == null || probability.Length == 0)
            {
                return -1;
            }

            float sum = 0, factor = 0;
            int count = probability.Length;
            for (int i = 0; i < count; i++)
            {
                if (probability[i] > 0)
                {
                    sum += probability[i];
                }
            }

            if (sum <= 0)
            {
                return -1;
            }

            float r = RandomHelper.NextFloat(0, sum);
            for (int i = 0; i < count; i++)
            {
                if (probability[i] <= 0)
                {
                    continue;
                }

                factor += probability[i];
                if (r <= factor) return i;
            }

            return -1;
        }

        public static int RandomIndexByProbability(int[] probability)
        {
            if (probability == null || probability.Length == 0)
            {
                return -1;
            }

            float sum = 0, factor = 0;
            int count = probability.Length;
            for (int i = 0; i < count; i++)
            {
                if (probability[i] > 0)
                {
                    sum += probability[i];
                }
            }

            if (sum <= 0)
            {
                return -1;
            }

            float r = RandomHelper.NextFloat(0, sum);

            for (int i = 0; i < count; i++)
            {
                if (probability[i] <= 0)
                {
                    continue;
                }

                factor += probability[i];
                if (r <= factor) return i;
            }

            return -1;
        }

        public static List<List<int>> FindContinuousNumbers(List<int> arr)
        {
            List<List<int>> result = new();
            List<int> currentGroup = new();

            for (int i = 0; i < arr.Count; i++)
            {
                // Add the current number to the current group
                currentGroup.Add(arr[i]);

                // Check if this is the last element or if the next element is not consecutive
                if (i == arr.Count - 1 || arr[i] + 1 != arr[i + 1])
                {
                    // If it's the last element or the next element is not consecutive,
                    // add the current group to the result and start a new group
                    result.Add(new List<int>(currentGroup));
                    currentGroup.Clear();
                }
            }

            return result;
        }

        public static int Sum(this IList<int> list)
        {
            int sum = 0;
            foreach (int item in list) sum += item;
            return sum;
        }

        public static float Average(this IList<int> list)
        {
            if (list == null || list.Count == 0) return 0;

            long sum = 0; // 使用 long 防止求和时溢出
            foreach (int item in list) sum += item;
            return (float)sum / list.Count;
        }

        public static IList<T> Except<T>(this IList<T> list, IList<T> otherList)
        {
            IList<T> result = new List<T>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!otherList.Contains(list[i]))
                    result.Add(list[i]);
            }

            return result;
        }

        /// <summary>
        /// 比较两个列表是否相等（顺序和内容都相同）
        /// </summary>
        public static bool IsEqual<T>(this IList<T> list, IList<T> otherList)
        {
            if (list == null && otherList == null) return true;
            if (list == null || otherList == null) return false;
            if (list.Count != otherList.Count) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(list[i], otherList[i]))
                    return false;
            }

            return true;
        }
    }
}
