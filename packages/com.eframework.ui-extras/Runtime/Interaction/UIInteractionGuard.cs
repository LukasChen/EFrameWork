using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Interaction
{
    public enum UIInteractionGuardMode
    {
        Cooldown,
        Once,
        Manual
    }

    [DisallowMultipleComponent]
    public sealed class UIInteractionGuard : MonoBehaviour
    {
        [SerializeField] private Selectable m_selectable;
        [SerializeField] private UIInteractionGuardMode m_mode = UIInteractionGuardMode.Cooldown;
        [SerializeField] private float m_cooldownSeconds = 0.3f;
        [SerializeField] private bool m_restoreOnDisable = true;

        private Button m_button;
        private Toggle m_toggle;
        private bool m_listenerAdded;
        private bool m_locked;

        public Selectable Selectable => m_selectable;
        public bool IsLocked => m_locked;

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
            CancelInvoke(nameof(Unlock));

            if (m_restoreOnDisable)
            {
                Unlock();
            }
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

            m_cooldownSeconds = Mathf.Max(0f, m_cooldownSeconds);
        }

        public void Lock()
        {
            if (m_locked)
            {
                return;
            }

            m_locked = true;
            if (m_selectable != null)
            {
                m_selectable.interactable = false;
            }
        }

        public void Unlock()
        {
            m_locked = false;
            if (m_selectable != null)
            {
                m_selectable.interactable = true;
            }
        }

        public void ResetGuard()
        {
            Unlock();
        }

        private void OnInteractionTriggered()
        {
            if (m_mode == UIInteractionGuardMode.Manual || m_locked)
            {
                return;
            }

            Lock();
            if (m_mode == UIInteractionGuardMode.Cooldown && m_cooldownSeconds > 0f)
            {
                Invoke(nameof(Unlock), m_cooldownSeconds);
            }
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
                m_button.onClick.AddListener(OnInteractionTriggered);
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
                m_button.onClick.RemoveListener(OnInteractionTriggered);
            }

            if (m_toggle != null)
            {
                m_toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            m_listenerAdded = false;
        }

        private void OnToggleValueChanged(bool _)
        {
            OnInteractionTriggered();
        }
    }
}
