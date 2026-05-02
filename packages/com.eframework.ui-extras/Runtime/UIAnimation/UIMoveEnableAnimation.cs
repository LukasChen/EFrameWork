using EFramework.Runtime.Tween;
using UnityEngine;

namespace EFramework.Extensions.UI.Extras.UIAnimation
{
    [RequireComponent(typeof(RectTransform))]
    public class UIMoveEnableAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform m_rectTransform;
        [SerializeField] private float m_openDuration = 0.15f;
        [SerializeField] private Vector2 m_moveOffset = new Vector2(0f, 20f);

        void OnEnable()
        {
            if (!this.gameObject.activeSelf) this.gameObject.SetActive(true);

            if (m_rectTransform == null)
            {
                m_rectTransform = this.GetComponent<RectTransform>();
            }

            var targetPos = m_rectTransform.anchoredPosition;
            var startPos = targetPos + m_moveOffset;
            EFrameTween.Kill(m_rectTransform, true);
            m_rectTransform.anchoredPosition = startPos;
            EFrameTween.Vector2(startPos, targetPos, m_openDuration, value => m_rectTransform.anchoredPosition = value, new EFrameTweenOptions
            {
                Target = m_rectTransform,
                Ease = EFrameEase.OutQuad
            });
        }

        private void OnDestroy()
        {
            if (m_rectTransform != null)
            {
                EFrameTween.Kill(m_rectTransform);
            }
        }

        void Reset()
        {
            m_rectTransform = this.GetComponent<RectTransform>();
        }
    }
}
