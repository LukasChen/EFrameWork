using Cysharp.Threading.Tasks;

namespace EFrameWork.Runtime.UI.Transitions
{
    public interface IUIViewTransition
    {
        UniTask PlayOpenAsync(BindingViewBase view);
        UniTask PlayCloseAsync(BindingViewBase view);
        void Kill(BindingViewBase view);
    }
}