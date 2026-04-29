using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using EFrameWork.Runtime;
using EFrameWork.Runtime.Event;

namespace EFrameWork.Runtime.UI
{
    /// <summary>
    /// UI View 基类
    /// - 支持从 QUIBinding 读取预配置（UILayer、缓存、动画）
    /// - 构造时可选覆盖配置
    /// - 支持缓存池复用
    /// - 支持动画开关
    /// </summary>
    public class BindingViewBase : IDisposable, IEFrameContextAware
    {
        private bool m_disposed = false;
        private string m_assetPath;
        private bool m_usingCache;
        private UILayer m_layer;
        protected EFrameContext Context { get; private set; }

        #region Constructors

        /// <summary>
        /// 从 QUIBinding 构造（不自动 Open）
        /// </summary>
        public BindingViewBase(QUIBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            BindContext(EFrame.Current);
            Binding = binding;
            ApplyConfig(binding.Config);
        }

        /// <summary>
        /// 从资源路径构造（不自动 Open）
        /// </summary>
        public BindingViewBase(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            m_assetPath = assetPath;
            BindContext(EFrame.Current);
            if (Context?.UI == null)
                throw new InvalidOperationException($"BindingViewBase: EFrame.Current.UI is not initialized. AssetPath: {assetPath}");

            Binding = Context.UI.CreateBinding(assetPath);
            ApplyConfig(Binding.Config);
        }

        /// <summary>
        /// 从资源路径构造并自动 Open 到指定层级
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="layer">UI 层级（覆盖预配置）</param>
        /// <param name="useCache">是否使用缓存（null 表示使用预配置）</param>
        public BindingViewBase(string assetPath, UILayer layer, bool? useCache = null)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            BindContext(EFrame.Current);
            if (Context?.UI == null)
                throw new InvalidOperationException($"BindingViewBase: EFrame.Current.UI is not initialized. Make sure EFrame.Initialize() is called before creating UI views. AssetPath: {assetPath}");

            m_assetPath = assetPath;
            m_layer = layer;

            Binding = Context.UI.CreateBinding(assetPath, layer);
            ApplyConfig(Binding.Config, layer, useCache);
        }

        /// <summary>
        /// Protected 无参构造函数，供子类在特殊情况下使用
        /// </summary>
        protected BindingViewBase()
        {
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        private void ApplyConfig(ViewConfig config, UILayer? overrideLayer = null, bool? overrideCache = null)
        {
            m_layer = overrideLayer ?? config.DefaultLayer;
            m_usingCache = overrideCache ?? config.UsingCache;
        }

        #endregion

        #region Properties

        public QUIBinding Binding { get; protected set; }

        public GameObject gameObject => Binding == null ? null : Binding.gameObject;
        public Transform transform => Binding == null ? null : Binding.transform;

        /// <summary>
        /// 资源路径（用于缓存）
        /// </summary>
        public string AssetPath => m_assetPath;

        /// <summary>
        /// 是否使用缓存模式
        /// </summary>
        public bool UsingCache => m_usingCache;

        /// <summary>
        /// 检查对象是否已被销毁
        /// </summary>
        public bool IsDisposed => m_disposed;

        /// <summary>
        /// View 配置
        /// </summary>
        public ViewConfig Config => Binding == null ? ViewConfig.Default : Binding.Config;

        internal UILayer CurrentLayer => m_layer;

        #endregion

        #region Open / Close

        internal UILayer ResolveLayer(UILayer? layer = null)
        {
            return layer ?? m_layer;
        }

        internal void PrepareForOpen(UILayer? layer = null)
        {
            ThrowIfDisposed();
            SetActive(true);
            AttachToLayer(ResolveLayer(layer));
            DispatchOpenEvent();
        }

        internal void AttachToLayer(UILayer layer)
        {
            ThrowIfDisposed();
            m_layer = layer;

            if (Binding != null)
            {
                Context?.UI?.OpenBindingView(this, layer);
            }
        }

        internal void DispatchOpenEvent()
        {
            if (Binding == null)
            {
                return;
            }

            EventBus.Dispatch(new UIOpenEvent() { UIName = Binding.gameObject.name });
        }

        internal void DispatchCloseEvent()
        {
            if (Binding == null)
            {
                return;
            }

            EventBus.Dispatch(new UICloseEvent() { UIName = Binding.gameObject.name });
        }

        internal UniTask PlayOpenTransitionAsync()
        {
            return Context?.UI?.PlayOpenTransitionAsync(this) ?? UniTask.CompletedTask;
        }

        internal UniTask PlayCloseTransitionAsync()
        {
            return Context?.UI?.PlayCloseTransitionAsync(this) ?? UniTask.CompletedTask;
        }

        internal void KillTransition()
        {
            Context?.UI?.KillTransition(this);
        }

        internal void PrepareForClose(bool killTransition, bool dispatchCloseEvent = true)
        {
            if (dispatchCloseEvent)
            {
                DispatchCloseEvent();
            }

            Context?.UI?.RemoveFromStack(this);

            if (killTransition)
            {
                KillTransition();
            }
        }

        internal void DeactivateForReuse()
        {
            if (Binding == null)
            {
                return;
            }

            KillTransition();
            SetActive(false);
        }

        internal void Release(bool forceDestroy = false)
        {
            if (m_disposed)
            {
                return;
            }

            if (Binding != null)
            {
                var useCache = !forceDestroy && m_usingCache && !string.IsNullOrEmpty(m_assetPath);
                Context?.UI?.ReleaseBinding(m_assetPath, Binding, useCache);
                Binding = null;
            }

            m_disposed = true;
        }

        #endregion

        #region Animation

        #endregion

        #region Utility

        /// <summary>
        /// 设置UI组件的激活状态
        /// </summary>
        public virtual void SetActive(bool active)
        {
            ThrowIfDisposed();
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 重新初始化组件引用（子类应重写此方法）
        /// </summary>
        public virtual void RefreshComponents()
        {
            ThrowIfDisposed();
        }

        /// <summary>
        /// 验证所有组件引用是否有效（子类应重写此方法）
        /// </summary>
        public virtual bool ValidateReferences()
        {
            ThrowIfDisposed();
            return true;
        }

        /// <summary>
        /// 设置 Binding 引用（支持延迟初始化）
        /// </summary>
        public void SetBinding(QUIBinding binding, string assetPath = null)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            m_assetPath = assetPath ?? m_assetPath;
            Binding = binding;
            m_disposed = false;
            BindContext(EFrame.Current);
            ApplyConfig(binding.Config);
            OnBindingSet();
        }

        public void BindContext(EFrameContext context)
        {
            if (context == null) return;
            Context = context;
        }

        /// <summary>
        /// 当 Binding 被设置时调用，子类可重写此方法进行初始化
        /// </summary>
        protected virtual void OnBindingSet()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    PrepareForClose(true, false);
                    Release(true);
                }
                m_disposed = true;
            }
        }

        #endregion
    }
}
