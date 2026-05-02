using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using System;

namespace EFramework.Runtime.UI.Components
{
    /// <summary>
    /// QScroller 是一个自定义的 ScrollRect，允许根据水平和垂直单位进行吸附对齐。并提供了嵌套ScrollRect的支持。
    /// 它继承自 Unity 的 ScrollRect，并使用 DOTween 实现平滑过渡。
    /// 用法：
    /// 1. 将此脚本挂载到组件上，用 QScroller 替换原有的 ScrollRect。
    /// 2. 设置 `m_snapHorizontalUnits` 和 `m_snapVerticalUnits` 以定义吸附行为,注意不能同时支持横向和纵向吸附。(设置为 2 表示三段式对齐)
    /// 3. 当用户停止拖拽或惯性减慢时会自动吸附到指定位置。
    /// 注意：请确保你的项目已安装并配置好 DOTween。配合 QScrollerEditor使用可以更方便地调整参数。
    /// Creator：Ethan.Hu 2025.6.10
    /// </summary>
    /// 
    public class QScroller : ScrollRect
    {
        [SerializeField] private int m_snapHorizontalUnits;
        [SerializeField] private int m_snapVerticalUnits;
        [SerializeField] public UnityEvent<int> OnSnapToHorizontalSegment;
        [SerializeField] public UnityEvent<int> OnSnapToVerticalSegment;
        private bool m_isDragging;
        private bool m_isSnapMoving;
        private QScroller m_parentScroller;
        private bool m_parentScrolling;

        public int snapHorizontalUnits
        {
            get => m_snapHorizontalUnits;
            set
            {
                m_snapHorizontalUnits = value;
                SnapNearestUnit();
            }
        }

        public int snapVerticalUnits
        {
            get => m_snapVerticalUnits;
            set
            {
                m_snapVerticalUnits = value;
                SnapNearestUnit();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_parentScroller = transform.parent?.GetComponentInParent<QScroller>(true);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (m_parentScroller != null)
            {
                //判定 当前拖动的方向 与当前 QScroller 的方向是否一致，如果不一致则执行父级的拖动事件
                if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y) && m_parentScroller.horizontal)
                {
                    m_parentScroller.OnBeginDrag(eventData);
                    m_parentScrolling = true;
                    return;
                }
                else if (Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x) && m_parentScroller.vertical)
                {
                    m_parentScroller.OnBeginDrag(eventData);
                    m_parentScrolling = true;
                    return;
                }
            }

            base.OnBeginDrag(eventData);
            m_parentScrolling = false;
            m_isDragging = true;
            m_isSnapMoving = false; // 开始拖动时停止任何正在进行的吸附移动
            DOTween.Kill(this);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (m_parentScrolling)
            {
                m_parentScroller.OnEndDrag(eventData);
                return;
            }
            else
            {
                base.OnEndDrag(eventData);
                m_isDragging = false;
                m_parentScrolling = false;

                // 如果没有惯性或速度极小，则立刻对齐
                if (!inertia || velocity.magnitude < 1f)
                {
                    SnapNearestUnit();
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (m_parentScrolling)
            {
                m_parentScroller.OnDrag(eventData);
                return;
            }

            base.OnDrag(eventData);
        }

        protected override void LateUpdate()
        {
            if (m_isSnapMoving) return;

            base.LateUpdate();

            // 如果当前正在回弹，则不进行吸附对齐
            if (IsBouncing())
            {
                return;
            }
            //如果开启了惯性运动,并且释放了拖动，则根据当前的速度判定吸附对齐
            if (inertia && !m_isDragging && velocity != Vector2.zero)
            {
                if (velocity.magnitude > 800f)
                {
                    //如果速度足够大，直接对齐到移动方向的下一个单位
                    if (m_snapHorizontalUnits > 0)
                    {
                        var horizontalSegmentIndex = Mathf.RoundToInt(horizontalNormalizedPosition * m_snapHorizontalUnits) + (velocity.x > 0 ? -1 : 1);

                        //   Debug.Log($"SnapToHorizontalSegmentIndex: {horizontalSegmentIndex}, horizontalNormalizedPosition: {horizontalNormalizedPosition}, velocity.x: {velocity.x}");
                        SnapToHorizontalSegmentIndex(horizontalSegmentIndex);
                    }
                    else if (m_snapVerticalUnits > 0)
                    {
                        var verticalSegmentIndex = Mathf.RoundToInt(verticalNormalizedPosition * m_snapVerticalUnits) + (velocity.y > 0 ? -1 : 1);
                        SnapToVerticalSegmentIndex(verticalSegmentIndex);
                    }
                }
                else
                {
                    //对齐到当前单位
                    SnapNearestUnit();
                }
            }
        }

        // 判断是否超出边界
        private bool IsOutOfBounds()
        {
            var contentRect = content.rect;
            var viewportRect = viewport.rect;
            // 水平
            if (horizontal)
            {
                if (content.anchoredPosition.x > 0 || contentRect.width + content.anchoredPosition.x < viewportRect.width)
                    return true;
            }
            // 垂直
            if (vertical)
            {
                if (content.anchoredPosition.y < 0 || contentRect.height - content.anchoredPosition.y < viewportRect.height)
                    return true;
            }
            return false;
        }
        // 判断是否正在回弹
        private bool IsBouncing()
        {
            return velocity != Vector2.zero
                && movementType == MovementType.Elastic
                && IsOutOfBounds();
        }

        private void SnapNearestUnit()
        {
            if (m_snapHorizontalUnits > 0)
            {
                var horizontalSegmentIndex = Mathf.RoundToInt(horizontalNormalizedPosition * m_snapHorizontalUnits);
                SnapToHorizontalSegmentIndex(horizontalSegmentIndex);
            }
            else if (m_snapVerticalUnits > 0)
            {
                var verticalSegmentIndex = Mathf.RoundToInt(verticalNormalizedPosition * m_snapVerticalUnits);
                SnapToVerticalSegmentIndex(verticalSegmentIndex);
            }
        }

        private void SpanTo(Vector2 position)
        {
            velocity = Vector2.zero;
            m_isSnapMoving = true;
            DOTween.Kill(this);
            DOTween.To(() => normalizedPosition, v => normalizedPosition = v, position, 0.15f)
            .SetTarget(this).OnUpdate(() =>
            {
                onValueChanged?.Invoke(normalizedPosition);
            }).OnComplete(() =>
            {
                m_isSnapMoving = false;
            });
        }

        public void SnapToHorizontalSegmentIndex(int index)
        {
            if (m_snapHorizontalUnits <= 0) return;
            index = Mathf.Clamp(index, 0, m_snapHorizontalUnits);
            // 计算目标位置
            float targetX = (float)index / m_snapHorizontalUnits;

            // Debug.Log($"SnapToHorizontalSegmentIndex: {index}, targetX: {targetX}");
            SpanTo(new Vector2(targetX, verticalNormalizedPosition));
            OnSnapToHorizontalSegment?.Invoke(index);
        }

        public void SnapToVerticalSegmentIndex(int index)
        {
            if (m_snapVerticalUnits <= 0) return;
            index = Mathf.Clamp(index, 0, m_snapVerticalUnits);
            // 计算目标位置
            float targetY = (float)index / m_snapVerticalUnits;
            // Debug.Log($"SnapToVerticalSegmentIndex: {index}, targetY: {targetY}");
            SpanTo(new Vector2(horizontalNormalizedPosition, targetY));
            OnSnapToVerticalSegment?.Invoke(index);
        }
    }
}