using EFramework.Runtime.Tween;
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

        private void OnDestroy()
        {
            if (m_grabIconImage != null)
            {
                EFrameTween.Kill(m_grabIconImage.rectTransform);
            }

            if (m_checkMarkImage != null)
            {
                EFrameTween.Kill(m_checkMarkImage);
            }

            if (m_OnText != null)
            {
                EFrameTween.Kill(m_OnText);
            }

            if (m_OffText != null)
            {
                EFrameTween.Kill(m_OffText);
            }
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
                    var rectTransform = m_grabIconImage.rectTransform;
                    var startPos = rectTransform.anchoredPosition;
                    var endPos = new Vector2(targetPos, startPos.y);
                    EFrameTween.Kill(rectTransform, true);
                    EFrameTween.Vector2(startPos, endPos, 0.2f, value => rectTransform.anchoredPosition = value, new EFrameTweenOptions
                    {
                        Target = rectTransform,
                        Ease = EFrameEase.OutQuad
                    });
                }

                if (m_checkMarkImage != null)
                {
                    FadeGraphic(m_checkMarkImage, isOn ? 1f : 0f, 0.2f);
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
                FadeGraphic(m_OnText, isOn ? 1f : 0f, 0.2f);
            }

            if (m_OffText != null)
            {
                FadeGraphic(m_OffText, isOn ? 0f : 1f, 0.2f);
            }
        }

        private static void FadeGraphic(Graphic graphic, float targetAlpha, float duration)
        {
            EFrameTween.Kill(graphic, true);
            var startColor = graphic.color;
            var targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            EFrameTween.Color(startColor, targetColor, duration, value => graphic.color = value, new EFrameTweenOptions
            {
                Target = graphic,
                Ease = EFrameEase.Linear
            });
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
