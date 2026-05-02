using UnityEngine;

namespace EFrame.Runtime.Utils
{
    public static class GameObjectExtends
    {
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        //移除所有子节点
        public static void RemoveAllChildren(this GameObject go)
        {
            if (go == null) return;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(go.transform.GetChild(i).gameObject);
            }
        }
    }
}
