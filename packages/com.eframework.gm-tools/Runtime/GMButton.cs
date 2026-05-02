using System;
using System.Reflection;
using EFramework.Runtime;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace EFramework.Extensions.GMTools
{
    /// <summary>
    /// GM button entry for opening a project-provided GM debug view.
    /// </summary>
    public class GMButton : MonoBehaviour
    {
        public static GMButton Instance { get; private set; }
        public static string GmButtonAddressKey { get; set; } = string.Empty;
        public static string GmViewAddressKey { get; set; } = string.Empty;

        [Header("连点5次开启调试控制台")]
        [Range(0f, 1f)]
        [SerializeField]
        private float m_maxIntervalBetweenClicks = 0.5f;

        [Range(3, 10)]
        [SerializeField]
        private int m_requiredClicks = 5;

        [SerializeField]
        private Button m_button;

        [SerializeField]
        private GameObject m_holder;

        private int m_clickCount;
        private float m_lastClickTime = -1f;
        private GameObject m_gmView;

        public GameObject Holder => m_holder;

        public static void Initialize()
        {
            var gmButtonPrefab = LoadAddressableAsset<GameObject>(GmButtonAddressKey);
            if (gmButtonPrefab == null)
            {
                Debug.LogWarning("[GMButton] GMButton prefab not found. GM tools initialization is skipped.");
                return;
            }

            var go = Instantiate(gmButtonPrefab);
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

        private void Awake()
        {
            if (m_button != null)
            {
                m_button.onClick.AddListener(OnGMButtonClick);
            }

            SetActive(false);
            Instance = this;
        }

        public void SetActive(bool active)
        {
            if (m_holder != null)
            {
                m_holder.SetActive(active);
            }
        }

        private void Update()
        {
            if (!QInput.GetPrimaryPointerDown() || !QInput.TryGetPrimaryPointerPosition(out var pointerPosition))
            {
                return;
            }

            int area = GetArea(pointerPosition);
            if (area == 2)
            {
                RegisterCornerClicks(m_requiredClicks, true);
            }
            else if (area == 3)
            {
                RegisterCornerClicks(3, false);
            }
        }

        private void RegisterCornerClicks(int requiredClicks, bool active)
        {
            if (m_lastClickTime < 0 || Time.time - m_lastClickTime > m_maxIntervalBetweenClicks)
            {
                m_clickCount = 0;
            }

            m_clickCount++;
            m_lastClickTime = Time.time;

            if (m_clickCount < requiredClicks)
            {
                return;
            }

            SetActive(active);
            m_clickCount = 0;
            m_lastClickTime = -1f;
        }

        private static int GetArea(Vector2 point)
        {
            float halfWidth = Screen.width / 2f;
            float halfHeight = Screen.height / 2f;

            if (point.x < halfWidth && point.y >= halfHeight)
            {
                return 1;
            }

            if (point.x >= halfWidth && point.y >= halfHeight)
            {
                return 2;
            }

            return point.x < halfWidth && point.y < halfHeight ? 3 : 4;
        }

        private void OnGMButtonClick()
        {
            if (m_gmView == null)
            {
                var prefab = EFrame.Current?.Assets?.Load<GameObject>(GmViewAddressKey);
                if (prefab == null)
                {
                    Debug.LogWarning("[GMButton] GMView prefab not found. GM view toggle is skipped.");
                    return;
                }

                m_gmView = Instantiate(prefab);
                if (m_holder != null)
                {
                    m_gmView.transform.SetParent(m_holder.transform, false);
                }
            }
            else
            {
                m_gmView.SetActive(!m_gmView.activeSelf);
            }
        }
    }
}
