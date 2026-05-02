using EFramework.Runtime;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Event;
using EFramework.Runtime.Procedure;
using EFramework.Runtime.UI;
using UnityEngine;

namespace EFrameConsumerFixture
{
    public sealed class ConsumerApiProbe : MonoBehaviour
    {
        public bool IsInitialized => EFrame.Initialized;
        public EFrameContext Current => EFrame.Current;
        public IAssetService Assets => EFrame.Assets;
        public IUIService UI => EFrame.UI;
        public IEventService Events => EFrame.Events;
    }

    public sealed class ConsumerProcedure : EFrameProcedure
    {
        protected override void OnEnter(ProcedureEnterContext context)
        {
            _ = EFrame.Current;
            _ = EFrame.Assets;
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
