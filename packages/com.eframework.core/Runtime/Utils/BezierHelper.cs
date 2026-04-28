using System.Collections.Generic;
using UnityEngine;

namespace EFrameWork.Runtime.Utils
{
    public static class BezierHelper
    {
        public static List<Vector3> GetBezierCurve(Vector3 startPoint, Vector3 endPoint, Vector3 controlPoint,
            int numberOfPoints)
        {
            List<Vector3> result = new();
            result.Add(startPoint);
            for (int i = 0; i <= numberOfPoints; i++)
            {
                float t = i / (float)numberOfPoints;
                Vector3 position = CalculateLinearBezierPoint(t, startPoint, controlPoint, endPoint);
                result.Add(position);
            }

            result.Add(endPoint);
            return result;
        }

        public static Vector3 CalculateLinearBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return (1 - t) * ((1 - t) * p0 + t * p1) + t * ((1 - t) * p1 + t * p2);
        }

        public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0; // (1-t)^2 * p0
            p += 2 * u * t * p1; // 2 * (1-t) * t * p1
            p += tt * p2;        // t^2 * p2

            return p;
        }

        public static float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, int segmentCount = 100)
        {
            float length = 0f;
            Vector3 previousPoint = p0;

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 currentPoint = CalculateBezierPoint(t, p0, p1, p2);
                length += Vector3.Distance(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            return length;
        }

        public static List<float> CalculateArcLengthTable(Vector3 p0, Vector3 p1, Vector3 p2, int segmentCount = 100)
        {
            List<float> arcLengthTable = new();
            float length = 0f;
            Vector3 previousPoint = p0;
            arcLengthTable.Add(0f);

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 currentPoint = CalculateBezierPoint(t, p0, p1, p2);
                length += Vector3.Distance(previousPoint, currentPoint);
                arcLengthTable.Add(length);
                previousPoint = currentPoint;
            }

            return arcLengthTable;
        }

        public static float GetTForArcLength(float arcLength, List<float> arcLengthTable)
        {
            float totalLength = arcLengthTable[arcLengthTable.Count - 1];
            float targetLength = arcLength * totalLength;

            for (int i = 1; i < arcLengthTable.Count; i++)
            {
                if (arcLengthTable[i] >= targetLength)
                {
                    float t1 = (i - 1) / (float)(arcLengthTable.Count - 1);
                    float t2 = i / (float)(arcLengthTable.Count - 1);
                    float l1 = arcLengthTable[i - 1];
                    float l2 = arcLengthTable[i];
                    return t1 + (targetLength - l1) / (l2 - l1) * (t2 - t1);
                }
            }

            return 1f;
        }

        /// <summary>
        ///     计算贝塞尔曲线长度
        /// </summary>
        /// <param name="p0">起始点</param>
        /// <param name="p1">终点</param>
        /// <param name="p2">控制点1</param>
        /// <param name="p3">控制点2</param>
        /// <param name="segmentCount"></param>
        /// <returns></returns>
        public static float CalculateCubicBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segmentCount = 100)
        {
            float length = 0f;
            Vector3 previousPoint = p0;

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 currentPoint = CalculateCubicBezierPoint(t, p0, p1, p2, p3);
                length += Vector3.Distance(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            return length;
        }

        public static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float ttt = tt * t;
            float uuu = uu * u;

            Vector3 p = uuu * p0; // (1-t)^3 * p0
            p += 3 * uu * t * p1; // 3 * (1-t)^2 * t * p1
            p += 3 * u * tt * p2; // 3 * (1-t) * t^2 * p2
            p += ttt * p3;        // t^3 * p3

            return p;
        }

        public static List<float> CalculateCubicArcLengthTable(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segmentCount = 100)
        {
            List<float> arcLengthTable = new();
            float length = 0f;
            Vector3 previousPoint = p0;
            arcLengthTable.Add(0f);

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 currentPoint = CalculateCubicBezierPoint(t, p0, p1, p2, p3);
                length += Vector3.Distance(previousPoint, currentPoint);
                arcLengthTable.Add(length);
                previousPoint = currentPoint;
            }

            return arcLengthTable;
        }
    }
}
