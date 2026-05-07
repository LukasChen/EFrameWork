using Cysharp.Threading.Tasks;
using EFramework.Runtime;
using EFramework.Runtime.UI.Handles;
using EFramework.Runtime.UI.Interactions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Runtime.UI
{
    /// <summary>
    /// UI 控制器基类
    /// - 管理 View 的生命周期
    /// - 支持单例模式（可选）
    /// </summary>
    public abstract class UIControllerBase : IDisposable, IEFrameContextAware
    {
        private static readonly Dictionary<Type, UIControllerBase> s_instances = new();
        private static readonly object s_lock = new();

        private bool m_disposed = false;
        private bool m_initialized = false;
        private readonly List<UIDismissScope> m_dismissScopes = new();
        private EFrameContext m_context;
        protected EFrameContext Context
        {
            get
            {
                if (m_context == null)
                {
                    BindContext(EFrame.Current);
                }

                return m_context;
            }
        }

        /// <summary>
        /// 控制器是否存活
        /// </summary>
        public bool IsAlive => !m_disposed;

        /// <summary>
        /// 是否正在显示
        /// </summary>
        public abstract bool IsShowing { get; }

        #region Singleton

        /// <summary>
        /// 获取控制器单例
        /// </summary>
        public static T GetInstance<T>() where T : UIControllerBase
        {
            lock (s_lock)
            {
                var type = typeof(T);
                if (s_instances.TryGetValue(type, out var instance))
                {
                    return instance as T;
                }
                return null;
            }
        }

        /// <summary>
        /// 获取或创建控制器单例
        /// </summary>
        public static T GetOrCreateInstance<T>() where T : UIControllerBase, new()
        {
            lock (s_lock)
            {
                var type = typeof(T);
                if (s_instances.TryGetValue(type, out var instance) && instance.IsAlive)
                {
                    return instance as T;
                }
                return new T();
            }
        }

        /// <summary>
        /// 注册单例
        /// </summary>
        protected void RegisterInstance()
        {
            lock (s_lock)
            {
                var type = this.GetType();
                s_instances[type] = this;
            }
        }

        /// <summary>
        /// 注销单例
        /// </summary>
        protected void UnregisterInstance()
        {
            lock (s_lock)
            {
                var type = this.GetType();
                if (s_instances.TryGetValue(type, out var instance) && instance == this)
                {
                    s_instances.Remove(type);
                }
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// UI 关闭时触发（用于队列系统）
        /// </summary>
        public event Action OnClosed;

        /// <summary>
        /// 触发关闭事件
        /// </summary>
        protected void InvokeOnClosed()
        {
            OnClosed?.Invoke();
        }

        protected UIControllerBase()
        {
            RegisterInstance();
            BindContext(EFrame.Current);
        }

        public void BindContext(EFrameContext context)
        {
            if (context == null)
            {
                throw new InvalidOperationException($"{GetType().Name} requires EFrame.Initialize() before using UIController context.");
            }

            if (ReferenceEquals(m_context, context)) return;
            m_context = context;
            OnContextBound(context);
        }

        protected EFrameContext RequireContext()
        {
            var context = Context;

            if (context == null)
            {
                throw new InvalidOperationException($"{GetType().Name} requires EFrame.Initialize() to complete before showing UI.");
            }

            EnsureInitialized();
            return context;
        }

        private void EnsureInitialized()
        {
            if (m_initialized)
            {
                return;
            }

            m_initialized = true;
            OnInit();
        }

        protected virtual void OnContextBound(EFrameContext context)
        {
        }


        /// <summary>
        /// 初始化（子类重写）
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 显示 UI
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        public abstract void Show(UILayer? layer = null);

        /// <summary>
        /// 带动画显示 UI
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        public virtual UniTask ShowAsync(UILayer? layer = null)
        {
            Show(layer);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 隐藏 UI
        /// </summary>
        public abstract void Hide();

        /// <summary>
        /// 带动画隐藏 UI
        /// </summary>
        public virtual UniTask HideAsync()
        {
            Hide();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 销毁时调用（子类重写）
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 检查组件是否为空
        /// </summary>
        protected bool IsNull(Component go)
        {
            return go == null || go.Equals(null);
        }

        /// <summary>
        /// 添加按钮点击监听
        /// </summary>
        /// <param name="button">按钮组件</param>
        /// <param name="action">点击事件回调</param>
        /// <param name="removeOldListeners">是否移除旧的监听器</param>
        protected void AddButtonClickListener(Button button, UnityEngine.Events.UnityAction action, bool removeOldListeners = false)
        {
            if (IsNull(button)) return;
            if (removeOldListeners)
            {
                button.onClick.RemoveAllListeners();
            }
            button.onClick.AddListener(action);
        }

        /// <summary>
        /// 添加 Toggle 值变化监听
        /// </summary>
        /// <param name="toggle">Toggle 组件</param>
        /// <param name="action">值变化回调</param>
        /// <param name="initValue">初始值（可选）</param>
        /// <param name="removeOldListeners">是否移除旧的监听器</param>
        protected void AddToggleValueChangedListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action, bool? initValue = null, bool removeOldListeners = true)
        {
            if (IsNull(toggle)) return;
            if (initValue.HasValue)
            {
                toggle.SetIsOnWithoutNotify(initValue.Value);
            }
            if (removeOldListeners)
            {
                toggle.onValueChanged.RemoveAllListeners();
            }
            toggle.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 添加 Slider 值变化监听
        /// </summary>
        /// <param name="slider">Slider 组件</param>
        /// <param name="action">值变化回调</param>
        /// <param name="initValue">初始值（可选）</param>
        /// <param name="removeOldListeners">是否移除旧的监听器</param>
        protected void AddSliderValueChangedListener(Slider slider, UnityEngine.Events.UnityAction<float> action, float? initValue = null, bool removeOldListeners = true)
        {
            if (IsNull(slider)) return;
            if (initValue.HasValue)
            {
                slider.SetValueWithoutNotify(initValue.Value);
            }
            if (removeOldListeners)
            {
                slider.onValueChanged.RemoveAllListeners();
            }
            slider.onValueChanged.AddListener(action);
        }

        #endregion

        #region Queue Support

        /// <summary>
        /// 将此控制器加入 UI 展示队列，按顺序显示，并等待关闭后继续下一个 UI。
        /// </summary>
        /// <param name="priority">优先级（数值越小越优先）</param>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        /// <param name="name">任务名称（调试用）</param>
        /// <returns>队列票据，可 await Completion 等待此 UI 关闭</returns>
        public UIQueueTicket EnqueueToShow(int priority = 0, UILayer? layer = null, string name = null)
        {
            return RequireContext().UI.Queue.Enqueue(this, layer, priority, name);
        }

        /// <summary>
        /// 将此控制器加入 UI 展示队列，并等待其显示后关闭。
        /// </summary>
        public UniTask EnqueueToShowAsync(int priority = 0, UILayer? layer = null, string name = null)
        {
            return EnqueueToShow(priority, layer, name).Completion;
        }

        #endregion

        #region Dismiss

        protected UIDismissScope RegisterDismissScope(Action<UIDismissTrigger> onDismiss, params RectTransform[] insideAreas)
        {
            var scope = RequireContext().UI.Dismiss.Register(onDismiss, true, insideAreas);
            m_dismissScopes.Add(scope);
            return scope;
        }

        protected UIDismissScope RegisterDismissScope(IEnumerable<RectTransform> insideAreas, Action<UIDismissTrigger> onDismiss, bool ignoreCurrentFrame = true)
        {
            var scope = RequireContext().UI.Dismiss.Register(insideAreas, onDismiss, ignoreCurrentFrame);
            m_dismissScopes.Add(scope);
            return scope;
        }

        protected void ClearDismissScopes()
        {
            for (int i = m_dismissScopes.Count - 1; i >= 0; i--)
            {
                m_dismissScopes[i]?.Dispose();
            }

            m_dismissScopes.Clear();
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
                    ClearDismissScopes();
                    Hide();
                    OnDestroy();
                    UnregisterInstance();
                }
                m_disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 泛型 UI 控制器基类
    /// - 自动管理 View Handle
    /// - 提供 Handle 访问
    /// </summary>
    /// <typeparam name="TView">View 类型</typeparam>
    public abstract class UIControllerBase<TView> : UIControllerBase where TView : BindingViewBase
    {
        private UIViewHandle<TView> m_viewHandle;
        private int m_controllerOperationVersion;

        /// <summary>
        /// 非泛型 View Handle
        /// </summary>
        public IUIViewHandle ViewHandle => m_viewHandle;

        /// <summary>
        /// 泛型 View Handle
        /// </summary>
        public UIViewHandle<TView> TypedViewHandle => m_viewHandle;

        /// <summary>
        /// 当前生成 View 实例，供业务 Controller 访问绑定组件。
        /// </summary>
        protected TView CurrentView => m_viewHandle?.TypedView;

        /// <summary>
        /// 是否正在显示
        /// </summary>
        public override bool IsShowing => m_viewHandle != null && m_viewHandle.IsShowing;

        /// <summary>
        /// 资源路径（子类必须实现）
        /// </summary>
        protected abstract string AssetPath { get; }

        /// <summary>
        /// 创建 View 实例（子类可重写自定义创建逻辑）
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        protected virtual UIViewHandle<TView> CreateViewHandle(UILayer? layer = null)
        {
            var context = RequireContext();
            return context.UI.CreateViewHandle<TView>(AssetPath, layer);
        }

        /// <summary>
        /// 显示 UI
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        public override void Show(UILayer? layer = null)
        {
            RequireContext();
            m_controllerOperationVersion++;

            if (m_viewHandle == null || !m_viewHandle.IsAlive)
            {
                m_viewHandle = CreateViewHandle(layer);
                OnViewCreated();
            }

            bool wasShowing = m_viewHandle.IsShowing;
            m_viewHandle.Open(layer);

            if (!wasShowing && m_viewHandle.IsShowing)
            {
                OnViewOpened();
            }
        }

        /// <summary>
        /// 带动画显示 UI
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        public override async UniTask ShowAsync(UILayer? layer = null)
        {
            RequireContext();
            int operationVersion = ++m_controllerOperationVersion;

            if (m_viewHandle == null || !m_viewHandle.IsAlive)
            {
                m_viewHandle = CreateViewHandle(layer);
                OnViewCreated();
            }

            var handle = m_viewHandle;
            bool wasShowing = handle.IsShowing;
            await handle.OpenAsync(layer);

            if (operationVersion != m_controllerOperationVersion || !ReferenceEquals(m_viewHandle, handle))
            {
                return;
            }

            if (!wasShowing && handle.IsShowing)
            {
                OnViewOpened();
            }
        }

        /// <summary>
        /// 隐藏 UI
        /// </summary>
        public override void Hide()
        {
            m_controllerOperationVersion++;
            var didClose = false;

            if (m_viewHandle != null && m_viewHandle.IsAlive)
            {
                bool wasShowing = m_viewHandle.IsShowing;
                bool willRelease = !m_viewHandle.UsingCache;
                didClose = wasShowing;

                if (wasShowing)
                {
                    OnViewClosed();
                    ClearDismissScopes();
                }

                if (willRelease)
                {
                    OnViewDestroyed();
                }

                m_viewHandle.Close();

                if (m_viewHandle.IsReleased)
                {
                    m_viewHandle = null;
                }
            }

            if (didClose)
            {
                InvokeOnClosed();
            }
        }

        /// <summary>
        /// 带动画隐藏 UI
        /// </summary>
        public override async UniTask HideAsync()
        {
            int operationVersion = ++m_controllerOperationVersion;
            var handle = m_viewHandle;
            var didClose = false;

            if (handle != null && handle.IsAlive)
            {
                bool wasShowing = handle.IsShowing;
                bool willRelease = !handle.UsingCache;
                didClose = wasShowing;

                if (wasShowing)
                {
                    OnViewClosed();
                    ClearDismissScopes();
                }

                if (willRelease)
                {
                    OnViewDestroyed();
                }

                await handle.CloseAsync();

                if (operationVersion != m_controllerOperationVersion || !ReferenceEquals(m_viewHandle, handle))
                {
                    return;
                }

                if (handle.IsReleased)
                {
                    m_viewHandle = null;
                }
            }

            if (didClose)
            {
                InvokeOnClosed();
            }
        }

        /// <summary>
        /// View 创建后调用（绑定事件等）
        /// </summary>
        protected virtual void OnViewCreated()
        {
        }

        /// <summary>
        /// View 每次打开后调用（刷新显示状态等）
        /// </summary>
        protected virtual void OnViewOpened()
        {
        }

        /// <summary>
        /// View 每次关闭时调用（暂停刷新等）
        /// </summary>
        protected virtual void OnViewClosed()
        {
        }

        /// <summary>
        /// View 释放前调用（解绑事件等）
        /// </summary>
        protected virtual void OnViewDestroyed()
        {
        }

        protected override void Dispose(bool disposing)
        {
            m_controllerOperationVersion++;
            if (disposing && m_viewHandle != null)
            {
                bool wasShowing = m_viewHandle.IsShowing;
                if (wasShowing)
                {
                    OnViewClosed();
                    ClearDismissScopes();
                }

                OnViewDestroyed();
                m_viewHandle.CloseAndDestroy();
                m_viewHandle = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 带返回值的弹窗控制器基类
    /// 使用 async/await 模式，避免回调时序问题
    /// </summary>
    /// <typeparam name="TView">View 类型</typeparam>
    /// <typeparam name="TResult">返回结果类型（通常是枚举）</typeparam>
    /// <example>
    /// // 定义结果枚举
    /// public enum SettingResult { Close, Restart, Quit }
    /// 
    /// // 继承基类
    /// public class SettingPopup : UIPopupController&lt;v_SettingWindow, SettingResult&gt; { ... }
    /// 
    /// // 使用
    /// var result = await new SettingPopup().ShowAsync();
    /// if (result == SettingResult.Quit) QuitLevel();
    /// </example>
    public abstract class UIPopupController<TView, TResult> : UIControllerBase<TView> where TView : BindingViewBase
    {
        private UniTaskCompletionSource<TResult> m_completionSource;
        private bool m_waitingForResult;
        private bool m_hasResult;

        /// <summary>
        /// 当前结果（在 CloseWithResult 后有效）
        /// </summary>
        protected TResult Result { get; private set; }

        /// <summary>
        /// 关闭前回调（在 Hide 之前调用，可用于保存 UI 元素位置等信息）
        /// </summary>
        public System.Action OnBeforeClose { get; set; }

        /// <summary>
        /// 显示弹窗并等待用户操作，返回结果
        /// </summary>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        /// <returns>用户操作结果</returns>
        public async UniTask<TResult> ShowAndWaitAsync(UILayer? layer = null)
        {
            if (m_waitingForResult)
            {
                throw new InvalidOperationException($"{GetType().Name} is already waiting for a popup result.");
            }

            m_waitingForResult = true;
            m_hasResult = false;
            m_completionSource = new UniTaskCompletionSource<TResult>();

            try
            {
                await ShowAsync(layer);

                // 等待用户操作
                var result = await m_completionSource.Task;
                return result;
            }
            finally
            {
                m_waitingForResult = false;
                m_completionSource = null;
            }
        }

        /// <summary>
        /// 关闭弹窗并设置结果
        /// 子类应在按钮点击时调用此方法
        /// </summary>
        /// <param name="result">操作结果</param>
        protected void CloseWithResult(TResult result)
        {
            Result = result;
            m_hasResult = true;
            OnBeforeClose?.Invoke();
            Hide();
            m_completionSource?.TrySetResult(result);
        }

        /// <summary>
        /// 关闭弹窗并设置结果（带动画）
        /// </summary>
        protected async UniTask CloseWithResultAsync(TResult result)
        {
            Result = result;
            m_hasResult = true;
            OnBeforeClose?.Invoke();
            await HideAsync();
            m_completionSource?.TrySetResult(result);
        }

        protected override void OnViewClosed()
        {
            base.OnViewClosed();
            if (m_waitingForResult && !m_hasResult)
            {
                Result = default;
                m_completionSource?.TrySetResult(Result);
            }
        }

        protected override void OnViewDestroyed()
        {
            base.OnViewDestroyed();
            if (m_waitingForResult && !m_hasResult)
            {
                Result = default;
                m_completionSource?.TrySetResult(Result);
            }
        }
    }
}
