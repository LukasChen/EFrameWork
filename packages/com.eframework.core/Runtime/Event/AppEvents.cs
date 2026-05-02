namespace EFramework.Runtime.Event
{
    /// <summary>
    /// 应用启动事件
    /// </summary>
    public struct AppStartedEvent : IEvent { }

    /// <summary>
    /// 应用退出事件
    /// </summary>
    public struct AppQuitEvent : IEvent { }

    /// <summary>
    /// 应用暂停事件（进入后台）
    /// </summary>
    public struct AppPausedEvent : IEvent
    {
        public int Duration;
    }

    /// <summary>
    /// 应用恢复事件（从后台返回）
    /// </summary>
    public struct AppResumedEvent : IEvent { }

    /// <summary>
    /// 每帧更新事件
    /// </summary>
    public struct AppUpdateEvent : IEvent
    {
        /// <summary>
        /// Time.deltaTime
        /// </summary>
        public float DeltaTime;

        /// <summary>
        /// Time.unscaledDeltaTime
        /// </summary>
        public float UnscaledDeltaTime;
    }

    /// <summary>
    /// 每秒更新事件
    /// </summary>
    public struct AppUpdatePerSecondEvent : IEvent { }
}
