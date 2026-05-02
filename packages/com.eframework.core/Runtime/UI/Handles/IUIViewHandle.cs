using Cysharp.Threading.Tasks;

namespace EFrame.Runtime.UI.Handles
{
    public interface IUIViewHandle
    {
        BindingViewBase View { get; }

        bool IsAlive { get; }

        bool IsShowing { get; }

        void Open(UILayer? layer = null);

        UniTask OpenAsync(UILayer? layer = null);

        void Close();

        UniTask CloseAsync();

        void CloseAndDestroy();
    }
}