using UnityEngine;

namespace EFramework.Extensions.DebugConsole
{
    public static class EFrameDebugConsole
    {
        private const string PrefabResourcePath = "IngameDebugConsole";

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
    }
}
