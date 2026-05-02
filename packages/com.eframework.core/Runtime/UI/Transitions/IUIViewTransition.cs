using Cysharp.Threading.Tasks;

namespace EFrame.Runtime.UI.Transitions
{
    public interface IUIViewTransition
    {
        UniTask PlayOpenAsync(BindingViewBase view);
        UniTask PlayCloseAsync(BindingViewBase view);
        void Kill(BindingViewBase view);
    }
}