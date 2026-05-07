using System;
using System.Collections.Generic;
using EFramework.Runtime.Tween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Interaction
{
    [DisallowMultipleComponent]
    public sealed class UIInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private Selectable m_selectable;
        [SerializeField] private bool m_autoDriveSelectable = true;
        [SerializeField] private bool m_autoDriveToggleChecked = true;
        [SerializeField] private bool m_restorePressedOnPointerExit = true;
        [SerializeField] private float m_submitPressedDuration = 0.08f;

        [Header("States")]
        [SerializeField] private UIInteractionFeedbackState m_normal = new() { Enabled = true };
        [SerializeField] private UIInteractionFeedbackState m_hover = new();
        [SerializeField] private UIInteractionFeedbackState m_focus = new();
        [SerializeField] private UIInteractionFeedbackState m_pressed = new();
        [SerializeField] private UIInteractionFeedbackState m_checked = new();
        [SerializeField] private UIInteractionFeedbackState m_disabled = new();

        [Header("Enable")]
        [SerializeField] private UIInteractionEnableFeedback m_enableFeedback = new();

        private readonly Dictionary<Transform, Vector3> m_baseScales = new();
        private readonly Dictionary<RectTransform, Vector2> m_baseOffsets = new();
        private readonly List<GameObject> m_knownStateObjects = new();

        private Toggle m_toggle;
        private bool m_listenerAdded;
        private bool m_hovered;
        private bool m_focused;
        private bool m_isPressed;
        private bool m_checkedValue;
        private bool m_lastInteractable = true;

        private void Awake()
        {
            ResolveSelectable();
            CaptureBaseValues();
        }

        private void OnEnable()
        {
            ResolveSelectable();
            AddListeners();
            SyncCheckedFromToggle();
            CaptureBaseValues();
            m_lastInteractable = IsInteractable();

            ApplyCurrentState();

            if (m_enableFeedback.Enabled)
            {
                PlayEnableFeedback();
            }
        }

        private void OnDisable()
        {
            RemoveListeners();
            CancelInvoke(nameof(ClearPressed));
            m_hovered = false;
            m_focused = false;
            m_isPressed = false;
            KillEnableFeedback();
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

            m_submitPressedDuration = Mathf.Max(0f, m_submitPressedDuration);
        }

        private void Update()
        {
            var isInteractable = IsInteractable();
            if (m_lastInteractable == isInteractable)
            {
                return;
            }

            m_lastInteractable = isInteractable;
            if (!isInteractable)
            {
                m_hovered = false;
                m_focused = false;
                m_isPressed = false;
            }

            ApplyCurrentState();
        }

        public void SetChecked(bool isChecked)
        {
            if (m_checkedValue == isChecked)
            {
                ApplyCurrentState();
                return;
            }

            m_checkedValue = isChecked;
            ApplyCurrentState();
        }

        public void Refresh()
        {
            CaptureBaseValues();
            ApplyCurrentState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ShouldAutoDrive())
            {
                return;
            }

            m_hovered = true;
            ApplyCurrentState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ShouldAutoDrive())
            {
                return;
            }

            m_hovered = false;
            if (m_restorePressedOnPointerExit)
            {
                m_isPressed = false;
            }

            ApplyCurrentState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!ShouldAutoDrive() || !IsInteractable())
            {
                return;
            }

            m_isPressed = true;
            ApplyCurrentState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!ShouldAutoDrive())
            {
                return;
            }

            ClearPressed();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!ShouldAutoDrive())
            {
                return;
            }

            m_focused = true;
            ApplyCurrentState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!ShouldAutoDrive())
            {
                return;
            }

            m_focused = false;
            ClearPressed();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!ShouldAutoDrive() || !IsInteractable())
            {
                return;
            }

            m_isPressed = true;
            ApplyCurrentState();

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

        private bool ShouldAutoDrive()
        {
            return m_autoDriveSelectable;
        }

        private bool IsInteractable()
        {
            return m_selectable == null || (m_selectable.IsActive() && m_selectable.IsInteractable());
        }

        private void ClearPressed()
        {
            CancelInvoke(nameof(ClearPressed));
            if (!m_isPressed)
            {
                return;
            }

            m_isPressed = false;
            ApplyCurrentState();
        }

        private void ResolveSelectable()
        {
            if (m_selectable == null)
            {
                m_selectable = GetComponent<Selectable>();
            }

            m_toggle = m_selectable as Toggle;
        }

        private void AddListeners()
        {
            if (m_listenerAdded || !m_autoDriveToggleChecked || m_toggle == null)
            {
                return;
            }

            m_toggle.onValueChanged.AddListener(OnToggleValueChanged);
            m_listenerAdded = true;
        }

        private void RemoveListeners()
        {
            if (!m_listenerAdded || m_toggle == null)
            {
                return;
            }

            m_toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            m_listenerAdded = false;
        }

        private void OnToggleValueChanged(bool isOn)
        {
            SetChecked(isOn);
        }

        private void SyncCheckedFromToggle()
        {
            if (m_autoDriveToggleChecked && m_toggle != null)
            {
                m_checkedValue = m_toggle.isOn;
            }
        }

        private void ApplyCurrentState()
        {
            var state = ResolveCurrentState();
            ApplyState(state);
        }

        private UIInteractionFeedbackState ResolveCurrentState()
        {
            if (!IsInteractable() && m_disabled.Enabled)
            {
                return m_disabled;
            }

            if (m_isPressed && m_pressed.Enabled)
            {
                return m_pressed;
            }

            if (m_checkedValue && m_checked.Enabled)
            {
                return m_checked;
            }

            if (m_hovered && m_hover.Enabled)
            {
                return m_hover;
            }

            if (m_focused && m_focus.Enabled)
            {
                return m_focus;
            }

            return m_normal;
        }

        private void ApplyState(UIInteractionFeedbackState state)
        {
            DeactivateKnownStateObjects();
            if (state == null || !state.Enabled)
            {
                return;
            }

            for (var i = 0; i < state.ActiveObjects.Count; i++)
            {
                var target = state.ActiveObjects[i];
                if (target != null)
                {
                    target.SetActive(true);
                }
            }

            for (var i = 0; i < state.GraphicColors.Count; i++)
            {
                var item = state.GraphicColors[i];
                if (item.Target != null)
                {
                    item.Target.color = item.Color;
                }
            }

            for (var i = 0; i < state.Scales.Count; i++)
            {
                var item = state.Scales[i];
                if (item.Target == null)
                {
                    continue;
                }

                if (!m_baseScales.TryGetValue(item.Target, out var baseScale))
                {
                    baseScale = item.Target.localScale;
                    m_baseScales[item.Target] = baseScale;
                }

                item.Target.localScale = Vector3.Scale(baseScale, item.Multiplier);
            }

            for (var i = 0; i < state.Offsets.Count; i++)
            {
                var item = state.Offsets[i];
                if (item.Target == null)
                {
                    continue;
                }

                if (!m_baseOffsets.TryGetValue(item.Target, out var baseOffset))
                {
                    baseOffset = item.Target.anchoredPosition;
                    m_baseOffsets[item.Target] = baseOffset;
                }

                item.Target.anchoredPosition = baseOffset + item.Offset;
            }

            for (var i = 0; i < state.Alphas.Count; i++)
            {
                var item = state.Alphas[i];
                if (item.Target != null)
                {
                    item.Target.alpha = Mathf.Clamp01(item.Alpha);
                }
            }
        }

        private void DeactivateKnownStateObjects()
        {
            RebuildKnownStateObjects();
            for (var i = 0; i < m_knownStateObjects.Count; i++)
            {
                var target = m_knownStateObjects[i];
                if (target != null)
                {
                    target.SetActive(false);
                }
            }
        }

        private void RebuildKnownStateObjects()
        {
            m_knownStateObjects.Clear();
            AddKnownObjects(m_normal);
            AddKnownObjects(m_hover);
            AddKnownObjects(m_focus);
            AddKnownObjects(m_pressed);
            AddKnownObjects(m_checked);
            AddKnownObjects(m_disabled);
        }

        private void AddKnownObjects(UIInteractionFeedbackState state)
        {
            if (state == null)
            {
                return;
            }

            for (var i = 0; i < state.ActiveObjects.Count; i++)
            {
                var target = state.ActiveObjects[i];
                if (target != null && !m_knownStateObjects.Contains(target))
                {
                    m_knownStateObjects.Add(target);
                }
            }
        }

        private void CaptureBaseValues()
        {
            CaptureStateBaseValues(m_normal);
            CaptureStateBaseValues(m_hover);
            CaptureStateBaseValues(m_focus);
            CaptureStateBaseValues(m_pressed);
            CaptureStateBaseValues(m_checked);
            CaptureStateBaseValues(m_disabled);
            CaptureEnableBaseValues();
        }

        private void CaptureStateBaseValues(UIInteractionFeedbackState state)
        {
            if (state == null)
            {
                return;
            }

            for (var i = 0; i < state.Scales.Count; i++)
            {
                var target = state.Scales[i].Target;
                if (target != null && !m_baseScales.ContainsKey(target))
                {
                    m_baseScales[target] = target.localScale;
                }
            }

            for (var i = 0; i < state.Offsets.Count; i++)
            {
                var target = state.Offsets[i].Target;
                if (target != null && !m_baseOffsets.ContainsKey(target))
                {
                    m_baseOffsets[target] = target.anchoredPosition;
                }
            }
        }

        private void CaptureEnableBaseValues()
        {
            if (m_enableFeedback == null)
            {
                return;
            }

            for (var i = 0; i < m_enableFeedback.Scales.Count; i++)
            {
                var target = m_enableFeedback.Scales[i].Target;
                if (target != null && !m_baseScales.ContainsKey(target))
                {
                    m_baseScales[target] = target.localScale;
                }
            }

            for (var i = 0; i < m_enableFeedback.Offsets.Count; i++)
            {
                var target = m_enableFeedback.Offsets[i].Target;
                if (target != null && !m_baseOffsets.ContainsKey(target))
                {
                    m_baseOffsets[target] = target.anchoredPosition;
                }
            }
        }

        private void PlayEnableFeedback()
        {
            KillEnableFeedback();

            var duration = Mathf.Max(0f, m_enableFeedback.Duration);
            for (var i = 0; i < m_enableFeedback.Scales.Count; i++)
            {
                var item = m_enableFeedback.Scales[i];
                if (item.Target == null)
                {
                    continue;
                }

                var baseScale = m_baseScales.TryGetValue(item.Target, out var cached) ? cached : item.Target.localScale;
                var from = Vector3.Scale(baseScale, item.FromMultiplier);
                var to = Vector3.Scale(baseScale, item.ToMultiplier);
                item.Target.localScale = from;
                EFrameTween.Vector3(from, to, duration, value => item.Target.localScale = value, new EFrameTweenOptions
                {
                    Target = item.Target,
                    Ease = m_enableFeedback.Ease,
                    IgnoreTimeScale = m_enableFeedback.IgnoreTimeScale
                });
            }

            for (var i = 0; i < m_enableFeedback.Offsets.Count; i++)
            {
                var item = m_enableFeedback.Offsets[i];
                if (item.Target == null)
                {
                    continue;
                }

                var baseOffset = m_baseOffsets.TryGetValue(item.Target, out var cached) ? cached : item.Target.anchoredPosition;
                var from = baseOffset + item.FromOffset;
                var to = baseOffset + item.ToOffset;
                item.Target.anchoredPosition = from;
                EFrameTween.Vector2(from, to, duration, value => item.Target.anchoredPosition = value, new EFrameTweenOptions
                {
                    Target = item.Target,
                    Ease = m_enableFeedback.Ease,
                    IgnoreTimeScale = m_enableFeedback.IgnoreTimeScale
                });
            }

            for (var i = 0; i < m_enableFeedback.Alphas.Count; i++)
            {
                var item = m_enableFeedback.Alphas[i];
                if (item.Target == null)
                {
                    continue;
                }

                var from = Mathf.Clamp01(item.FromAlpha);
                var to = Mathf.Clamp01(item.ToAlpha);
                item.Target.alpha = from;
                EFrameTween.Float(from, to, duration, value => item.Target.alpha = value, new EFrameTweenOptions
                {
                    Target = item.Target,
                    Ease = m_enableFeedback.Ease,
                    IgnoreTimeScale = m_enableFeedback.IgnoreTimeScale
                });
            }
        }

        private void KillEnableFeedback()
        {
            if (m_enableFeedback == null)
            {
                return;
            }

            for (var i = 0; i < m_enableFeedback.Scales.Count; i++)
            {
                if (m_enableFeedback.Scales[i].Target != null)
                {
                    EFrameTween.Kill(m_enableFeedback.Scales[i].Target);
                }
            }

            for (var i = 0; i < m_enableFeedback.Offsets.Count; i++)
            {
                if (m_enableFeedback.Offsets[i].Target != null)
                {
                    EFrameTween.Kill(m_enableFeedback.Offsets[i].Target);
                }
            }

            for (var i = 0; i < m_enableFeedback.Alphas.Count; i++)
            {
                if (m_enableFeedback.Alphas[i].Target != null)
                {
                    EFrameTween.Kill(m_enableFeedback.Alphas[i].Target);
                }
            }
        }
    }

    [Serializable]
    public sealed class UIInteractionFeedbackState
    {
        public bool Enabled;
        public List<GameObject> ActiveObjects = new();
        public List<UIGraphicColorFeedback> GraphicColors = new();
        public List<UIScaleFeedback> Scales = new();
        public List<UIOffsetFeedback> Offsets = new();
        public List<UIAlphaFeedback> Alphas = new();
    }

    [Serializable]
    public sealed class UIInteractionEnableFeedback
    {
        public bool Enabled;
        public float Duration = 0.15f;
        public EFrameEase Ease = EFrameEase.OutQuad;
        public bool IgnoreTimeScale = true;
        public List<UIEnableScaleFeedback> Scales = new();
        public List<UIEnableOffsetFeedback> Offsets = new();
        public List<UIEnableAlphaFeedback> Alphas = new();
    }

    [Serializable]
    public sealed class UIGraphicColorFeedback
    {
        public Graphic Target;
        public Color Color = Color.white;
    }

    [Serializable]
    public sealed class UIScaleFeedback
    {
        public Transform Target;
        public Vector3 Multiplier = Vector3.one;
    }

    [Serializable]
    public sealed class UIOffsetFeedback
    {
        public RectTransform Target;
        public Vector2 Offset;
    }

    [Serializable]
    public sealed class UIAlphaFeedback
    {
        public CanvasGroup Target;
        [Range(0f, 1f)] public float Alpha = 1f;
    }

    [Serializable]
    public sealed class UIEnableScaleFeedback
    {
        public Transform Target;
        public Vector3 FromMultiplier = Vector3.one * 0.85f;
        public Vector3 ToMultiplier = Vector3.one;
    }

    [Serializable]
    public sealed class UIEnableOffsetFeedback
    {
        public RectTransform Target;
        public Vector2 FromOffset = new(0f, 20f);
        public Vector2 ToOffset = Vector2.zero;
    }

    [Serializable]
    public sealed class UIEnableAlphaFeedback
    {
        public CanvasGroup Target;
        [Range(0f, 1f)] public float FromAlpha;
        [Range(0f, 1f)] public float ToAlpha = 1f;
    }
}
