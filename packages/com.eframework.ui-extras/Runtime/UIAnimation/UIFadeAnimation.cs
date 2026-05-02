using EFramework.Runtime.Tween;
using UnityEngine;

namespace EFramework.Extensions.UI.Extras.UIAnimation
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
            EFrameTween.Kill(m_canvasGroup, true);
            m_canvasGroup.alpha = 0f;
            EFrameTween.Float(0f, 1f, m_openDuration, value => m_canvasGroup.alpha = value, new EFrameTweenOptions
            {
                Target = m_canvasGroup,
                Ease = EFrameEase.Linear
            });
        }

        void OnDisable()
        {
            if (m_canvasGroup != null)
            {
                EFrameTween.Kill(m_canvasGroup, true);
            }
        }

        private void OnDestroy()
        {
            if (m_canvasGroup != null)
            {
                EFrameTween.Kill(m_canvasGroup);
            }
        }
    }
}
