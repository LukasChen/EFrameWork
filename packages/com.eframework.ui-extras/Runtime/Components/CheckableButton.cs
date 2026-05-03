using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class CheckableButton : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button")]
        [SerializeField] private Button m_button;
        [SerializeField] private bool m_forceButtonTransitionNone = true;
        [SerializeField] private bool m_checked = false;
        [SerializeField] private bool m_toggleOnClick = true;
        [SerializeField] private bool m_allowUncheck = true;
        [SerializeField] private bool m_focusOnClick = true;
        [SerializeField] private bool m_focusOnPointerEnter = false;
        [SerializeField] private float m_submitPressedDuration = 0.08f;

        [Header("State Objects")]
        [SerializeField] private GameObject m_uncheckedState;
        [SerializeField] private GameObject m_checkedState;
        [SerializeField] private GameObject m_hoverState;
        [SerializeField] private GameObject m_focusState;
        [SerializeField] private GameObject m_pressedState;
        [SerializeField] private GameObject m_disabledState;

        [Header("Graphic Colors")]
        [SerializeField] private bool m_useGraphicColor = false;
        [SerializeField] private Graphic m_targetGraphic;
        [SerializeField] private Color m_normalColor = Color.white;
        [SerializeField] private Color m_checkedColor = Color.white;
        [SerializeField] private Color m_hoveredColor = Color.white;
        [SerializeField] private Color m_checkedHoveredColor = Color.white;
        [SerializeField] private Color m_focusedColor = Color.white;
        [SerializeField] private Color m_checkedFocusedColor = Color.white;
        [SerializeField] private Color m_pressedColor = Color.white;
        [SerializeField] private Color m_disabledColor = Color.gray;

        [Header("Events")]
        [SerializeField] public UnityEvent OnClick = new();
        [SerializeField] public UnityEvent<bool> OnCheckedChanged = new();
        [SerializeField] public UnityEvent<bool> OnFocusChanged = new();

        private bool m_focused;
        private bool m_hovered;
        private bool m_pressed;
        private bool m_listenerAdded;
        private bool m_lastInteractable;
        private bool m_hasInteractableSnapshot;

        public Button Button => m_button;
        public bool IsChecked => m_checked;
        public bool IsFocused => m_focused;
        public bool IsHovered => m_hovered;
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
            EnsureButtonTransition();
            ApplyState();
        }

        private void OnEnable()
        {
            EnsureButton();
            EnsureButtonTransition();
            AddButtonListener();
            ApplyState();
        }

        private void OnDisable()
        {
            RemoveButtonListener();
            CancelInvoke(nameof(ClearPressed));
            m_pressed = false;
            m_hovered = false;
            SetFocused(false, false);
        }

        private void Update()
        {
            bool interactable = IsInteractableForState();
            if (m_hasInteractableSnapshot && m_lastInteractable == interactable)
            {
                return;
            }

            if (!interactable)
            {
                CancelInvoke(nameof(ClearPressed));
                m_pressed = false;
                m_hovered = false;
            }

            ApplyState(interactable);
        }

        private void Reset()
        {
            m_button = GetComponent<Button>();
            m_targetGraphic = m_button != null ? m_button.targetGraphic : GetComponent<Graphic>();
            EnsureButtonTransition();
            m_uncheckedState = FindChildState("unchecked", "uncheck", "normal");
            m_checkedState = FindChildState("checked", "check", "selected", "select");
            m_hoverState = FindChildState("hovered", "hover", "pointer");
            m_focusState = FindChildState("focused", "focus", "highlight");
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

            EnsureButtonTransition();
            ApplyState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true, true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ClearPressed();
            SetFocused(false, true);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractableForState())
            {
                return;
            }

            SetPressed(true);

            if (m_submitPressedDuration > 0f)
            {
                CancelInvoke(nameof(ClearPressed));
                Invoke(nameof(ClearPressed), m_submitPressedDuration);
            }
            else
            {
                ClearPressed();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractableForState())
            {
                return;
            }

            m_hovered = true;

            if (m_focusOnPointerEnter)
            {
                Focus();
            }

            ApplyState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!m_hovered)
            {
                return;
            }

            m_hovered = false;
            ApplyState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractableForState())
            {
                return;
            }

            if (m_focusOnClick)
            {
                Focus();
            }

            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ClearPressed();
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
            if (!IsInteractableForState() || EventSystem.current == null)
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

        private void EnsureButtonTransition()
        {
            if (m_forceButtonTransitionNone && m_button != null && m_button.transition != Selectable.Transition.None)
            {
                m_button.transition = Selectable.Transition.None;
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

        private void SetPressed(bool pressed)
        {
            if (m_pressed == pressed)
            {
                ApplyState();
                return;
            }

            m_pressed = pressed;
            ApplyState();
        }

        private void ClearPressed()
        {
            CancelInvoke(nameof(ClearPressed));
            SetPressed(false);
        }

        private void ApplyState()
        {
            ApplyState(IsInteractableForState());
        }

        private void ApplyState(bool interactable)
        {
            m_lastInteractable = interactable;
            m_hasInteractableSnapshot = true;

            bool uncheckedActive = interactable && !m_checked;
            bool checkedActive = interactable && m_checked;
            bool hoverActive = interactable && m_hovered;
            bool focusActive = interactable && m_focused;
            bool pressedActive = interactable && m_pressed;
            bool disabledActive = !interactable;

            SetActive(m_uncheckedState, IsStateObjectActive(m_uncheckedState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));
            SetActive(m_checkedState, IsStateObjectActive(m_checkedState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));
            SetActive(m_hoverState, IsStateObjectActive(m_hoverState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));
            SetActive(m_focusState, IsStateObjectActive(m_focusState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));
            SetActive(m_pressedState, IsStateObjectActive(m_pressedState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));
            SetActive(m_disabledState, IsStateObjectActive(m_disabledState, uncheckedActive, checkedActive, hoverActive, focusActive, pressedActive, disabledActive));

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

            if (m_checked && m_hovered)
            {
                return m_checkedHoveredColor;
            }

            if (m_checked)
            {
                return m_checkedColor;
            }

            if (m_focused)
            {
                return m_focusedColor;
            }

            if (m_hovered)
            {
                return m_hoveredColor;
            }

            return m_normalColor;
        }

        private bool IsInteractableForState()
        {
            if (m_button == null)
            {
                return false;
            }

            if (!Application.isPlaying)
            {
                return m_button.interactable;
            }

            return m_button.IsActive() && m_button.IsInteractable();
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

        private bool IsStateObjectActive(
            GameObject target,
            bool uncheckedActive,
            bool checkedActive,
            bool hoverActive,
            bool focusActive,
            bool pressedActive,
            bool disabledActive)
        {
            if (target == null)
            {
                return false;
            }

            return (target == m_uncheckedState && uncheckedActive)
                || (target == m_checkedState && checkedActive)
                || (target == m_hoverState && hoverActive)
                || (target == m_focusState && focusActive)
                || (target == m_pressedState && pressedActive)
                || (target == m_disabledState && disabledActive);
        }
    }
}
