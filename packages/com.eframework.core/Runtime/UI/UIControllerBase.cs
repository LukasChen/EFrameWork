using Cysharp.Threading.Tasks;
using EFrame.Runtime;
using EFrame.Runtime.UI.Handles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFrame.Runtime.UI
{
    #region Queueable Task Wrapper

    /// <summary>
    /// UI 控制器的队列任务包装器
    /// 将 UIControllerBase 包装为 IQueueableTask
    /// </summary>
    internal class UIControllerQueueableTask : IQueueableTask
    {
        private readonly UIControllerBase m_controller;
        private readonly int m_priority;
        private readonly UILayer? m_layer;
        private readonly string m_taskName;
        private UniTaskCompletionSource m_completionSource;

        public int Priority => m_priority;
        public string TaskName => m_taskName;

        public UIControllerQueueableTask(UIControllerBase controller, int priority, UILayer? layer = null, string taskName = null)
        {
            m_controller = controller;
            m_priority = priority;
            m_layer = layer;
            m_taskName = taskName ?? controller.GetType().Name;
        }

        public async UniTask ExecuteAsync()
        {
            m_completionSource = new UniTaskCompletionSource();

            // 注册关闭回调
            m_controller.OnClosed += OnControllerClosed;

            // 显示 UI（传入可空层级，优先使用配置）
            await m_controller.ShowAsync(m_layer);

            // 等待关闭
            await m_completionSource.Task;

            // 清理
            m_controller.OnClosed -= OnControllerClosed;
        }

        private void OnControllerClosed()
        {
            m_completionSource?.TrySetResult();
        }
    }

    #endregion

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
        protected EFrameContext Context { get; private set; }

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
            BindContext(EFrame.Current);
            RegisterInstance();
            OnInit();
        }

        public void BindContext(EFrameContext context)
        {
            if (context == null) return;
            Context = context;
            OnContextBound(context);
        }

        protected EFrameContext RequireContext()
        {
            if (Context == null)
            {
                BindContext(EFrame.Current);
            }

            if (Context == null)
            {
                throw new InvalidOperationException($"{GetType().Name} requires EFrame.Initialize() to complete before showing UI.");
            }

            return Context;
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
        protected void AddButtonClickListener(Button button, UnityEngine.Events.UnityAction action, bool removeOldListeners = true)
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
        /// 将此控制器包装为可排队任务
        /// </summary>
        /// <param name="priority">优先级（数值越小越优先）</param>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        /// <param name="taskName">任务名称（调试用，默认使用类名）</param>
        /// <returns>可排队任务实例</returns>
        public IQueueableTask ToQueueableTask(int priority = 0, UILayer? layer = null, string taskName = null)
        {
            return new UIControllerQueueableTask(this, priority, layer, taskName);
        }

        /// <summary>
        /// 将此控制器加入全局队列
        /// </summary>
        /// <param name="priority">优先级（数值越小越优先）</param>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        /// <param name="taskName">任务名称（调试用）</param>
        public void EnqueueToShow(int priority = 0, UILayer? layer = null, string taskName = null)
        {
            var task = ToQueueableTask(priority, layer, taskName);
            TaskQueue.Enqueue(task);
        }

        /// <summary>
        /// 将此控制器作为子任务加入队列
        /// </summary>
        /// <param name="blocking">是否阻塞主队列</param>
        /// <param name="priority">优先级</param>
        /// <param name="layer">UI 层级（null 表示使用 View 配置的默认层级）</param>
        public void EnqueueAsChild(bool blocking = true, int priority = 0, UILayer? layer = null)
        {
            var task = ToQueueableTask(priority, layer);
            TaskQueue.EnqueueChild(task, blocking);
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

        /// <summary>
        /// 非泛型 View Handle
        /// </summary>
        public IUIViewHandle ViewHandle => m_viewHandle;

        /// <summary>
        /// 泛型 View Handle
        /// </summary>
        public UIViewHandle<TView> TypedViewHandle => m_viewHandle;

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

            if (m_viewHandle == null || !m_viewHandle.IsAlive)
            {
                m_viewHandle = CreateViewHandle(layer);
                OnViewCreated();
            }

            bool wasShowing = m_viewHandle.IsShowing;
            await m_viewHandle.OpenAsync(layer);

            if (!wasShowing && m_viewHandle.IsShowing)
            {
                OnViewOpened();
            }
        }

        /// <summary>
        /// 隐藏 UI
        /// </summary>
        public override void Hide()
        {
            if (m_viewHandle != null && m_viewHandle.IsAlive)
            {
                bool wasShowing = m_viewHandle.IsShowing;
                bool willRelease = !m_viewHandle.UsingCache;

                if (wasShowing)
                {
                    OnViewClosed();
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
            InvokeOnClosed();
        }

        /// <summary>
        /// 带动画隐藏 UI
        /// </summary>
        public override async UniTask HideAsync()
        {
            if (m_viewHandle != null && m_viewHandle.IsAlive)
            {
                bool wasShowing = m_viewHandle.IsShowing;
                bool willRelease = !m_viewHandle.UsingCache;

                if (wasShowing)
                {
                    OnViewClosed();
                }

                if (willRelease)
                {
                    OnViewDestroyed();
                }

                await m_viewHandle.CloseAsync();

                if (m_viewHandle.IsReleased)
                {
                    m_viewHandle = null;
                }
            }
            InvokeOnClosed();
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
            if (disposing && m_viewHandle != null)
            {
                bool wasShowing = m_viewHandle.IsShowing;
                if (wasShowing)
                {
                    OnViewClosed();
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
            m_completionSource = new UniTaskCompletionSource<TResult>();

            await ShowAsync(layer);

            // 等待用户操作
            var result = await m_completionSource.Task;

            return result;
        }

        /// <summary>
        /// 关闭弹窗并设置结果
        /// 子类应在按钮点击时调用此方法
        /// </summary>
        /// <param name="result">操作结果</param>
        protected void CloseWithResult(TResult result)
        {
            Result = result;
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
            OnBeforeClose?.Invoke();
            await HideAsync();
            m_completionSource?.TrySetResult(result);
        }
    }
}
