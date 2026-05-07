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

        [Header("Targets")]
        [SerializeField] private bool m_useTargetFeedback = true;
        [SerializeField] private bool m_autoSetupTargets = true;
        [SerializeField] private List<UIInteractionFeedbackTarget> m_targets = new();

        [Header("Enable")]
        [SerializeField] private UIInteractionEnableFeedback m_enableFeedback = new();

        private readonly Dictionary<Transform, Vector3> m_baseScales = new();
        private readonly Dictionary<RectTransform, Vector2> m_baseOffsets = new();
        private readonly Dictionary<Graphic, Color> m_baseColors = new();
        private readonly Dictionary<CanvasGroup, float> m_baseAlphas = new();
        private readonly Dictionary<GameObject, bool> m_baseActiveStates = new();
        private readonly Dictionary<Graphic, Material> m_baseGraphicMaterials = new();
        private readonly Dictionary<Component, Material> m_baseTextMaterials = new();
        private readonly HashSet<RectTransform> m_restoreRects = new();
        private readonly HashSet<CanvasGroup> m_restoreCanvasGroups = new();
        private readonly HashSet<Graphic> m_restoreGraphics = new();
        private readonly HashSet<Component> m_restoreTmpTexts = new();
        private readonly Dictionary<UIInteractionFeedbackTweenKey, UIInteractionFeedbackTweenTarget> m_tweenTargets = new();

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
            ResolveTargetReferences();
            CaptureBaseValues();
        }

        private void OnEnable()
        {
            ResolveSelectable();
            if (m_autoSetupTargets && m_targets.Count == 0)
            {
                SetupDefaultTargets();
            }

            ResolveTargetReferences();
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
            RestoreTargetBaseValues(false);
            m_restoreRects.Clear();
            m_restoreCanvasGroups.Clear();
            m_restoreGraphics.Clear();
            m_restoreTmpTexts.Clear();
            KillEnableFeedback();
        }

        private void Reset()
        {
            m_selectable = GetComponent<Selectable>();
            m_useTargetFeedback = true;
            m_autoSetupTargets = true;
            SetupDefaultTargets();
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
            ClearBaseValues();
            ResolveTargetReferences();
            CaptureBaseValues();
            ApplyCurrentState();
        }

        [ContextMenu("EFrame/Setup Common Button Feedback")]
        private void SetupCommonButtonFeedback()
        {
            m_useTargetFeedback = true;
            m_autoSetupTargets = true;
            m_selectable = GetComponent<Selectable>();
            SetupDefaultTargets();
            Refresh();
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

        private void SetupDefaultTargets()
        {
            m_targets.Clear();

            var content = FindContentTarget();
            if (content != null)
            {
                m_targets.Add(UIInteractionFeedbackTarget.Content(content, GetRelativePath(content.transform)));
            }

            var selected = FindSelectedTarget();
            if (selected != null)
            {
                m_targets.Add(UIInteractionFeedbackTarget.Selected(selected, GetRelativePath(selected.transform)));
            }
        }

        private void ResolveTargetReferences()
        {
            for (var i = 0; i < m_targets.Count; i++)
            {
                var target = m_targets[i];
                if (target == null || string.IsNullOrWhiteSpace(target.TargetPath))
                {
                    continue;
                }

                var resolved = FindRelativeRect(target.TargetPath);
                if (resolved != null)
                {
                    target.Target = resolved;
                }
            }
        }

        private RectTransform FindRelativeRect(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
            {
                return transform as RectTransform;
            }

            var found = transform.Find(relativePath);
            return found as RectTransform;
        }

        private string GetRelativePath(Transform target)
        {
            if (target == null || target == transform)
            {
                return ".";
            }

            var path = target.name;
            var parent = target.parent;
            while (parent != null && parent != transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return parent == transform ? path : string.Empty;
        }

        private RectTransform FindContentTarget()
        {
            var knownNames = new[] { "content", "container", "body", "texticon", "labelicon", "label", "caption" };
            for (var i = 0; i < knownNames.Length; i++)
            {
                var child = FindDirectChildRect(knownNames[i]);
                if (child != null)
                {
                    return child;
                }
            }

            var selectableGraphic = m_selectable != null ? m_selectable.targetGraphic : null;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null || IsStateVisualName(child.name))
                {
                    continue;
                }

                var graphics = child.GetComponentsInChildren<Graphic>(true);
                if (graphics.Length == 0)
                {
                    continue;
                }

                if (selectableGraphic != null && graphics.Length == 1 && graphics[0] == selectableGraphic)
                {
                    continue;
                }

                return child;
            }

            return transform as RectTransform;
        }

        private RectTransform FindSelectedTarget()
        {
            var knownNames = new[] { "selected", "select", "checked", "check", "glow", "highlight", "highLight" };
            for (var i = 0; i < knownNames.Length; i++)
            {
                var child = FindChildRect(knownNames[i]);
                if (child != null && child != transform)
                {
                    return child;
                }
            }

            return null;
        }

        private RectTransform FindDirectChildRect(string lowerInvariantName)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child != null && string.Equals(child.name, lowerInvariantName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private RectTransform FindChildRect(string lowerInvariantName)
        {
            var children = GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child != null && string.Equals(child.name, lowerInvariantName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsStateVisualName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            var normalized = objectName.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            return normalized.Contains("normal")
                || normalized.Contains("hover")
                || normalized.Contains("press")
                || normalized.Contains("disable")
                || normalized.Contains("selected")
                || normalized.Contains("checked")
                || normalized == "bg"
                || normalized.Contains("background");
        }

        private void ApplyCurrentState()
        {
            if (m_useTargetFeedback)
            {
                ApplyTargetState(ResolveCurrentVisualState());
            }
        }

        private UIInteractionVisualState ResolveCurrentVisualState()
        {
            if (!IsInteractable())
            {
                return UIInteractionVisualState.Disabled;
            }

            if (m_isPressed)
            {
                return UIInteractionVisualState.Pressed;
            }

            if (m_checkedValue)
            {
                return UIInteractionVisualState.Checked;
            }

            if (m_hovered)
            {
                return UIInteractionVisualState.Hover;
            }

            if (m_focused)
            {
                return UIInteractionVisualState.Focus;
            }

            return UIInteractionVisualState.Normal;
        }

        private void ApplyTargetState(UIInteractionVisualState visualState)
        {
            RestoreTargetBaseValues();

            for (var i = 0; i < m_targets.Count; i++)
            {
                var target = m_targets[i];
                if (target == null || !target.Enabled)
                {
                    continue;
                }

                for (var j = 0; j < target.Actions.Count; j++)
                {
                    var action = target.Actions[j];
                    if (action == null || !action.Enabled || !action.Matches(visualState))
                    {
                        continue;
                    }

                    ApplyTargetAction(target, action);
                }
            }
        }

        private void RestoreTargetBaseValues(bool restoreActiveState = true)
        {
            BuildRestoreSets();

            foreach (var rectTransform in m_restoreRects)
            {
                if (rectTransform == null)
                {
                    continue;
                }

                KillTween(rectTransform, UIInteractionFeedbackTweenProperty.Offset);
                KillTween(rectTransform, UIInteractionFeedbackTweenProperty.Scale);
                if (m_baseOffsets.TryGetValue(rectTransform, out var baseOffset))
                {
                    rectTransform.anchoredPosition = baseOffset;
                }

                if (m_baseScales.TryGetValue(rectTransform, out var baseScale))
                {
                    rectTransform.localScale = baseScale;
                }

                if (restoreActiveState && m_baseActiveStates.TryGetValue(rectTransform.gameObject, out var activeSelf))
                {
                    rectTransform.gameObject.SetActive(activeSelf);
                }
            }

            foreach (var canvasGroup in m_restoreCanvasGroups)
            {
                if (canvasGroup == null)
                {
                    continue;
                }

                KillTween(canvasGroup, UIInteractionFeedbackTweenProperty.Alpha);
                if (m_baseAlphas.TryGetValue(canvasGroup, out var baseAlpha))
                {
                    canvasGroup.alpha = baseAlpha;
                }
            }

            foreach (var graphic in m_restoreGraphics)
            {
                if (graphic == null)
                {
                    continue;
                }

                KillTween(graphic, UIInteractionFeedbackTweenProperty.Color);
                KillTween(graphic, UIInteractionFeedbackTweenProperty.Alpha);
                if (m_baseColors.TryGetValue(graphic, out var baseColor))
                {
                    graphic.color = baseColor;
                }

                if (m_baseGraphicMaterials.TryGetValue(graphic, out var baseMaterial))
                {
                    graphic.material = baseMaterial;
                }
            }

            foreach (var tmpText in m_restoreTmpTexts)
            {
                if (tmpText == null)
                {
                    continue;
                }

                if (m_baseTextMaterials.TryGetValue(tmpText, out var baseMaterial))
                {
                    SetTmpFontMaterial(tmpText, baseMaterial);
                }
            }
        }

        private void BuildRestoreSets()
        {
            m_restoreRects.Clear();
            m_restoreCanvasGroups.Clear();
            m_restoreGraphics.Clear();
            m_restoreTmpTexts.Clear();

            for (var i = 0; i < m_targets.Count; i++)
            {
                var target = m_targets[i];
                if (target == null || !target.Enabled)
                {
                    continue;
                }

                var rectTransform = target.Target;
                if (rectTransform != null)
                {
                    m_restoreRects.Add(rectTransform);
                }

                ForEachCanvasGroup(target, canvasGroup => m_restoreCanvasGroups.Add(canvasGroup));
                ForEachGraphic(target, graphic => m_restoreGraphics.Add(graphic));
                ForEachTmpText(target, tmpText => m_restoreTmpTexts.Add(tmpText));
            }
        }

        private void ApplyTargetAction(UIInteractionFeedbackTarget target, UIInteractionFeedbackAction action)
        {
            var rectTransform = target.Target;
            var useTween = action.Mode == UIInteractionFeedbackActionMode.Tween
                && action.Type != UIInteractionFeedbackActionType.Material
                && action.Duration > 0f;

            switch (action.Type)
            {
                case UIInteractionFeedbackActionType.Offset:
                    if (rectTransform != null)
                    {
                        if (!m_baseOffsets.TryGetValue(rectTransform, out var baseOffset))
                        {
                            baseOffset = rectTransform.anchoredPosition;
                            m_baseOffsets[rectTransform] = baseOffset;
                        }

                        var to = baseOffset + action.Offset;
                        if (useTween)
                        {
                            var tweenTarget = GetTweenTarget(rectTransform, UIInteractionFeedbackTweenProperty.Offset);
                            EFrameTween.Vector2(rectTransform.anchoredPosition, to, action.Duration, value => rectTransform.anchoredPosition = value, CreateTweenOptions(tweenTarget, action));
                        }
                        else
                        {
                            rectTransform.anchoredPosition = to;
                        }
                    }
                    break;

                case UIInteractionFeedbackActionType.Scale:
                    if (rectTransform != null)
                    {
                        if (!m_baseScales.TryGetValue(rectTransform, out var baseScale))
                        {
                            baseScale = rectTransform.localScale;
                            m_baseScales[rectTransform] = baseScale;
                        }

                        var to = Vector3.Scale(baseScale, action.ScaleMultiplier);
                        if (useTween)
                        {
                            var tweenTarget = GetTweenTarget(rectTransform, UIInteractionFeedbackTweenProperty.Scale);
                            EFrameTween.Vector3(rectTransform.localScale, to, action.Duration, value => rectTransform.localScale = value, CreateTweenOptions(tweenTarget, action));
                        }
                        else
                        {
                            rectTransform.localScale = to;
                        }
                    }
                    break;

                case UIInteractionFeedbackActionType.None:
                    break;

                case UIInteractionFeedbackActionType.Alpha:
                    ApplyAlphaAction(target, action, useTween);
                    break;

                case UIInteractionFeedbackActionType.Color:
                    ForEachGraphic(target, graphic =>
                    {
                        if (!m_baseColors.TryGetValue(graphic, out var baseColor))
                        {
                            baseColor = graphic.color;
                            m_baseColors[graphic] = baseColor;
                        }

                        var to = MultiplyColor(baseColor, action.ColorMultiplier);
                        if (useTween)
                        {
                            var tweenTarget = GetTweenTarget(graphic, UIInteractionFeedbackTweenProperty.Color);
                            EFrameTween.Color(graphic.color, to, action.Duration, value => graphic.color = value, CreateTweenOptions(tweenTarget, action));
                        }
                        else
                        {
                            graphic.color = to;
                        }
                    });
                    break;

                case UIInteractionFeedbackActionType.Material:
                    ApplyMaterialAction(target, action.Material);
                    break;

                case UIInteractionFeedbackActionType.Active:
                    if (rectTransform != null)
                    {
                        rectTransform.gameObject.SetActive(action.Active);
                    }
                    break;
            }
        }

        private void ApplyAlphaAction(UIInteractionFeedbackTarget target, UIInteractionFeedbackAction action, bool useTween)
        {
            var usedCanvasGroup = false;
            ForEachCanvasGroup(target, canvasGroup =>
            {
                usedCanvasGroup = true;
                if (!m_baseAlphas.TryGetValue(canvasGroup, out var baseAlpha))
                {
                    baseAlpha = canvasGroup.alpha;
                    m_baseAlphas[canvasGroup] = baseAlpha;
                }

                var to = Mathf.Clamp01(baseAlpha * action.Alpha);
                if (useTween)
                {
                    var tweenTarget = GetTweenTarget(canvasGroup, UIInteractionFeedbackTweenProperty.Alpha);
                    EFrameTween.Float(canvasGroup.alpha, to, action.Duration, value => canvasGroup.alpha = value, CreateTweenOptions(tweenTarget, action));
                }
                else
                {
                    canvasGroup.alpha = to;
                }
            });

            if (usedCanvasGroup)
            {
                return;
            }

            ForEachGraphic(target, graphic =>
            {
                if (!m_baseColors.TryGetValue(graphic, out var baseColor))
                {
                    baseColor = graphic.color;
                    m_baseColors[graphic] = baseColor;
                }

                var to = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(baseColor.a * action.Alpha));
                if (useTween)
                {
                    var tweenTarget = GetTweenTarget(graphic, UIInteractionFeedbackTweenProperty.Alpha);
                    EFrameTween.Color(graphic.color, to, action.Duration, value => graphic.color = value, CreateTweenOptions(tweenTarget, action));
                }
                else
                {
                    graphic.color = to;
                }
            });
        }

        private UIInteractionFeedbackTweenTarget GetTweenTarget(UnityEngine.Object target, UIInteractionFeedbackTweenProperty property)
        {
            var key = new UIInteractionFeedbackTweenKey(target, property);
            if (!m_tweenTargets.TryGetValue(key, out var tweenTarget))
            {
                tweenTarget = new UIInteractionFeedbackTweenTarget(target, property);
                m_tweenTargets[key] = tweenTarget;
            }

            return tweenTarget;
        }

        private UIInteractionFeedbackTweenTarget FindTweenTarget(UnityEngine.Object target, UIInteractionFeedbackTweenProperty property)
        {
            return m_tweenTargets.TryGetValue(new UIInteractionFeedbackTweenKey(target, property), out var tweenTarget) ? tweenTarget : null;
        }

        private void KillTween(UnityEngine.Object target, UIInteractionFeedbackTweenProperty property)
        {
            var tweenTarget = FindTweenTarget(target, property);
            if (tweenTarget != null)
            {
                EFrameTween.Kill(tweenTarget);
            }
        }

        private static EFrameTweenOptions CreateTweenOptions(object target, UIInteractionFeedbackAction action)
        {
            return new EFrameTweenOptions
            {
                Target = target,
                Ease = action.Ease,
                IgnoreTimeScale = true
            };
        }

        private void ApplyMaterialAction(UIInteractionFeedbackTarget target, Material material)
        {
            ForEachGraphic(target, graphic =>
            {
                if (!m_baseGraphicMaterials.ContainsKey(graphic))
                {
                    m_baseGraphicMaterials[graphic] = graphic.material;
                }

                graphic.material = material;
            });

            ForEachTmpText(target, tmpText =>
            {
                if (!m_baseTextMaterials.ContainsKey(tmpText))
                {
                    m_baseTextMaterials[tmpText] = GetTmpFontMaterial(tmpText);
                }

                SetTmpFontMaterial(tmpText, material);
            });
        }

        private static void ForEachGraphic(UIInteractionFeedbackTarget target, Action<Graphic> action)
        {
            if (target == null || target.Target == null || action == null)
            {
                return;
            }

            if (target.IncludeChildren)
            {
                var graphics = target.Target.GetComponentsInChildren<Graphic>(true);
                for (var i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] != null)
                    {
                        action(graphics[i]);
                    }
                }

                return;
            }

            var graphic = target.Target.GetComponent<Graphic>();
            if (graphic != null)
            {
                action(graphic);
            }
        }

        private static void ForEachCanvasGroup(UIInteractionFeedbackTarget target, Action<CanvasGroup> action)
        {
            if (target == null || target.Target == null || action == null)
            {
                return;
            }

            if (target.IncludeChildren)
            {
                var canvasGroups = target.Target.GetComponentsInChildren<CanvasGroup>(true);
                for (var i = 0; i < canvasGroups.Length; i++)
                {
                    if (canvasGroups[i] != null)
                    {
                        action(canvasGroups[i]);
                    }
                }

                return;
            }

            var canvasGroup = target.Target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                action(canvasGroup);
            }
        }

        private static void ForEachTmpText(UIInteractionFeedbackTarget target, Action<Component> action)
        {
            if (target == null || target.Target == null || action == null)
            {
                return;
            }

            if (target.IncludeChildren)
            {
                var components = target.Target.GetComponentsInChildren<Component>(true);
                for (var i = 0; i < components.Length; i++)
                {
                    if (IsTmpText(components[i]))
                    {
                        action(components[i]);
                    }
                }

                return;
            }

            var targetComponents = target.Target.GetComponents<Component>();
            for (var i = 0; i < targetComponents.Length; i++)
            {
                if (IsTmpText(targetComponents[i]))
                {
                    action(targetComponents[i]);
                }
            }
        }

        private static bool IsTmpText(Component component)
        {
            if (component == null)
            {
                return false;
            }

            var type = component.GetType();
            while (type != null)
            {
                if (type.FullName == "TMPro.TMP_Text")
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static Material GetTmpFontMaterial(Component tmpText)
        {
            var property = tmpText.GetType().GetProperty("fontSharedMaterial");
            return property != null ? property.GetValue(tmpText) as Material : null;
        }

        private static void SetTmpFontMaterial(Component tmpText, Material material)
        {
            var property = tmpText.GetType().GetProperty("fontSharedMaterial");
            if (property != null && property.CanWrite)
            {
                property.SetValue(tmpText, material);
            }
        }

        private static Color MultiplyColor(Color baseColor, Color multiplier)
        {
            return new Color(
                Mathf.Clamp01(baseColor.r * multiplier.r),
                Mathf.Clamp01(baseColor.g * multiplier.g),
                Mathf.Clamp01(baseColor.b * multiplier.b),
                Mathf.Clamp01(baseColor.a * multiplier.a));
        }

        private void CaptureBaseValues()
        {
            CaptureTargetBaseValues();
            CaptureEnableBaseValues();
        }

        private void ClearBaseValues()
        {
            m_baseScales.Clear();
            m_baseOffsets.Clear();
            m_baseColors.Clear();
            m_baseAlphas.Clear();
            m_baseActiveStates.Clear();
            m_baseGraphicMaterials.Clear();
            m_baseTextMaterials.Clear();
        }

        private void CaptureTargetBaseValues()
        {
            for (var i = 0; i < m_targets.Count; i++)
            {
                var target = m_targets[i];
                if (target == null)
                {
                    continue;
                }

                if (target.Target == null)
                {
                    continue;
                }

                if (!m_baseOffsets.ContainsKey(target.Target))
                {
                    m_baseOffsets[target.Target] = target.Target.anchoredPosition;
                }

                if (!m_baseScales.ContainsKey(target.Target))
                {
                    m_baseScales[target.Target] = target.Target.localScale;
                }

                if (!m_baseActiveStates.ContainsKey(target.Target.gameObject))
                {
                    m_baseActiveStates[target.Target.gameObject] = target.Target.gameObject.activeSelf;
                }

                ForEachCanvasGroup(target, canvasGroup =>
                {
                    if (!m_baseAlphas.ContainsKey(canvasGroup))
                    {
                        m_baseAlphas[canvasGroup] = canvasGroup.alpha;
                    }
                });

                ForEachGraphic(target, graphic =>
                {
                    if (!m_baseColors.ContainsKey(graphic))
                    {
                        m_baseColors[graphic] = graphic.color;
                    }

                    if (!m_baseGraphicMaterials.ContainsKey(graphic))
                    {
                        m_baseGraphicMaterials[graphic] = graphic.material;
                    }
                });

                ForEachTmpText(target, tmpText =>
                {
                    if (!m_baseTextMaterials.ContainsKey(tmpText))
                    {
                        m_baseTextMaterials[tmpText] = GetTmpFontMaterial(tmpText);
                    }
                });
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

    public enum UIInteractionVisualState
    {
        Normal,
        Hover,
        Focus,
        Pressed,
        Checked,
        Disabled
    }

    internal enum UIInteractionFeedbackTweenProperty
    {
        Offset,
        Scale,
        Alpha,
        Color
    }

    internal readonly struct UIInteractionFeedbackTweenKey : IEquatable<UIInteractionFeedbackTweenKey>
    {
        private readonly UnityEngine.Object m_target;
        private readonly UIInteractionFeedbackTweenProperty m_property;

        public UIInteractionFeedbackTweenKey(UnityEngine.Object target, UIInteractionFeedbackTweenProperty property)
        {
            m_target = target;
            m_property = property;
        }

        public bool Equals(UIInteractionFeedbackTweenKey other)
        {
            return m_target == other.m_target && m_property == other.m_property;
        }

        public override bool Equals(object obj)
        {
            return obj is UIInteractionFeedbackTweenKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_target != null ? m_target.GetInstanceID() : 0) * 397) ^ (int)m_property;
            }
        }
    }

    internal sealed class UIInteractionFeedbackTweenTarget
    {
        public readonly UnityEngine.Object Target;
        public readonly UIInteractionFeedbackTweenProperty Property;

        public UIInteractionFeedbackTweenTarget(UnityEngine.Object target, UIInteractionFeedbackTweenProperty property)
        {
            Target = target;
            Property = property;
        }
    }

    [Serializable]
    public sealed class UIInteractionFeedbackTarget
    {
        public bool Enabled = true;
        public UIInteractionFeedbackTargetRole Role = UIInteractionFeedbackTargetRole.Content;
        public RectTransform Target;
        public string TargetPath;
        public bool IncludeChildren = true;
        public int SelectedTab;
        public bool Foldout = true;
        public List<UIInteractionFeedbackAction> Actions = new();

        public static UIInteractionFeedbackTarget Content(RectTransform target, string targetPath)
        {
            return new UIInteractionFeedbackTarget
            {
                Role = UIInteractionFeedbackTargetRole.Content,
                Target = target,
                TargetPath = targetPath,
                IncludeChildren = true,
                Actions =
                {
                    UIInteractionFeedbackAction.PressedMove()
                }
            };
        }

        public static UIInteractionFeedbackTarget Selected(RectTransform target, string targetPath)
        {
            return new UIInteractionFeedbackTarget
            {
                Role = UIInteractionFeedbackTargetRole.SelectedVisual,
                Target = target,
                TargetPath = targetPath,
                IncludeChildren = false,
                Actions =
                {
                    UIInteractionFeedbackAction.SelectedActive()
                }
            };
        }
    }

    public enum UIInteractionFeedbackTargetRole
    {
        Custom,
        Content,
        Text,
        Icon,
        Glow,
        SelectedVisual
    }

    [Serializable]
    public sealed class UIInteractionFeedbackAction
    {
        public bool Enabled = true;
        public UIInteractionFeedbackStateMask States = UIInteractionFeedbackStateMask.Pressed;
        public UIInteractionFeedbackActionType Type = UIInteractionFeedbackActionType.None;
        public UIInteractionFeedbackActionMode Mode = UIInteractionFeedbackActionMode.Immediate;
        public EFrameEase Ease = EFrameEase.OutQuad;
        public float Duration = 0.15f;
        public Vector2 Offset;
        public Vector3 ScaleMultiplier = Vector3.one;
        [Range(0f, 1f)] public float Alpha = 1f;
        public Color ColorMultiplier = Color.white;
        public Material Material;
        public bool Active = true;

        public static UIInteractionFeedbackAction PressedMove()
        {
            return new UIInteractionFeedbackAction
            {
                States = UIInteractionFeedbackStateMask.Pressed,
                Type = UIInteractionFeedbackActionType.Offset,
                Offset = new Vector2(0f, -2f)
            };
        }

        public static UIInteractionFeedbackAction DisabledTint()
        {
            return new UIInteractionFeedbackAction
            {
                States = UIInteractionFeedbackStateMask.Disabled,
                Type = UIInteractionFeedbackActionType.Color,
                ColorMultiplier = new Color(0.65f, 0.65f, 0.65f, 0.65f)
            };
        }

        public static UIInteractionFeedbackAction SelectedActive()
        {
            return new UIInteractionFeedbackAction
            {
                States = UIInteractionFeedbackStateMask.Checked,
                Type = UIInteractionFeedbackActionType.Active,
                Active = true
            };
        }

        public bool Matches(UIInteractionVisualState visualState)
        {
            return (States & ToMask(visualState)) != 0;
        }

        private static UIInteractionFeedbackStateMask ToMask(UIInteractionVisualState visualState)
        {
            return visualState switch
            {
                UIInteractionVisualState.Hover => UIInteractionFeedbackStateMask.Hover,
                UIInteractionVisualState.Focus => UIInteractionFeedbackStateMask.Focus,
                UIInteractionVisualState.Pressed => UIInteractionFeedbackStateMask.Pressed,
                UIInteractionVisualState.Checked => UIInteractionFeedbackStateMask.Checked,
                UIInteractionVisualState.Disabled => UIInteractionFeedbackStateMask.Disabled,
                _ => UIInteractionFeedbackStateMask.Normal
            };
        }
    }

    [Flags]
    public enum UIInteractionFeedbackStateMask
    {
        Normal = 1 << 0,
        Hover = 1 << 1,
        Focus = 1 << 2,
        Pressed = 1 << 3,
        Checked = 1 << 4,
        Disabled = 1 << 5
    }

    public enum UIInteractionFeedbackActionType
    {
        None,
        Scale,
        Offset,
        Color,
        Material,
        Alpha,
        Active
    }

    public enum UIInteractionFeedbackActionMode
    {
        Immediate,
        Tween
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
