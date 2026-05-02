using Cysharp.Threading.Tasks;

namespace EFramework.Runtime.UI.Transitions
{
    public sealed class NoneViewTransition : IUIViewTransition
    {
        public const string Id = "None";

        public UniTask PlayOpenAsync(BindingViewBase view)
        {
            return UniTask.CompletedTask;
        }

        public UniTask PlayCloseAsync(BindingViewBase view)
        {
            return UniTask.CompletedTask;
        }

        public void Kill(BindingViewBase view)
        {
        }
    }
}
