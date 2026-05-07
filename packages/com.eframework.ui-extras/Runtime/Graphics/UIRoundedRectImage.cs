using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Graphics
{
    [RequireComponent(typeof(Image))]
    public sealed class UIRoundedRectImage : BaseMeshEffect
    {
        [SerializeField, Min(0f)] private float m_radius = 24f;
        [SerializeField, Range(2, 12)] private int m_cornerSegments = 6;

        public float Radius
        {
            get => m_radius;
            set
            {
                m_radius = Mathf.Max(0f, value);
                graphic?.SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => m_cornerSegments;
            set
            {
                m_cornerSegments = Mathf.Clamp(value, 2, 12);
                graphic?.SetVerticesDirty();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            m_radius = Mathf.Max(0f, m_radius);
            m_cornerSegments = Mathf.Clamp(m_cornerSegments, 2, 12);
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
            {
                return;
            }

            Rect rect = graphic.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            UIVertex source = new UIVertex();
            vh.PopulateUIVertex(ref source, 0);
            vh.Clear();

            float radius = Mathf.Min(m_radius, rect.width * 0.5f, rect.height * 0.5f);
            if (radius <= 0.01f)
            {
                AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, rect, source);
                return;
            }

            List<Vector2> points = new List<Vector2>((m_cornerSegments + 1) * 4);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

            int centerIndex = AddVertex(vh, rect.center, rect, source);
            for (int i = 0; i < points.Count; i++)
            {
                AddVertex(vh, points[i], rect, source);
            }

            for (int i = 0; i < points.Count; i++)
            {
                int next = i == points.Count - 1 ? 0 : i + 1;
                vh.AddTriangle(centerIndex, i + 1, next + 1);
            }
        }

        private void AddCorner(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle)
        {
            for (int i = 0; i <= m_cornerSegments; i++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, i / (float)m_cornerSegments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Rect rect, UIVertex source)
        {
            int start = vh.currentVertCount;
            AddVertex(vh, new Vector2(xMin, yMin), rect, source);
            AddVertex(vh, new Vector2(xMin, yMax), rect, source);
            AddVertex(vh, new Vector2(xMax, yMax), rect, source);
            AddVertex(vh, new Vector2(xMax, yMin), rect, source);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private static int AddVertex(VertexHelper vh, Vector2 position, Rect rect, UIVertex source)
        {
            source.position = position;
            source.uv0 = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, position.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, position.y));
            vh.AddVert(source);
            return vh.currentVertCount - 1;
        }
    }
}
