using EFramework.Runtime.Event;
using EFramework.Runtime.Tween;
using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.UIHelper
{
    [RequireComponent(typeof(Button))]
    public class ButtonHelper : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float DisableTime = 0.3f;
        public bool ClickOnce;
        private Button m_button;
        private RectTransform m_rectTransform;
        private Vector3 m_originalScale;
        [SerializeField] private bool m_clickAnimation = false;
        [SerializeField] private bool m_taReport;

        private string m_viewName;

        public void OnPointerClick(PointerEventData eventData)
        {
            m_button.targetGraphic.raycastTarget = false;
            if (!ClickOnce && DisableTime > 0) Invoke("CallLater", DisableTime);
            if (m_taReport) EventBus.Dispatch(new ButtonClickEvent() { UIName = m_viewName, ButtonName = gameObject.name });
        }

        private void Awake()
        {
            m_button = GetComponent<Button>();
            m_rectTransform = GetComponent<RectTransform>();
            m_viewName = "UnknownView";
            var binding = gameObject.GetComponentInParent<QUIBinding>();
            if (binding != null && binding.Config.IsViewRoot)
            {
                m_viewName = binding.gameObject.name.Replace("(Clone)", "");
            }
        }

        private void OnDestroy()
        {
            if (m_button != null)
            {
                EFrameTween.Kill(m_button.transform);
            }

            CancelInvoke();
        }

        private void CallLater()
        {
            m_button.targetGraphic.raycastTarget = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_clickAnimation)
            {
                EFrameTween.Kill(m_button.transform, true);
                m_originalScale = m_rectTransform.localScale;
                //根据按钮尺寸，缩放固定数值
                float scale = Mathf.Clamp(1 - 15f / m_rectTransform.sizeDelta.magnitude, 0.8f, 1);
                EFrameTween.Vector3(m_originalScale, m_originalScale * scale, 0.05f, value => m_button.transform.localScale = value, new EFrameTweenOptions
                {
                    Target = m_button.transform,
                    Ease = EFrameEase.OutQuad
                });
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_clickAnimation)
            {
                EFrameTween.Kill(m_button.transform);
                m_button.transform.localScale = m_originalScale;
            }
        }
    }
}
