using Cysharp.Threading.Tasks;

namespace EFramework.Runtime
{
    /// <summary>
    /// 可排队任务接口
    /// 任何需要进入队列顺序执行的任务都应实现此接口
    /// </summary>
    public interface IQueueableTask
    {
        /// <summary>
        /// 任务优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 任务名称（用于调试）
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// 执行任务
        /// 任务完成时（如弹窗关闭）返回
        /// </summary>
        UniTask ExecuteAsync();
    }
}
