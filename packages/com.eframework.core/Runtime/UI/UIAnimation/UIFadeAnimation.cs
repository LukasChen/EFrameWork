using DG.Tweening;
using UnityEngine;

namespace EFrame.Runtime.UI.UIAnimation
{
    public class UIFadeAnimation : MonoBehaviour
    {
        [SerializeField] private float m_openDuration = 0.25f;
        private CanvasGroup m_canvasGroup;
        void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        void OnEnable()
        {
            m_canvasGroup.DOKill(true);
            m_canvasGroup.DOFade(1f, m_openDuration).From(0).SetLink(gameObject);
        }

        void OnDisable()
        {
            m_canvasGroup.DOKill(true);
        }

        private void OnDestroy()
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.DOKill();
            }
        }
    }
}
