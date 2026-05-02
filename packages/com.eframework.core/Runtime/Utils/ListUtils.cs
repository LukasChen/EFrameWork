using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.Utils
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
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = RandomHelper.Random.Next(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        public static T GetRandomElement<T>(this IList<T> list)
        {
            if (list.Count == 0) return default;
            return list[RandomHelper.Random.Next(0, list.Count)];
        }

        public static int GetRandomIndex<T>(this IList<T> list)
        {
            if (list.Count == 0) return -1;
            return RandomHelper.Random.Next(0, list.Count);
        }

        public static int GetRandomIndexExcept<T>(this IList<T> list, int exceptIndex)
        {
            int index = RandomHelper.Random.Next(0, list.Count - 1);
            if (index >= exceptIndex) index++;
            return index;
        }

        public static T GetRandomElementByProbability<T>(this IList<T> list, int[] probability)
        {
            return list[RandomIndexByProbability(probability)];
        }

        public static T GetRandomElementByProbability<T>(this IList<T> list, float[] probability)
        {
            return list[RandomIndexByProbability(probability)];
        }

        /// <summary>
        ///     Vector2Int权重随机选择
        ///     x为元素Id，y为权重
        /// </summary>
        /// <param name="vector2Ints"></param>
        /// <returns></returns>
        public static int GenElementByVector2Int(this List<Vector2Int> vector2Ints)
        {
            float sum = 0, factor = 0;
            int count = vector2Ints.Count;
            for (int i = 0; i < count; i++)
            {
                sum += vector2Ints[i].y;
            }

            float r = RandomHelper.NextFloat(0, sum);
            for (int i = 0; i < count; i++)
            {
                factor += vector2Ints[i].y;
                if (r <= factor) return vector2Ints[i].x;
            }

            return vector2Ints[0].x;
        }

        // 计算正态分布的概率密度函数 (PDF)
        public static double CalculatePdf(float x, float mean, float standardDeviation)
        {
            return 1 / (Math.Sqrt(2 * Math.PI) * standardDeviation) * Math.Exp(-(Math.Pow(x - mean, 2) / (2 * Math.Pow(standardDeviation, 2))));
        }

        // 从列表中随机选择一个元素，根据正态分布概率
        public static T GetRandomElementByNormalDistribution<T>(this List<T> list, float mean, float std)
        {
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
            float sum = 0, factor = 0;
            int count = probability.Length;
            for (int i = 0; i < count; i++)
            {
                sum += probability[i];
            }

            float r = RandomHelper.NextFloat(0, sum);
            for (int i = 0; i < count; i++)
            {
                factor += probability[i];
                if (r <= factor) return i;
            }

            return 0;
        }

        public static int RandomIndexByProbability(int[] probability)
        {
            float sum = 0, factor = 0;
            int count = probability.Length;
            for (int i = 0; i < count; i++)
            {
                sum += probability[i];
            }

            float r = RandomHelper.NextFloat(0, sum);

            for (int i = 0; i < count; i++)
            {
                factor += probability[i];
                if (r <= factor) return i;
            }

            return 0;
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
