using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EFramework.Runtime.UI
{
    /// <summary>
    /// View 动画配置。
    /// </summary>
    [System.Serializable]
    public struct ViewTransitionConfig
    {
        public const float DefaultDuration = 0.25f;

        [Tooltip("动画类型（留空则使用框架默认动画）")]
        public string TransitionTypeName;

        [Tooltip("动画时长（秒，0 表示使用动画默认时长）")]
        public float Duration;

        public ViewTransitionConfig(string transitionTypeName = "", float duration = DefaultDuration)
        {
            TransitionTypeName = transitionTypeName;
            Duration = duration;
        }

        public bool HasExplicitValue()
        {
            return !string.IsNullOrWhiteSpace(TransitionTypeName) || Duration > 0f;
        }

        public static ViewTransitionConfig Default => new ViewTransitionConfig();
    }

    /// <summary>
    /// View 配置 - 存储在 Prefab 上的默认配置
    /// </summary>
    [System.Serializable]
    public struct ViewConfig
    {
        [Tooltip("默认 UI 层级")]
        public UILayer DefaultLayer;

        [Tooltip("是否为界面的根节点")]
        public bool IsViewRoot;
        
        [Tooltip("是否使用缓存池（关闭时隐藏而非销毁）")]
        public bool UsingCache;

        [Tooltip("动画根节点名称（留空则不播放动画，常用：Window、Root、Content）")]
        public string AnimationRootName;

        [Tooltip("打开动画")]
        public ViewTransitionConfig OpenTransition;

        [Tooltip("关闭动画")]
        public ViewTransitionConfig CloseTransition;

        [SerializeField]
        [HideInInspector]
        [Tooltip("动画类型（留空则使用框架默认动画）")]
        public string TransitionTypeName;

        [SerializeField]
        [HideInInspector]
        [Tooltip("动画时长")]
        public float AnimationDuration;

        public ViewConfig(UILayer layer = UILayer.QuiPanel, bool isViewRoot = false, bool cache = false, string animRoot = "", float animDuration = 0.25f, string transitionTypeName = "")
        {
            DefaultLayer = layer;
            IsViewRoot = isViewRoot;
            UsingCache = cache;
            AnimationRootName = animRoot;
            TransitionTypeName = transitionTypeName;
            AnimationDuration = animDuration;
            OpenTransition = new ViewTransitionConfig(transitionTypeName, animDuration);
            CloseTransition = new ViewTransitionConfig(transitionTypeName, animDuration);
        }

        public string GetTransitionTypeName(bool opening)
        {
            var transition = opening ? OpenTransition : CloseTransition;
            if (transition.HasExplicitValue())
            {
                return transition.TransitionTypeName;
            }

            return TransitionTypeName;
        }

        public float GetTransitionDuration(bool opening)
        {
            var transition = opening ? OpenTransition : CloseTransition;
            if (transition.Duration > 0f)
            {
                return transition.Duration;
            }

            if (AnimationDuration > 0f)
            {
                return AnimationDuration;
            }

            return ViewTransitionConfig.DefaultDuration;
        }

        public static ViewConfig Default => new ViewConfig(UILayer.QuiPanel, false, false, "", 0.25f);
    }

    /// <summary>
    /// 组件绑定项 - 简化的数据结构，只存储选中的组件
    /// </summary>
    [System.Serializable]
    public class ComponentBindingItem
    {
        [SerializeField] public string BindingName;    // 绑定名称 (用于生成属性名)
        [SerializeField] public Component Component;   // 组件引用
        [SerializeField] public string DisplayName;    // 显示名称 (GameObject名称)

        public ComponentBindingItem(string bindingName, Component component, string displayName)
        {
            this.BindingName = bindingName;
            this.Component = component;
            this.DisplayName = displayName;
        }

        /// <summary>
        /// 获取组件类型名称
        /// </summary>
        public string ComponentTypeName => Component?.GetType().Name ?? "Component";

        /// <summary>
        /// 检查组件是否有效
        /// </summary>
        public bool IsValid => Component != null;
    }

    public class QUIBinding : MonoBehaviour
    {
        public const string GeneratedAccessClassNamespace = "EFramework.Generated.UI";

        [SerializeField]
        [HideInInspector]
        private List<ComponentBindingItem> m_bindingItems = new List<ComponentBindingItem>();

        [SerializeField]
        [HideInInspector]
        private string m_accessClassName = "";

        [SerializeField]
        [HideInInspector]
        // Kept only so old prefabs deserialize cleanly; generated namespaces are fixed.
        private string m_accessClassNamespace = "";

        [Header("View 配置")]
        [SerializeField]
        private ViewConfig m_viewConfig = ViewConfig.Default;

        /// <summary>
        /// 获取 View 配置
        /// </summary>
        public ViewConfig Config => m_viewConfig;

        /// <summary>
        /// 获取所有绑定的组件项
        /// </summary>
        public List<ComponentBindingItem> BindingItems => m_bindingItems;

        /// <summary>
        /// 获取或设置访问类名称
        /// </summary>
        public string AccessClassName
        {
            get => string.IsNullOrEmpty(m_accessClassName) ? $"v_{gameObject.name}" : m_accessClassName;
            set => m_accessClassName = value;
        }

        /// <summary>
        /// 获取固定的生成访问类命名空间
        /// </summary>
        public string AccessClassNamespace
        {
            get => GeneratedAccessClassNamespace;
            set => m_accessClassNamespace = string.Empty;
        }

        /// <summary>
        /// 获取动画根节点 Transform（根据配置的 AnimationRootName 查找）
        /// </summary>
        public Transform GetAnimationRoot()
        {
            if (string.IsNullOrEmpty(m_viewConfig.AnimationRootName))
                return null;

            return transform.Find(m_viewConfig.AnimationRootName);
        }

        /// <summary>
        /// 根据绑定名称获取组件
        /// </summary>
        public T GetComponent<T>(string bindingName) where T : Component
        {
            var item = m_bindingItems.Find(c => c.BindingName == bindingName && c.Component is T);
            return item?.Component as T;
        }

        /// <summary>
        /// 获取指定类型的所有组件
        /// </summary>
        public new List<T> GetComponents<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var item in m_bindingItems)
            {
                if (item.Component is T component)
                {
                    result.Add(component);
                }
            }
            return result;
        }

        /// <summary>
        /// 添加组件绑定
        /// </summary>
        public void AddBinding(string bindingName, Component component, string displayName)
        {
            if (component == null) return;

            // 检查是否已存在相同绑定名称
            var existingItem = m_bindingItems.Find(item => item.BindingName == bindingName);
            if (existingItem != null)
            {
                existingItem.Component = component;
                existingItem.DisplayName = displayName;
            }
            else
            {
                m_bindingItems.Add(new ComponentBindingItem(bindingName, component, displayName));
            }
        }

        /// <summary>
        /// 移除组件绑定
        /// </summary>
        public void RemoveBinding(string bindingName)
        {
            m_bindingItems.RemoveAll(item => item.BindingName == bindingName);
        }

        /// <summary>
        /// 清空所有绑定
        /// </summary>
        public void ClearAllBindings()
        {
            m_bindingItems.Clear();
        }

        /// <summary>
        /// 检查绑定名称是否存在
        /// </summary>
        public bool HasBinding(string bindingName)
        {
            return m_bindingItems.Exists(item => item.BindingName == bindingName);
        }

        /// <summary>
        /// 获取所有绑定名称
        /// </summary>
        public List<string> GetAllBindingNames()
        {
            return m_bindingItems.Select(item => item.BindingName).ToList();
        }

        /// <summary>
        /// 验证所有绑定的有效性
        /// </summary>
        public void ValidateBindings()
        {
            m_bindingItems.RemoveAll(item => !item.IsValid);
        }

        /// <summary>
        /// 输出所有绑定信息（用于调试）
        /// </summary>
        public void LogBindings()
        {
            Debug.Log($"绑定组件数量: {m_bindingItems.Count}");
            foreach (var item in m_bindingItems)
            {
                Debug.Log($"- {item.BindingName}: {item.ComponentTypeName} ({item.DisplayName})");
            }
        }
    }
}
