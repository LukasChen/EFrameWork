using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.Audio
{
    /// <summary>
    /// 音效事件配置资源
    /// </summary>
    [CreateAssetMenu(fileName = "AudioEventConfig", menuName = "EFrame/Audio Event Config")]
    public class AudioEventConfigAsset : ScriptableObject
    {
        /// <summary>
        /// 所有事件音效配置项
        /// </summary>
        public List<AudioEventConfigItem> ConfigItems = new List<AudioEventConfigItem>();

        /// <summary>
        /// 根据事件类型名查找配置
        /// </summary>
        public AudioEventConfigItem FindByEventType(string eventTypeName)
        {
            return ConfigItems.Find(item => item.EventTypeName == eventTypeName);
        }

        /// <summary>
        /// 添加或更新配置项
        /// </summary>
        public void AddOrUpdateItem(AudioEventConfigItem item)
        {
            var existing = FindByEventType(item.EventTypeName);
            if (existing != null)
            {
                int index = ConfigItems.IndexOf(existing);
                ConfigItems[index] = item;
            }
            else
            {
                ConfigItems.Add(item);
            }
        }

        /// <summary>
        /// 移除配置项
        /// </summary>
        public bool RemoveItem(string eventTypeName)
        {
            var item = FindByEventType(eventTypeName);
            if (item != null)
            {
                ConfigItems.Remove(item);
                return true;
            }
            return false;
        }
    }
}
