using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Components
{
    [DisallowMultipleComponent]
    public class StateButton : MonoBehaviour
    {
        [Tooltip("UGUI Button variant shown while this component is not selected.")]
        [SerializeField] private Button m_normalButton;
        [Tooltip("UGUI Button variant shown while this component is selected.")]
        [SerializeField] private Button m_selectedButton;
        [SerializeField] private bool m_selected = false;
        [SerializeField] public UnityEvent OnClick = new();
        [SerializeField] public UnityEvent<bool> OnStateChanged = new();

        private bool m_listenersAdded;

        public Button NormalButton => m_normalButton;
        public Button SelectedButton => m_selectedButton;
        public Button Button => CurrentButton;
        public Button CurrentButton => GetCurrentButton();
        public bool IsSelected => m_selected;
        public bool HasButton => m_normalButton != null || m_selectedButton != null;

        private void Awake()
        {
            ApplyState();
        }

        private void OnEnable()
        {
            AddButtonListeners();
            ApplyState();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        private void Reset()
        {
            AutoAssignButtons();
        }

        private void OnValidate()
        {
            ApplyState();
        }

        public void Select()
        {
            SetSelected(true);
        }

        public void Deselect()
        {
            SetSelected(false);
        }

        public void Toggle()
        {
            SetSelected(!m_selected);
        }

        public void SetSelected(bool selected)
        {
            SetSelected(selected, true);
        }

        public void SetSelectedWithoutNotify(bool selected)
        {
            SetSelected(selected, false);
        }

        public void SetSelected(bool selected, bool notify)
        {
            if (m_selected == selected)
            {
                ApplyState();
                return;
            }

            m_selected = selected;
            ApplyState();

            if (notify)
            {
                OnStateChanged?.Invoke(m_selected);
            }
        }

        public void Refresh()
        {
            ApplyState();
        }

        private Button GetCurrentButton()
        {
            if (m_selected && m_selectedButton != null)
            {
                return m_selectedButton;
            }

            if (m_normalButton != null)
            {
                return m_normalButton;
            }

            return m_selectedButton;
        }

        private void AutoAssignButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
            {
                m_normalButton = buttons[0];
            }

            if (buttons.Length > 1)
            {
                m_selectedButton = buttons[1];
            }
        }

        private void AddButtonListeners()
        {
            if (m_listenersAdded)
            {
                return;
            }

            if (m_normalButton != null)
            {
                m_normalButton.onClick.AddListener(OnButtonClick);
            }

            if (m_selectedButton != null && m_selectedButton != m_normalButton)
            {
                m_selectedButton.onClick.AddListener(OnButtonClick);
            }

            m_listenersAdded = true;
        }

        private void RemoveButtonListeners()
        {
            if (!m_listenersAdded)
            {
                return;
            }

            if (m_normalButton != null)
            {
                m_normalButton.onClick.RemoveListener(OnButtonClick);
            }

            if (m_selectedButton != null && m_selectedButton != m_normalButton)
            {
                m_selectedButton.onClick.RemoveListener(OnButtonClick);
            }

            m_listenersAdded = false;
        }

        private void OnButtonClick()
        {
            OnClick?.Invoke();
        }

        private void ApplyState()
        {
            if (m_normalButton != null && m_normalButton == m_selectedButton)
            {
                SetButtonActive(m_normalButton, true);
                return;
            }

            bool normalActive = !m_selected || m_selectedButton == null;
            bool selectedActive = m_selected || m_normalButton == null;

            SetButtonActive(m_normalButton, normalActive);
            SetButtonActive(m_selectedButton, selectedActive);
        }

        private static void SetButtonActive(Button button, bool active)
        {
            if (button != null && button.gameObject.activeSelf != active)
            {
                button.gameObject.SetActive(active);
            }
        }
    }
}
