using DG.Tweening;
using EFrameWork.Runtime.Audio;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFrameWork.Runtime.UI.UIHelper
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleHelper : MonoBehaviour, IPointerClickHandler
    {
        public float DisableTime = 0.3f;
        private Toggle m_Toggle;
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            m_Toggle.targetGraphic.raycastTarget = false;
            if (DisableTime > 0) Invoke("CallLater", DisableTime);
        }
        public Toggle GetToggle()
        {
            return m_Toggle;
        }

        private void CallLater()
        {
            m_Toggle.targetGraphic.raycastTarget = true;
        }
    }
}
