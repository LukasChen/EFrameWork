using EFrame.Runtime.UI;
using UnityEngine;

namespace EFrame.Runtime
{
    [CreateAssetMenu(fileName = "EFrameSettings", menuName = "EFrame/Settings")]
    public sealed class EFrameSettings : ScriptableObject
    {
        [Header("Screen Fit")]
        public Vector2Int DesignSize = new(1080, 1920);
        public ScreenFitMode FitMode = ScreenFitMode.FitWidth;
        public bool EnableScreenFitDebugLog;
    }
}
