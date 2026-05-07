using EFramework.Runtime.Event;
using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Interaction
{
    [DisallowMultipleComponent]
    public sealed class UIInteractionReporter : MonoBehaviour
    {
        private const string ClickType = "Click";
        private const string ToggleOnType = "ToggleOn";
        private const string ToggleOffType = "ToggleOff";

        [SerializeField] private Selectable m_selectable;
        [SerializeField] private bool m_reportToggleOff = true;
        [SerializeField] private string m_uiNameOverride;
        [SerializeField] private string m_elementNameOverride;

        private Button m_button;
        private Toggle m_toggle;
        private bool m_listenerAdded;

        private void Awake()
        {
            ResolveSelectable();
        }

        private void OnEnable()
        {
            ResolveSelectable();
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        private void Reset()
        {
            m_selectable = GetComponent<Selectable>();
        }

        private void OnValidate()
        {
            if (m_selectable == null)
            {
                m_selectable = GetComponent<Selectable>();
            }
        }

        private void Report(string interactionType, string value = "")
        {
            EventBus.Dispatch(new UIInteractionEvent
            {
                UIName = ResolveUIName(),
                ElementName = ResolveElementName(),
                InteractionType = interactionType,
                Value = value
            });
        }

        private string ResolveUIName()
        {
            if (!string.IsNullOrWhiteSpace(m_uiNameOverride))
            {
                return m_uiNameOverride;
            }

            var binding = GetComponentInParent<QUIBinding>();
            if (binding != null && binding.Config.IsViewRoot)
            {
                return binding.gameObject.name.Replace("(Clone)", string.Empty);
            }

            return "UnknownView";
        }

        private string ResolveElementName()
        {
            return string.IsNullOrWhiteSpace(m_elementNameOverride) ? gameObject.name : m_elementNameOverride;
        }

        private void ResolveSelectable()
        {
            if (m_selectable == null)
            {
                m_selectable = GetComponent<Selectable>();
            }

            m_button = m_selectable as Button;
            m_toggle = m_selectable as Toggle;
        }

        private void AddListeners()
        {
            if (m_listenerAdded)
            {
                return;
            }

            if (m_button != null)
            {
                m_button.onClick.AddListener(OnButtonClicked);
                m_listenerAdded = true;
            }
            else if (m_toggle != null)
            {
                m_toggle.onValueChanged.AddListener(OnToggleValueChanged);
                m_listenerAdded = true;
            }
        }

        private void RemoveListeners()
        {
            if (!m_listenerAdded)
            {
                return;
            }

            if (m_button != null)
            {
                m_button.onClick.RemoveListener(OnButtonClicked);
            }

            if (m_toggle != null)
            {
                m_toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            m_listenerAdded = false;
        }

        private void OnButtonClicked()
        {
            Report(ClickType);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            if (!isOn && !m_reportToggleOff)
            {
                return;
            }

            Report(isOn ? ToggleOnType : ToggleOffType, isOn ? "true" : "false");
        }
    }
}
