using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EFramework.Runtime;
using EFramework.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFramework.Editor.AILoop
{
    public static class EFrameMouseUiSimulator
    {
        private static readonly List<RaycastResult> RaycastResults = new();

        public static async Task<EFrameMouseUiResult> ExecuteAsync(
            EFrameMouseUiRequest request,
            CancellationToken cancellationToken = default)
        {
            request ??= new EFrameMouseUiRequest();

            if (!EditorApplication.isPlaying)
            {
                return Fail("PlayMode is not active.");
            }

            EventSystem eventSystem = ResolveEventSystem();
            if (eventSystem == null)
            {
                return Fail("No EventSystem found. EFrame UI must be initialized or a scene EventSystem must exist.");
            }

            switch (request.Action)
            {
                case EFrameMouseUiAction.Click:
                    return await ClickAsync(request, eventSystem, cancellationToken);
                case EFrameMouseUiAction.LongPress:
                    return await LongPressAsync(request, eventSystem, cancellationToken);
                case EFrameMouseUiAction.Drag:
                    return await DragAsync(request, eventSystem, cancellationToken);
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Action), request.Action, null);
            }
        }

        internal static EventSystem ResolveEventSystem()
        {
            if (EFrame.Initialized && EFrame.Current.UI is QUI qui && qui.EventSystem != null)
            {
                return qui.EventSystem;
            }

            return EventSystem.current;
        }

        internal static RaycastResult? RaycastUi(Vector2 screenPosition, EventSystem eventSystem)
        {
            RaycastResults.Clear();
            PointerEventData pointerData = new(eventSystem)
            {
                position = screenPosition
            };
            eventSystem.RaycastAll(pointerData, RaycastResults);
            if (RaycastResults.Count > 0)
            {
                return RaycastResults[0];
            }

            return RaycastGraphics(screenPosition);
        }

        private static async Task<EFrameMouseUiResult> ClickAsync(
            EFrameMouseUiRequest request,
            EventSystem eventSystem,
            CancellationToken cancellationToken)
        {
            Vector2 inputPos = new(request.X, request.Y);
            Vector2 screenPos = InputToScreen(inputPos);
            RaycastResult? hit = RaycastUi(screenPos, eventSystem);
            PointerEventData pointerData = CreatePointerData(eventSystem, screenPos, request.Button);
            if (hit.HasValue)
            {
                pointerData.pointerCurrentRaycast = hit.Value;
                pointerData.pointerPressRaycast = hit.Value;
            }

            GameObject rawTarget = hit?.gameObject;
            GameObject pressTarget = rawTarget != null
                ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(rawTarget)
                : null;
            GameObject clickTarget = rawTarget != null
                ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(rawTarget)
                : null;

            if (pressTarget != null)
            {
                pointerData.pointerPress = pressTarget;
                pointerData.rawPointerPress = rawTarget;
                ExecuteEvents.ExecuteHierarchy(rawTarget, pointerData, ExecuteEvents.pointerDownHandler);
                await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);
                ExecuteEvents.Execute(pressTarget, pointerData, ExecuteEvents.pointerUpHandler);
            }

            if (clickTarget != null)
            {
                ExecuteEvents.Execute(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);
            }

            string targetName = (clickTarget ?? pressTarget)?.name ?? "";
            return new EFrameMouseUiResult
            {
                Success = true,
                Message = string.IsNullOrEmpty(targetName)
                    ? $"Clicked at ({request.X:F1}, {request.Y:F1}); no UI handler hit."
                    : $"Clicked '{targetName}' at ({request.X:F1}, {request.Y:F1}).",
                HitGameObjectName = targetName,
                X = request.X,
                Y = request.Y
            };
        }

        private static async Task<EFrameMouseUiResult> LongPressAsync(
            EFrameMouseUiRequest request,
            EventSystem eventSystem,
            CancellationToken cancellationToken)
        {
            if (request.Duration <= 0f || float.IsNaN(request.Duration) || float.IsInfinity(request.Duration))
            {
                return Fail($"Duration must be positive, got {request.Duration}.");
            }

            Vector2 inputPos = new(request.X, request.Y);
            Vector2 screenPos = InputToScreen(inputPos);
            RaycastResult? hit = RaycastUi(screenPos, eventSystem);
            PointerEventData pointerData = CreatePointerData(eventSystem, screenPos, request.Button);
            if (hit.HasValue)
            {
                pointerData.pointerCurrentRaycast = hit.Value;
                pointerData.pointerPressRaycast = hit.Value;
            }

            GameObject rawTarget = hit?.gameObject;
            GameObject pressTarget = rawTarget != null
                ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(rawTarget)
                : null;

            if (pressTarget != null)
            {
                pointerData.pointerPress = pressTarget;
                pointerData.rawPointerPress = rawTarget;
                ExecuteEvents.ExecuteHierarchy(rawTarget, pointerData, ExecuteEvents.pointerDownHandler);
            }

            int frames = Mathf.Max(1, Mathf.CeilToInt(request.Duration / Mathf.Max(Time.unscaledDeltaTime, 1f / 60f)));
            try
            {
                await EFrameAiLoopDelay.DelayFrames(frames, cancellationToken);
            }
            finally
            {
                if (pressTarget != null)
                {
                    ExecuteEvents.Execute(pressTarget, pointerData, ExecuteEvents.pointerUpHandler);
                }
            }

            return new EFrameMouseUiResult
            {
                Success = true,
                Message = string.IsNullOrEmpty(pressTarget?.name)
                    ? $"Long-pressed at ({request.X:F1}, {request.Y:F1}); no UI handler hit."
                    : $"Long-pressed '{pressTarget.name}' for {request.Duration:F1}s.",
                HitGameObjectName = pressTarget != null ? pressTarget.name : "",
                X = request.X,
                Y = request.Y
            };
        }

        private static async Task<EFrameMouseUiResult> DragAsync(
            EFrameMouseUiRequest request,
            EventSystem eventSystem,
            CancellationToken cancellationToken)
        {
            if (request.Button != EFrameMouseButton.Left)
            {
                return Fail("uGUI drag simulation only supports the left button.");
            }

            Vector2 inputStart = new(request.FromX, request.FromY);
            Vector2 inputEnd = new(request.X, request.Y);
            Vector2 screenStart = InputToScreen(inputStart);
            Vector2 screenEnd = InputToScreen(inputEnd);
            RaycastResult? hit = RaycastUi(screenStart, eventSystem);
            if (!hit.HasValue)
            {
                return Fail($"No UI element hit at drag start ({request.FromX:F1}, {request.FromY:F1}).");
            }

            GameObject rawTarget = hit.Value.gameObject;
            GameObject dragTarget = ExecuteEvents.GetEventHandler<IDragHandler>(rawTarget);
            if (dragTarget == null)
            {
                return Fail($"UI element '{rawTarget.name}' has no drag handler.");
            }

            PointerEventData pointerData = CreatePointerData(eventSystem, screenStart, EFrameMouseButton.Left);
            pointerData.pointerCurrentRaycast = hit.Value;
            pointerData.pointerPressRaycast = hit.Value;
            pointerData.pointerDrag = dragTarget;
            pointerData.rawPointerPress = rawTarget;

            pointerData.pointerPress = ExecuteEvents.ExecuteHierarchy(
                rawTarget,
                pointerData,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(dragTarget, pointerData, ExecuteEvents.beginDragHandler);
            pointerData.dragging = true;

            try
            {
                await InterpolateDrag(pointerData, dragTarget, screenEnd, request.DragSpeed, cancellationToken);
            }
            finally
            {
                FinalizeDrag(pointerData, dragTarget, eventSystem);
            }

            return new EFrameMouseUiResult
            {
                Success = true,
                Message = $"Dragged '{dragTarget.name}' from ({request.FromX:F1}, {request.FromY:F1}) to ({request.X:F1}, {request.Y:F1}).",
                HitGameObjectName = dragTarget.name,
                X = request.X,
                Y = request.Y
            };
        }

        private static async Task InterpolateDrag(
            PointerEventData pointerData,
            GameObject target,
            Vector2 endPosition,
            float dragSpeed,
            CancellationToken cancellationToken)
        {
            Vector2 start = pointerData.position;
            float distance = Vector2.Distance(start, endPosition);
            float duration = dragSpeed > 0f ? distance / dragSpeed : 0f;
            float startedAt = Time.realtimeSinceStartup;

            do
            {
                await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);
                float t = duration <= 0f ? 1f : Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / duration);
                Vector2 previous = pointerData.position;
                Vector2 current = Vector2.Lerp(start, endPosition, t);
                pointerData.position = current;
                pointerData.delta = current - previous;
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.dragHandler);
                if (t >= 1f)
                {
                    break;
                }
            } while (true);
        }

        private static void FinalizeDrag(PointerEventData pointerData, GameObject target, EventSystem eventSystem)
        {
            RaycastResult? dropHit = RaycastUi(pointerData.position, eventSystem);
            pointerData.pointerCurrentRaycast = dropHit ?? new RaycastResult();

            if (pointerData.pointerPress != null)
            {
                ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);
            }

            if (dropHit.HasValue && dropHit.Value.gameObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(dropHit.Value.gameObject, pointerData, ExecuteEvents.dropHandler);
            }

            pointerData.dragging = false;
            if (target != null)
            {
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.endDragHandler);
            }
        }

        private static PointerEventData CreatePointerData(
            EventSystem eventSystem,
            Vector2 screenPosition,
            EFrameMouseButton button)
        {
            return new PointerEventData(eventSystem)
            {
                position = screenPosition,
                pressPosition = screenPosition,
                button = ToInputButton(button)
            };
        }

        private static PointerEventData.InputButton ToInputButton(EFrameMouseButton button)
        {
            return button switch
            {
                EFrameMouseButton.Right => PointerEventData.InputButton.Right,
                EFrameMouseButton.Middle => PointerEventData.InputButton.Middle,
                _ => PointerEventData.InputButton.Left
            };
        }

        private static Vector2 InputToScreen(Vector2 inputPosition)
        {
            float targetHeight = Handles.GetMainGameViewSize().y;
            return new Vector2(inputPosition.x, targetHeight - inputPosition.y);
        }

        private static RaycastResult? RaycastGraphics(Vector2 screenPosition)
        {
            Graphic best = null;
            int bestSortingOrder = int.MinValue;
            int bestDepth = int.MinValue;
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>();

            foreach (Canvas canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy || canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    continue;
                }

                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

                Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>();
                foreach (Graphic graphic in graphics)
                {
                    if (!IsRaycastCandidate(graphic, screenPosition, camera))
                    {
                        continue;
                    }

                    if (canvas.sortingOrder > bestSortingOrder ||
                        canvas.sortingOrder == bestSortingOrder && graphic.depth > bestDepth)
                    {
                        best = graphic;
                        bestSortingOrder = canvas.sortingOrder;
                        bestDepth = graphic.depth;
                    }
                }
            }

            return best == null
                ? null
                : new RaycastResult { gameObject = best.gameObject, sortingOrder = bestSortingOrder };
        }

        private static bool IsRaycastCandidate(Graphic graphic, Vector2 screenPosition, Camera camera)
        {
            return graphic != null &&
                   graphic.gameObject.activeInHierarchy &&
                   graphic.enabled &&
                   graphic.raycastTarget &&
                   graphic.depth != -1 &&
                   !graphic.canvasRenderer.cull &&
                   RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPosition, camera) &&
                   graphic.Raycast(screenPosition, camera);
        }

        private static EFrameMouseUiResult Fail(string message)
        {
            return new EFrameMouseUiResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
