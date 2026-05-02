using EFrame.Runtime;
using EFrame.Runtime.Asset;
using EFrame.Runtime.Event;
using EFrame.Runtime.Procedure;
using EFrame.Runtime.UI;
using UnityEngine;
using EFrameRuntime = EFrame.Runtime.EFrame;

namespace EFrameConsumerFixture
{
    public sealed class ConsumerApiProbe : MonoBehaviour
    {
        public bool IsInitialized => EFrameRuntime.Initialized;
        public EFrameContext Current => EFrameRuntime.Current;
        public IAssetService Assets => EFrameRuntime.Assets;
        public IUIService UI => EFrameRuntime.UI;
        public IEventService Events => EFrameRuntime.Events;
    }

    public sealed class ConsumerProcedure : EFrameProcedure
    {
        protected override void OnEnter(ProcedureEnterContext context)
        {
            _ = EFrameRuntime.Current;
            _ = EFrameRuntime.Assets;
            _ = Context;
        }
    }

    public sealed class ConsumerView : BindingViewBase
    {
    }

    public sealed class ConsumerController : UIControllerBase<ConsumerView>
    {
        protected override string AssetPath => "Fixture/ConsumerView";
    }
}
