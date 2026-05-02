using EFrame.Runtime;
using EFrame.Runtime.Asset;
using EFrame.Runtime.Procedure;
using EFrame.Runtime.UI;
using EFrameConsumerFixture;
using NUnit.Framework;
using EFrameRuntime = EFrame.Runtime.EFrame;

namespace EFrameConsumerFixture.Tests
{
    public sealed class EFrameConsumerApiTests
    {
        [Test]
        public void RuntimeEntryAndRuntimeTypesResolveForExternalConsumers()
        {
            Assert.That(typeof(EFrameRuntime).Namespace, Is.EqualTo("EFrame.Runtime"));
            Assert.That(typeof(EFrameContext).Namespace, Is.EqualTo("EFrame.Runtime"));
            Assert.That(typeof(IAssetService).Namespace, Is.EqualTo("EFrame.Runtime.Asset"));
            Assert.That(typeof(EFrameProcedure).Namespace, Is.EqualTo("EFrame.Runtime.Procedure"));
            Assert.That(typeof(UIControllerBase).Namespace, Is.EqualTo("EFrame.Runtime.UI"));
        }

        [Test]
        public void ConsumerAssemblyCanUseRuntimeEntryAndRuntimeNamespacesTogether()
        {
            Assert.That(typeof(ConsumerApiProbe), Is.Not.Null);
            Assert.That(typeof(ConsumerProcedure).BaseType, Is.EqualTo(typeof(EFrameProcedure)));
            Assert.That(typeof(ConsumerController).BaseType, Is.Not.Null);
            Assert.That(EFrameRuntime.Initialized, Is.False);
        }
    }
}
