using System.Collections.Generic;
using System.Reflection;
using EFramework.Extensions.UI.Extras.Interaction;
using EFramework.Runtime.Tween;
using UnityEngine;
using UnityEngine.UI;

namespace GameApp.Modules.EFrameExtensionShowcase.UI
{
    internal static class ShowcaseFeedbackInstaller
    {
        private static readonly FieldInfo SelectableField = GetField("m_selectable");
        private static readonly FieldInfo AutoDriveSelectableField = GetField("m_autoDriveSelectable");
        private static readonly FieldInfo AutoDriveToggleCheckedField = GetField("m_autoDriveToggleChecked");
        private static readonly FieldInfo RestorePressedOnPointerExitField = GetField("m_restorePressedOnPointerExit");
        private static readonly FieldInfo SubmitPressedDurationField = GetField("m_submitPressedDuration");
        private static readonly FieldInfo UseTargetFeedbackField = GetField("m_useTargetFeedback");
        private static readonly FieldInfo AutoSetupTargetsField = GetField("m_autoSetupTargets");
        private static readonly FieldInfo TargetsField = GetField("m_targets");

        public static void InstallButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Configure(
                button.gameObject,
                button.GetComponent<RectTransform>(),
                FindChild(button.transform, "LabelText") as RectTransform ?? FindChild(button.transform, "Text") as RectTransform,
                button,
                new Vector3(1.025f, 1.025f, 1f),
                new Vector3(0.985f, 0.985f, 1f),
                new Vector2(0f, -4f),
                new Vector2(8f, 0f));
        }

        public static void InstallItem(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            var root = item.GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            Configure(
                item,
                root,
                FindChild(item.transform, "Label") as RectTransform ?? FindChild(item.transform, "Text") as RectTransform,
                null,
                new Vector3(1.012f, 1.012f, 1f),
                new Vector3(0.99f, 0.99f, 1f),
                new Vector2(0f, -3f),
                Vector2.zero);
        }

        public static void SetChecked(Button button, bool isChecked)
        {
            var feedback = button != null ? button.GetComponent<UIInteractionFeedback>() : null;
            if (feedback != null)
            {
                feedback.SetChecked(isChecked);
            }
        }

        private static void Configure(
            GameObject owner,
            RectTransform root,
            RectTransform label,
            Selectable selectable,
            Vector3 hoverScale,
            Vector3 pressedScale,
            Vector2 pressedOffset,
            Vector2 checkedOffset)
        {
            if (owner == null || root == null)
            {
                return;
            }

            var feedback = owner.GetComponent<UIInteractionFeedback>();
            if (feedback == null)
            {
                feedback = owner.AddComponent<UIInteractionFeedback>();
            }

            Set(SelectableField, feedback, selectable);
            Set(AutoDriveSelectableField, feedback, true);
            Set(AutoDriveToggleCheckedField, feedback, true);
            Set(RestorePressedOnPointerExitField, feedback, true);
            Set(SubmitPressedDurationField, feedback, 0.08f);
            Set(UseTargetFeedbackField, feedback, true);
            Set(AutoSetupTargetsField, feedback, false);

            var targets = new List<UIInteractionFeedbackTarget>
            {
                new()
                {
                    Enabled = true,
                    Role = UIInteractionFeedbackTargetRole.Custom,
                    Target = root,
                    TargetPath = string.Empty,
                    IncludeChildren = false,
                    Actions =
                    {
                        Scale(UIInteractionFeedbackStateMask.Hover, hoverScale, 0.12f),
                        Scale(UIInteractionFeedbackStateMask.Pressed, pressedScale, 0.08f),
                        Scale(UIInteractionFeedbackStateMask.Checked, new Vector3(1.018f, 1.018f, 1f), 0.12f),
                        Color(UIInteractionFeedbackStateMask.Hover, new Color(1.2f, 1.2f, 1.2f, 1f), 0.12f),
                        Color(UIInteractionFeedbackStateMask.Pressed, new Color(0.78f, 0.78f, 0.78f, 1f), 0.08f),
                        Color(UIInteractionFeedbackStateMask.Checked, new Color(1.28f, 1.18f, 0.88f, 1f), 0.12f),
                        Alpha(UIInteractionFeedbackStateMask.Disabled, 0.45f, 0.12f)
                    }
                }
            };

            if (label != null)
            {
                targets.Add(new UIInteractionFeedbackTarget
                {
                    Enabled = true,
                    Role = UIInteractionFeedbackTargetRole.Text,
                    Target = label,
                    TargetPath = GetRelativePath(root, label),
                    IncludeChildren = false,
                    Actions =
                    {
                        Offset(UIInteractionFeedbackStateMask.Pressed, pressedOffset, 0.08f),
                        Offset(UIInteractionFeedbackStateMask.Checked, checkedOffset, 0.12f),
                        Color(UIInteractionFeedbackStateMask.Checked, new Color(1.12f, 1.02f, 0.82f, 1f), 0.12f)
                    }
                });
            }

            Set(TargetsField, feedback, targets);
            feedback.Refresh();
        }

        private static UIInteractionFeedbackAction Scale(UIInteractionFeedbackStateMask states, Vector3 scale, float duration)
        {
            return new UIInteractionFeedbackAction
            {
                States = states,
                Type = UIInteractionFeedbackActionType.Scale,
                Mode = UIInteractionFeedbackActionMode.Tween,
                Ease = EFrameEase.OutQuad,
                Duration = duration,
                ScaleMultiplier = scale
            };
        }

        private static UIInteractionFeedbackAction Offset(UIInteractionFeedbackStateMask states, Vector2 offset, float duration)
        {
            return new UIInteractionFeedbackAction
            {
                States = states,
                Type = UIInteractionFeedbackActionType.Offset,
                Mode = UIInteractionFeedbackActionMode.Tween,
                Ease = EFrameEase.OutQuad,
                Duration = duration,
                Offset = offset
            };
        }

        private static UIInteractionFeedbackAction Color(UIInteractionFeedbackStateMask states, Color multiplier, float duration)
        {
            return new UIInteractionFeedbackAction
            {
                States = states,
                Type = UIInteractionFeedbackActionType.Color,
                Mode = UIInteractionFeedbackActionMode.Tween,
                Ease = EFrameEase.OutQuad,
                Duration = duration,
                ColorMultiplier = multiplier
            };
        }

        private static UIInteractionFeedbackAction Alpha(UIInteractionFeedbackStateMask states, float alpha, float duration)
        {
            return new UIInteractionFeedbackAction
            {
                States = states,
                Type = UIInteractionFeedbackActionType.Alpha,
                Mode = UIInteractionFeedbackActionMode.Tween,
                Ease = EFrameEase.OutQuad,
                Duration = duration,
                Alpha = alpha
            };
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null || root == target)
            {
                return string.Empty;
            }

            var parts = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            return current == root ? string.Join("/", parts) : string.Empty;
        }

        private static FieldInfo GetField(string name)
        {
            return typeof(UIInteractionFeedback).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void Set(FieldInfo field, object target, object value)
        {
            field?.SetValue(target, value);
        }
    }
}
