using UnityEngine;

namespace EFrameWork.Runtime
{
    [CreateAssetMenu(fileName = "AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public bool Debug;
        public bool HotFixEnable;
        public bool LogEnable;
        public string BaseUrl;
        public string NewestVersion;
        public string MinVersion;
        public bool ForceUpdateApk;
        public long PatchCode;

        public void CopyFrom(AppConfig appConfig)
        {
            Debug = appConfig.Debug;
            HotFixEnable = appConfig.HotFixEnable;
            LogEnable = appConfig.LogEnable;
            BaseUrl = appConfig.BaseUrl;
            NewestVersion = appConfig.NewestVersion;
            MinVersion = appConfig.MinVersion;
            ForceUpdateApk = appConfig.ForceUpdateApk;
            PatchCode = appConfig.PatchCode;
        }
    }
}
