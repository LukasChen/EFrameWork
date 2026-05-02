using DG.Tweening;
using EFrame.Runtime.Audio;
using EFrame.Runtime.Event;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFrame.Runtime.UI.UIHelper
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
                m_viewName =  binding.gameObject.name.Replace("(Clone)", "");
            }
        }


        private void OnDestroy()
        {
            m_button.transform.DOKill();
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
                m_button.transform.DOKill(true);
                m_originalScale = m_rectTransform.localScale;
                //根据按钮尺寸，缩放固定数值   
                float scale = Mathf.Clamp(1 - 15f / m_rectTransform.sizeDelta.magnitude, 0.8f, 1);
                m_button.transform.DOScale(m_originalScale * scale, 0.05f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_clickAnimation)
            {
                m_button.transform.DOKill();
                m_button.transform.localScale = m_originalScale;
            }
        }
    }
}
