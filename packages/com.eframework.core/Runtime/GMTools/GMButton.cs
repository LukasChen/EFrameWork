using EFrame.Runtime.Event;
using EFrame.Runtime.Utils;
using System;
using System.Collections;
using System.Reflection;
using EFrame.Runtime.Asset;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EFrame.Runtime.GMTools
{
    /// <summary>
    /// GM按钮
    /// 用于打开GM调试控制台
    /// 自定义GMView 预制体请配置为 Addressables，并确保 Addressables Key 可用
    /// </summary>

    public class GMButton : MonoBehaviour
    {
        public static GMButton Instance { get; private set; }
        public static string GmButtonAddressKey { get; set; } = string.Empty;
        public static string GmViewAddressKey { get; set; } = string.Empty;

        public static void Initialize()
        {
            var gmButtonPrefab = LoadAddressableAsset<GameObject>(GmButtonAddressKey);
            if (gmButtonPrefab == null)
            {
                Debug.LogWarning("[GMButton] GMButton prefab not found. GM tools initialization is skipped.");
                return;
            }
            var go = GameObject.Instantiate(gmButtonPrefab);
            DontDestroyOnLoad(go);
        }

        private static string GetStringField(Type type, object instance, string fieldName, string defaultValue)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return defaultValue;
            }

            var value = field.GetValue(instance) as string;
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private static T LoadAddressableAsset<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            try
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
                var asset = handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return asset;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GMButton] Addressables load failed for key '{key}': {ex.Message}");
            }

            return null;
        }

        [Header("连点5次开启调试控制台")]
        [Range(0f, 1f)][SerializeField] private float m_maxIntervalBetweenClicks = 0.5f;
        [Range(3, 10)][SerializeField] private int m_requiredClicks = 5;
        private int m_clickCount = 0;
        private float m_lastClickTime = -1f;
        [SerializeField] private Button m_button;
        [SerializeField] private GameObject m_holder;
        public GameObject Holder => m_holder;
        private GameObject m_gmView;
        void Awake()
        {
            m_button.onClick.AddListener(OnGMButtonClick);
            // 默认隐藏
            SetActive(false);
            Instance = this;
        }

        public void SetActive(bool active)
        {
            m_holder.SetActive(active);
        }

        void Update()
        {
            if (QInput.GetPrimaryPointerDown() && QInput.TryGetPrimaryPointerPosition(out var pointerPosition))
            {
                int area = GetArea(pointerPosition);
                if (area == 2) // 右上角
                {
                    if (m_lastClickTime < 0 || Time.time - m_lastClickTime > m_maxIntervalBetweenClicks)
                    {
                        m_clickCount = 0; // 重置计数器
                    }

                    m_clickCount++;
                    m_lastClickTime = Time.time;

                    if (m_clickCount >= m_requiredClicks)
                    {
                        SetActive(true);
                        m_clickCount = 0; // 重置计数器
                        m_lastClickTime = -1f; // 重置时间
                    }
                }
                else if (area == 3)
                {
                    // 右下角连点3次 关闭调试控制台
                    if (m_lastClickTime < 0 || Time.time - m_lastClickTime > m_maxIntervalBetweenClicks)
                    {
                        m_clickCount = 0; // 重置计数器
                    }

                    m_clickCount++;
                    m_lastClickTime = Time.time;

                    if (m_clickCount >= 3)
                    {
                        SetActive(false);
                        m_clickCount = 0; // 重置计数器
                        m_lastClickTime = -1f; // 重置时间
                    }
                }
            }
        }

        /// <summary>
        /// 获取屏幕区域,1234分别代表左上,右上,左下,右下
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private int GetArea(Vector2 point)
        {
            float halfWidth = Screen.width / 2f;
            float halfHeight = Screen.height / 2f;

            if (point.x < halfWidth && point.y >= halfHeight)
            {
                return 1; // 左上
            }
            else if (point.x >= halfWidth && point.y >= halfHeight)
            {
                return 2; // 右上
            }
            else if (point.x < halfWidth && point.y < halfHeight)
            {
                return 3; // 左下
            }
            else
            {
                return 4; // 右下
            }
        }
        private void OnGMButtonClick()
        {
            if (m_gmView == null)
            {
                var prefab = AssetManager.LoadAsset<GameObject>(GmViewAddressKey);
                if (prefab == null)
                {
                    Debug.LogWarning("[GMButton] GMView prefab not found. GM view toggle is skipped.");
                    return;
                }
                m_gmView = GameObject.Instantiate(prefab);
                m_gmView.transform.SetParent(m_holder.transform, false);
            }
            else
            {
                m_gmView.SetActive(!m_gmView.activeSelf);
            }
        }
    }
}
