using DG.Tweening;
using EFrameWork.Runtime.Asset;
using EFrameWork.Runtime.UI;
using EFrameWork.Runtime.UI.UIHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFrameWork.Runtime.Utils
{
    public class UIUtils
    {
        public static Transform GetUILayer(UILayer layer)
        {
            return EFrame.UI.UILayer(layer);
        }



        public static void AddClickListener(Button go, UnityEngine.Events.UnityAction action, bool removeOld = true)
        {
            if (IsNull(go))
            {
                return;
            }
            if (removeOld)
            {
                go.onClick.RemoveAllListeners();
            }
            go.onClick.AddListener(action);
        }

        public static void AddValueChangedListener(Toggle go, UnityEngine.Events.UnityAction<bool> action, bool? initValue = null)
        {
            if (IsNull(go))
            {
                return;
            }
            if (initValue != null)
            {
                go.isOn = initValue.Value;
            }
            go.onValueChanged.AddListener(action);
        }

        public static void SetActive(GameObject go, bool value)
        {
            if (IsNull(go))
            {
                return;
            }
            go.SetActive(value);
        }
        public static void SetActive(Component go, bool value)
        {
            if (IsNull(go))
            {
                return;
            }
            go.gameObject.SetActive(value);
        }
        public static bool IsNull(GameObject go)
        {
            return go == null || go.Equals(null);
        }
        public static bool IsNull(Component go)
        {
            return go == null || go.Equals(null);
        }

        public static bool IsValid(GameObject go)
        {
            return go != null && !go.Equals(null);
        }

        public static bool IsValid(Component go)
        {
            return go != null && !go.Equals(null);
        }

        public static void SetSprite(Image go, string value, bool useNative = true)
        {
            if (IsNull(go))
            {
                return;
            }
            var sprite = AssetManager.LoadAsset<Sprite>(value);
            go.sprite = sprite;
            if (useNative)
            {
                go.SetNativeSize();
            }
        }
        public static void SetText(TextMeshProUGUI go, string value)
        {
            if (IsNull(go))
            {
                return;
            }

            go.text = value;
        }
        public static void SetFullStretch(RectTransform rectTransform)
        {
            // 设置锚点模式为全屏拉伸
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            // 重置偏移量为0
            rectTransform.offsetMin = Vector2.zero; // 相当于left和bottom
            rectTransform.offsetMax = Vector2.zero; // 相当于right和top
        }

        public static Vector3 World2UGUI(RectTransform parentUI, Vector3 worldPosition, Camera sceneCamera)
        {
            var screenPoint = sceneCamera.WorldToScreenPoint(worldPosition);
            Vector3 uiWorldPosition = Vector3.zero;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parentUI, screenPoint, EFrame.UI.UICamera, out uiWorldPosition);
            return uiWorldPosition;
        }

        /// <summary>
        /// 将世界坐标转换为相对于UIRoot的UI坐标
        /// </summary>
        /// <param name="worldScenePosition">世界坐标位置</param>
        /// <param name="uiRoot">UI根节点的RectTransform,如果为null则使用默认UIRoot</param>
        /// <returns>相对于UIRoot的本地坐标</returns>
        public static Vector2 WorldToUIPosition(Vector3 worldScenePosition, RectTransform uiRoot = null)
        {
            if (uiRoot == null)
            {
                uiRoot = EFrame.UI.UILayer(UILayer.QuiPanel) as RectTransform;
            }

            // 将世界坐标转换为屏幕坐标
            Vector3 screenPoint = EFrame.SceneCamera.WorldToScreenPoint(worldScenePosition);

            // 将屏幕坐标转换为UI本地坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiRoot,
                screenPoint,
                EFrame.UI.UICamera,
                out localPoint
            );

            return localPoint;
        }

        /// <summary>
        /// 将世界坐标转换为UI世界坐标(用于直接设置UI元素的世界坐标)
        /// </summary>
        /// <param name="worldScenePosition">世界坐标位置</param>
        /// <param name="uiRoot">UI根节点的RectTransform</param>
        /// <returns>UI世界坐标</returns>
        public static Vector3 SceneToUIPosition(Vector3 worldScenePosition, RectTransform uiRoot = null)
        {
            if (uiRoot == null)
            {
                uiRoot = EFrame.UI.UILayer(UILayer.QuiPanel) as RectTransform;
            }

            // 将世界坐标转换为屏幕坐标
            Vector3 screenPoint = EFrame.SceneCamera.WorldToScreenPoint(worldScenePosition);

            // 将屏幕坐标转换为UI世界坐标
            Vector3 worldUIPosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                uiRoot,
                screenPoint,
                EFrame.UI.UICamera,
                out worldUIPosition
            );

            return worldUIPosition;
        }

        public static Vector3 UIToScenePosition(Vector3 uiPosition, float zOffset = 0)
        {
            var screenPoint = EFrame.UICamera.WorldToScreenPoint(uiPosition);
            var worldPosition = EFrame.SceneCamera.ScreenToWorldPoint(screenPoint);
            worldPosition.z = zOffset;
            return worldPosition;
        }

        public static List<Transform> GetUIArrayOrAdd(RectTransform parent, int count)
        {
            List<Transform> children = new List<Transform>();

            // 获取现有子对象
            GameObject src = null;
            for (int j = 0; j < parent.childCount; j++)
            {
                var child = parent.GetChild(j);
                children.Add(child);
                if (src == null)
                {
                    src = child.gameObject;
                }
            }

            // 补充不足的子对象
            while (children.Count < count)
            {
                GameObject newObj = GameObject.Instantiate(src, parent);
                RectTransform rt = newObj.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                children.Add(rt);
            }

            // 移除多余子对象
            while (children.Count > count)
            {
                Transform last = children[children.Count - 1];
                children.RemoveAt(children.Count - 1);
                last.gameObject.SetActive(false);
            }

            return children;
        }



        public static void InitUIMask(Transform view)
        {
            var ui = view.Find("buttonAniMask");
            SetActive(ui, false);
        }

        /// <summary>
        /// 检测点击按钮外区域
        /// </summary>
        /// <param name="button"></param>
        /// <param name="outClickAction"></param>
        public static void OutClickDetector(Button button, Action outClickAction)
        {
            EFrame.Coroutine.StartCoroutine(OutClickDetectorCoroutine(button, outClickAction));
        }

        private static IEnumerator OutClickDetectorCoroutine(Button button, Action outClickAction)
        {
            if (button == null) yield break;
            var rectTransform = button.transform as RectTransform;
            if (rectTransform == null) yield break;

            while (true)
            {
                if (button == null || !button.gameObject.activeInHierarchy) yield break;
                if (QInput.GetPrimaryPointerDown() && QInput.TryGetPrimaryPointerPosition(out var pointerPosition))
                {
                    bool inside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition, EFrame.UICamera);
                    if (!inside)
                    {
                        outClickAction?.Invoke();
                        yield break;
                    }
                }
                yield return null;
            }
        }

        public static void FadeInAll(Transform view, float duration = 0.3f, TweenCallback onComplete = null)
        {
            view.TryGetComponent<CanvasGroup>(out var canvasGroup);
            if (canvasGroup == null)
            {
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.DOFade(1, duration).OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
            }
            else
            {
                canvasGroup.DOFade(1, duration).OnComplete(onComplete);
            }

        }

        public static void FadeOutAll(Transform view, float duration = 0.3f, TweenCallback onComplete = null)
        {
            view.TryGetComponent<CanvasGroup>(out var canvasGroup);
            if (canvasGroup == null)
            {
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.DOFade(0, duration).OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
            }
            else
            {
                canvasGroup.DOFade(0, duration).OnComplete(onComplete);
            }

        }


        public static void SetLayer(RectTransform root, int layer, bool children)
        {
            if (root == null) return;
            if (layer < 0 || layer > 31) return;
            if (!children)
            {
                root.gameObject.layer = layer;
                return;
            }
            // 包含未激活对象
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }

        public static void SetRectTransformRelativeToCenter(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(targetRect.rect.center + new Vector2(targetRect.pivot.x * targetRect.rect.width - targetRect.rect.width / 2f, targetRect.pivot.y * targetRect.rect.height - targetRect.rect.height / 2f));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToTop(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0.5f - targetRect.pivot.x), targetRect.rect.height * (1 - targetRect.pivot.y), 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToBottom(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0.5f - targetRect.pivot.x), targetRect.rect.height * -targetRect.pivot.y, 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static void SetRectTransformRelativeToLeft(RectTransform currentRect, RectTransform targetRect, Vector2 offset)
        {
            if (targetRect == null || currentRect == null || currentRect.parent == null) return;
            Vector3 worldPos = targetRect.TransformPoint(new Vector3(targetRect.rect.width * (0 - targetRect.pivot.x), targetRect.rect.height * (0.5f - targetRect.pivot.y), 0));
            Vector3 localPos = currentRect.parent.InverseTransformPoint(worldPos);
            currentRect.anchoredPosition = (Vector2)localPos + offset;
        }

        public static Vector3 GetRectTransformCenter(RectTransform targetRect)
        {
            if (targetRect == null) return Vector3.zero;

            // 获取RectTransform的世界坐标中心点
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);

            // 计算四个角的中心点
            Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
            return center;
        }


        public static void SetAdjustedFillAmount(Image image, float fillAmount, float startCutout = 0.15f, float endCutout = 0.15f)
        {
            float adjustedFill = (1 - startCutout - endCutout) * fillAmount + startCutout;
            image.fillAmount = Mathf.Clamp01(adjustedFill);
        }
    }
}
