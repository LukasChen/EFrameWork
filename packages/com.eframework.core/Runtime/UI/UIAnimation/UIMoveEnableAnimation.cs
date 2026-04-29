using DG.Tweening;
using UnityEngine;

namespace EFrameWork.Runtime.UI.UIAnimation
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
            Vector2 startPos = m_rectTransform.anchoredPosition + m_moveOffset;
            m_rectTransform.DOKill(true);
            m_rectTransform.anchoredPosition = startPos;
            m_rectTransform.DOAnchorPos(m_rectTransform.anchoredPosition - m_moveOffset, m_openDuration)
                .SetEase(Ease.OutCubic);
        }
        private void OnDestroy()
        {
            m_rectTransform.DOKill();
        }

        void Reset()
        {
            m_rectTransform = this.GetComponent<RectTransform>();
        }
    }
}
