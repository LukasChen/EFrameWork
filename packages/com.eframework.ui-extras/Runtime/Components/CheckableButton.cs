using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class CheckableButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button")]
        [SerializeField] private Button m_button;
        [SerializeField] private bool m_checked = false;
        [SerializeField] private bool m_toggleOnClick = true;
        [SerializeField] private bool m_allowUncheck = true;
        [SerializeField] private bool m_focusOnClick = true;
        [SerializeField] private bool m_focusOnPointerEnter = false;

        [Header("State Objects")]
        [SerializeField] private GameObject m_uncheckedState;
        [SerializeField] private GameObject m_checkedState;
        [SerializeField] private GameObject m_focusState;
        [SerializeField] private GameObject m_pressedState;
        [SerializeField] private GameObject m_disabledState;

        [Header("Graphic Colors")]
        [SerializeField] private bool m_useGraphicColor = false;
        [SerializeField] private Graphic m_targetGraphic;
        [SerializeField] private Color m_normalColor = Color.white;
        [SerializeField] private Color m_checkedColor = Color.white;
        [SerializeField] private Color m_focusedColor = Color.white;
        [SerializeField] private Color m_checkedFocusedColor = Color.white;
        [SerializeField] private Color m_pressedColor = Color.white;
        [SerializeField] private Color m_disabledColor = Color.gray;

        [Header("Events")]
        [SerializeField] public UnityEvent OnClick = new();
        [SerializeField] public UnityEvent<bool> OnCheckedChanged = new();
        [SerializeField] public UnityEvent<bool> OnFocusChanged = new();

        private bool m_focused;
        private bool m_pressed;
        private bool m_listenerAdded;

        public Button Button => m_button;
        public bool IsChecked => m_checked;
        public bool IsFocused => m_focused;
        public bool IsPressed => m_pressed;
        public bool HasButton => m_button != null;
        public bool ToggleOnClick
        {
            get => m_toggleOnClick;
            set => m_toggleOnClick = value;
        }

        public bool AllowUncheck
        {
            get => m_allowUncheck;
            set => m_allowUncheck = value;
        }

        private void Awake()
        {
            EnsureButton();
            ApplyState();
        }

        private void OnEnable()
        {
            EnsureButton();
            AddButtonListener();
            ApplyState();
        }

        private void OnDisable()
        {
            RemoveButtonListener();
            m_pressed = false;
            SetFocused(false, false);
        }

        private void Reset()
        {
            m_button = GetComponent<Button>();
            m_targetGraphic = m_button != null ? m_button.targetGraphic : GetComponent<Graphic>();
            m_uncheckedState = FindChildState("unchecked", "uncheck", "normal");
            m_checkedState = FindChildState("checked", "check", "selected", "select");
            m_focusState = FindChildState("focused", "focus", "highlight", "hover");
            m_pressedState = FindChildState("pressed", "press", "down");
            m_disabledState = FindChildState("disabled", "disable", "inactive");
        }

        private void OnValidate()
        {
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }

            if (m_targetGraphic == null && m_button != null)
            {
                m_targetGraphic = m_button.targetGraphic;
            }

            ApplyState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true, true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            m_pressed = false;
            SetFocused(false, true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            if (m_focusOnPointerEnter)
            {
                Focus();
                return;
            }

            SetFocused(true, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                return;
            }

            SetFocused(false, true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            if (m_focusOnClick)
            {
                Focus();
            }

            m_pressed = true;
            ApplyState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_pressed)
            {
                return;
            }

            m_pressed = false;
            ApplyState();
        }

        public void Check()
        {
            SetChecked(true);
        }

        public void Uncheck()
        {
            SetChecked(false);
        }

        public void Toggle()
        {
            if (m_checked && !m_allowUncheck)
            {
                return;
            }

            SetChecked(!m_checked);
        }

        public void SetChecked(bool isChecked)
        {
            SetChecked(isChecked, true);
        }

        public void SetCheckedWithoutNotify(bool isChecked)
        {
            SetChecked(isChecked, false);
        }

        public void SetChecked(bool isChecked, bool notify)
        {
            if (m_checked == isChecked)
            {
                ApplyState();
                return;
            }

            m_checked = isChecked;
            ApplyState();

            if (notify)
            {
                OnCheckedChanged?.Invoke(m_checked);
            }
        }

        public void Focus()
        {
            if (!IsInteractable() || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void Refresh()
        {
            ApplyState();
        }

        private void OnButtonClick()
        {
            if (m_toggleOnClick)
            {
                Toggle();
            }

            OnClick?.Invoke();
        }

        private void EnsureButton()
        {
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }
        }

        private void AddButtonListener()
        {
            if (m_listenerAdded || m_button == null)
            {
                return;
            }

            m_button.onClick.AddListener(OnButtonClick);
            m_listenerAdded = true;
        }

        private void RemoveButtonListener()
        {
            if (!m_listenerAdded || m_button == null)
            {
                return;
            }

            m_button.onClick.RemoveListener(OnButtonClick);
            m_listenerAdded = false;
        }

        private void SetFocused(bool focused, bool notify)
        {
            if (m_focused == focused)
            {
                ApplyState();
                return;
            }

            m_focused = focused;
            ApplyState();

            if (notify)
            {
                OnFocusChanged?.Invoke(m_focused);
            }
        }

        private void ApplyState()
        {
            bool interactable = IsInteractable();

            SetActive(m_uncheckedState, interactable && !m_checked);
            SetActive(m_checkedState, interactable && m_checked);
            SetActive(m_focusState, interactable && m_focused);
            SetActive(m_pressedState, interactable && m_pressed);
            SetActive(m_disabledState, !interactable);

            if (m_useGraphicColor && m_targetGraphic != null)
            {
                m_targetGraphic.color = GetTargetColor(interactable);
            }
        }

        private Color GetTargetColor(bool interactable)
        {
            if (!interactable)
            {
                return m_disabledColor;
            }

            if (m_pressed)
            {
                return m_pressedColor;
            }

            if (m_checked && m_focused)
            {
                return m_checkedFocusedColor;
            }

            if (m_checked)
            {
                return m_checkedColor;
            }

            if (m_focused)
            {
                return m_focusedColor;
            }

            return m_normalColor;
        }

        private bool IsInteractable()
        {
            return m_button != null && m_button.IsActive() && m_button.IsInteractable();
        }

        private GameObject FindChildState(params string[] nameParts)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                string childName = child.name.ToLowerInvariant();

                for (int j = 0; j < nameParts.Length; j++)
                {
                    if (childName.Contains(nameParts[j]))
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
