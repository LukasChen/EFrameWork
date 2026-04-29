using Cysharp.Threading.Tasks;
using DG.Tweening;
using EFrameWork.Runtime.Asset;
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

            // 尝试从缓存获取
            BindContext(EFrame.Current);
            if (Context?.UI == null)
                throw new InvalidOperationException($"BindingViewBase: EFrame.Current.UI is not initialized. AssetPath: {assetPath}");
            var cached = Context.UI.GetViewFromCache<BindingViewBase>(assetPath);
            if (cached != null)
            {
                Binding = cached.Binding;
                ApplyConfig(Binding.Config);
                return;
            }

            // 缓存中没有，实例化新的
            var go = AssetManager.Instantiate(assetPath);
            Context.InjectInto(go);
            go.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (go.TryGetComponent<QUIBinding>(out var uiBinding))
            {
                Binding = uiBinding;
                ApplyConfig(Binding.Config);
            }
            else
            {
                GameObject.Destroy(go);
                throw new ArgumentException($"The instantiated GameObject from path '{assetPath}' does not contain a QUIBinding component.");
            }
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

            // 尝试从缓存获取
            var cached = Context.UI.GetViewFromCache<BindingViewBase>(assetPath);
            if (cached != null)
            {
                Binding = cached.Binding;
                ApplyConfig(Binding.Config, layer, useCache);
                Open(m_layer);
                return;
            }

            // 缓存中没有，实例化新的
            var parent = Context.UI.UILayer(layer);
            var go = AssetManager.Instantiate(assetPath, parent);
            Context.InjectInto(go);
            go.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (go.TryGetComponent<QUIBinding>(out var uiBinding))
            {
                Binding = uiBinding;
                ApplyConfig(Binding.Config, layer, useCache);
            }
            else
            {
                GameObject.Destroy(go);
                throw new ArgumentException($"The instantiated GameObject from path '{assetPath}' does not contain a QUIBinding component.");
            }
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

        #endregion

        #region Open / Close

        /// <summary>
        /// 打开 View 到指定层级
        /// </summary>
        public virtual void Open(UILayer layer)
        {
            ThrowIfDisposed();
            m_layer = layer;
            if (Binding != null)
            {
                Context.UI.OpenBindingView(this, layer);
            }

            EventBus.Dispatch(new UIOpenEvent() { UIName = Binding.gameObject.name });
        }

        /// <summary>
        /// 打开 View（使用预配置层级）
        /// </summary>
        public virtual void Open()
        {
            Open(m_layer);
        }

        /// <summary>
        /// 关闭 View
        /// - 如果使用缓存模式，回收到缓存池
        /// - 否则销毁 GameObject
        /// </summary>
        public virtual void Close()
        {
            if (m_disposed) return;

            // 从栈中移除（如果在栈中）
            Context?.UI?.RemoveFromStack(this);
            if (Binding != null)
            {
                EventBus.Dispatch(new UICloseEvent() { UIName = Binding.gameObject.name });

                // Kill所有DOTween动画，防止销毁后动画仍在运行
                var animRoot = Binding.GetAnimationRoot();
                if (animRoot != null)
                {
                    animRoot.DOKill(true);
                    var canvasGroup = animRoot.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.DOKill(true);
                    }
                }

                if (m_usingCache && !string.IsNullOrEmpty(m_assetPath))
                {
                    // 回收到缓存池
                    Context.UI.RecycleViewToCache(m_assetPath, this);
                }
                else
                {
                    // 销毁
                    GameObject.Destroy(Binding.gameObject);
                    Binding = null;
                    m_disposed = true;
                }
            }

        }

        /// <summary>
        /// 强制销毁（不使用缓存）
        /// </summary>
        public virtual void CloseAndDestroy()
        {
            if (m_disposed) return;

            Context?.UI?.RemoveFromStack(this);

            if (Binding != null)
            {
                // Kill所有DOTween动画，防止销毁后动画仍在运行
                var animRoot = Binding.GetAnimationRoot();
                if (animRoot != null)
                {
                    animRoot.DOKill(true);
                    var canvasGroup = animRoot.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.DOKill(true);
                    }
                }

                GameObject.Destroy(Binding.gameObject);
                Binding = null;
            }
            m_disposed = true;
        }

        #endregion

        #region Animation

        /// <summary>
        /// 带动画打开 View
        /// </summary>
        public virtual async UniTask OpenAsync(UILayer layer)
        {
            Open(layer);
            await PlayOpenAnimation();
        }

        /// <summary>
        /// 带动画打开 View（使用预配置层级）
        /// </summary>
        public virtual async UniTask OpenAsync()
        {
            await OpenAsync(m_layer);
        }

        /// <summary>
        /// 带动画关闭 View
        /// </summary>
        public virtual async UniTask CloseAsync()
        {
            await PlayCloseAnimation();
            Close();
        }

        /// <summary>
        /// 播放打开动画
        /// </summary>
        protected virtual UniTask PlayOpenAnimation()
        {
            var animRoot = Binding?.GetAnimationRoot();
            if (animRoot == null) return UniTask.CompletedTask;

            float duration = Config.AnimationDuration;
            if (duration <= 0) duration = 0.25f;

            // 默认动画：Scale 0.8 → 1 + Fade 0 → 1
            animRoot.localScale = Vector3.one * 0.8f;
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.Sequence()
                    .Join(animRoot.DOScale(1f, duration).SetEase(Ease.OutBack))
                    .Join(canvasGroup.DOFade(1f, duration));
            }
            else
            {
                animRoot.DOScale(1f, duration).SetEase(Ease.OutBack);
            }

            return UniTask.Delay(TimeSpan.FromSeconds(duration));
        }

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        protected virtual UniTask PlayCloseAnimation()
        {
            var animRoot = Binding?.GetAnimationRoot();
            if (animRoot == null) return UniTask.CompletedTask;

            float duration = Config.AnimationDuration;
            if (duration <= 0) duration = 0.25f;

            // 默认动画：Scale 1 → 0.8 + Fade 1 → 0
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.Sequence()
                    .Join(animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack))
                    .Join(canvasGroup.DOFade(0f, duration));
            }
            else
            {
                animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack);
            }

            return UniTask.Delay(TimeSpan.FromSeconds(duration));
        }

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
        public void SetBinding(QUIBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            Binding = binding;
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
                    CloseAndDestroy();
                }
                m_disposed = true;
            }
        }

        #endregion
    }
}
