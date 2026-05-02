using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.UIHelper
{
    /// <summary>
    /// Helper component to manage toggle icon animations and states.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ToggleIconHelper : MonoBehaviour
    {
        [SerializeField] private Image m_grabIconImage;
        [SerializeField] private Toggle m_toggle;
        [SerializeField] private float m_moveAmount = 48f;
        [SerializeField] private Image m_checkMarkImage;
        [SerializeField] private TextMeshProUGUI m_OnText;
        [SerializeField] private TextMeshProUGUI m_OffText;
        public Toggle Toggle => m_toggle;
        void Awake()
        {
            m_toggle?.onValueChanged.AddListener(OnToggleValueChanged);
            UpdateState(m_toggle.isOn, false);
        }

        public void SetIsOnWithoutNotify(bool isOn)
        {
            m_toggle.SetIsOnWithoutNotify(isOn);
            UpdateState(isOn, false);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            UpdateState(isOn, true);
        }

        private void UpdateState(bool isOn, bool animated)
        {
            if (animated)
            {
                if (m_grabIconImage != null)
                {
                    var targetPos = isOn ? m_moveAmount : -m_moveAmount;
                    m_grabIconImage.DOKill(true);
                    m_grabIconImage.rectTransform.DOAnchorPosX(targetPos, 0.2f).SetEase(Ease.OutQuad);
                }
                if (m_checkMarkImage != null)
                {
                    m_checkMarkImage.DOKill(true);
                    m_checkMarkImage.DOFade(isOn ? 1f : 0f, 0.2f);
                }
            }
            else
            {
                if (m_grabIconImage != null)
                {
                    var targetPos = isOn ? m_moveAmount : -m_moveAmount;
                    m_grabIconImage.rectTransform.anchoredPosition = new Vector2(targetPos, 0f);
                }
                if (m_checkMarkImage != null)
                {
                    m_checkMarkImage.color = new Color(1f, 1f, 1f, isOn ? 1f : 0f);
                }
            }
            
            if (m_OnText != null)
            {
                m_OnText.DOKill(true);
                m_OnText.DOFade(isOn ? 1f : 0f, 0.2f);
            }

            if (m_OffText != null) 
            {
                m_OffText.DOKill(true);
                m_OffText.DOFade(isOn ? 0f : 1f, 0.2f);
            }
        }

        void Reset()
        {
            m_toggle = GetComponent<Toggle>();
            m_checkMarkImage = transform.Find("Check")?.GetComponent<Image>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("grab") && child.GetComponent<Image>() != null)
                {
                    m_grabIconImage = child.GetComponent<Image>();
                    break;
                }
            }
        }
    }
}