using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameAiLoopDelay
    {
        public static Task DelayFrames(int frameCount, CancellationToken cancellationToken = default)
        {
            if (frameCount <= 0)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> completion = new();
            int remaining = frameCount;
            void Tick()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    EditorApplication.update -= Tick;
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                remaining--;
                if (remaining > 0)
                {
                    return;
                }

                EditorApplication.update -= Tick;
                completion.TrySetResult(true);
            }

            EditorApplication.update += Tick;
            return completion.Task;
        }
    }
}
