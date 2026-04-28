using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssociaireSort.UI.Components
{
    /// <summary>
    /// 按钮图标辅助脚本，实现按下时图标下移的效果
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonIconHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Image m_iconImage;
        [SerializeField] TextMeshProUGUI m_iconText;
        [SerializeField] Button m_button;
        [SerializeField] private float m_pressOffsetY = 5f;
        private Vector2 m_originalIconPos;
        private Vector2 m_originalTextPos;
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_button == null || !m_button.interactable || !m_button.targetGraphic.raycastTarget)
            {
                return;
            }
            // 按下时，图标稍微下移，模拟按下效果
            if (m_iconImage != null)
            {
                m_iconImage.rectTransform.anchoredPosition = m_originalIconPos + new Vector2(0, -m_pressOffsetY);
            }
            if (m_iconText != null)
            {
                m_iconText.rectTransform.anchoredPosition = m_originalTextPos + new Vector2(0, -m_pressOffsetY);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_button == null || !m_button.interactable || !m_button.targetGraphic.raycastTarget)
            {
                return;
            }
            // 抬起时，图标恢复原位
            if (m_iconImage != null)
            {
                m_iconImage.rectTransform.anchoredPosition = m_originalIconPos;
            }
            if (m_iconText != null)
            {
                m_iconText.rectTransform.anchoredPosition = m_originalTextPos;
            }
        }

        private void Awake()
        {
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }

            if (m_iconImage != null)
            {
                m_originalIconPos = m_iconImage.rectTransform.anchoredPosition;
            }
            if (m_iconText != null)
            {
                m_originalTextPos = m_iconText.rectTransform.anchoredPosition;
            }
        }

        void Reset()
        {
            m_button = GetComponent<Button>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("icon") && child.GetComponent<Image>() != null)
                {
                    m_iconImage = child.GetComponent<Image>();
                    break;
                }
            }

            m_iconText = transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (m_iconText == null)
            {
                m_iconText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }
}
