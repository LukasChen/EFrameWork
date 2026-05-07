using UnityEngine;
using UnityEngine.U2D;

namespace EFramework.Extensions.DebugConsole
{
    public static class EFrameDebugConsole
    {
        private const string PrefabResourcePath = "IngameDebugConsole";
        private const string SpriteAtlasResourcePath = "IngameDebugConsoleSpriteAtlas";
        private static SpriteAtlas s_spriteAtlas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
            SpriteAtlasManager.atlasRequested += OnAtlasRequested;
        }

        public static IngameDebugConsole.DebugLogManager Show()
        {
            var manager = IngameDebugConsole.DebugLogManager.Instance;
            if (manager == null)
            {
                var prefab = Resources.Load<IngameDebugConsole.DebugLogManager>(PrefabResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning("[EFrameDebugConsole] IngameDebugConsole prefab resource was not found.");
                    return null;
                }

                manager = Object.Instantiate(prefab);
                manager.name = "IngameDebugConsole";
            }

            manager.gameObject.SetActive(true);
            manager.ShowLogWindow();
            return manager;
        }

        private static void OnAtlasRequested(string tag, System.Action<SpriteAtlas> callback)
        {
            if (!string.Equals(tag, SpriteAtlasResourcePath, System.StringComparison.Ordinal))
            {
                return;
            }

            s_spriteAtlas ??= Resources.Load<SpriteAtlas>(SpriteAtlasResourcePath);
            if (s_spriteAtlas != null)
            {
                callback(s_spriteAtlas);
            }
        }
    }
}
