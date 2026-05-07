using EFramework.Runtime;
using UnityEngine;

namespace EFramework.Runtime.UI
{
    public static class UIPositionUtils
    {
        public static Vector3 WorldToUIWorldPosition(RectTransform parentUI, Vector3 worldPosition, Camera sceneCamera)
        {
            if (parentUI == null || sceneCamera == null)
            {
                return Vector3.zero;
            }

            var screenPoint = sceneCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parentUI, screenPoint, EFrame.UICamera, out var uiWorldPosition);
            return uiWorldPosition;
        }

        public static Vector2 WorldToUILocalPosition(Vector3 worldScenePosition, RectTransform uiRoot = null)
        {
            uiRoot ??= EFrame.UI.UILayer(UILayer.QuiPanel);
            var screenPoint = EFrame.SceneCamera.WorldToScreenPoint(worldScenePosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiRoot,
                screenPoint,
                EFrame.UICamera,
                out var localPoint);

            return localPoint;
        }

        public static Vector3 SceneToUIPosition(Vector3 worldScenePosition, RectTransform uiRoot = null)
        {
            uiRoot ??= EFrame.UI.UILayer(UILayer.QuiPanel);
            var screenPoint = EFrame.SceneCamera.WorldToScreenPoint(worldScenePosition);

            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                uiRoot,
                screenPoint,
                EFrame.UICamera,
                out var worldUIPosition);

            return worldUIPosition;
        }

        public static Vector3 UIToScenePosition(Vector3 uiPosition, float zOffset = 0)
        {
            var screenPoint = EFrame.UICamera.WorldToScreenPoint(uiPosition);
            var worldPosition = EFrame.SceneCamera.ScreenToWorldPoint(screenPoint);
            worldPosition.z = zOffset;
            return worldPosition;
        }
    }
}
