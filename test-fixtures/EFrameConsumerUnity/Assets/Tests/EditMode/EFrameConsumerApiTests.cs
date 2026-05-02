using EFramework.Runtime;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;
using EFramework.Runtime.UI;
using EFrameConsumerFixture;
using NUnit.Framework;

namespace EFrameConsumerFixture.Tests
{
    public sealed class EFrameConsumerApiTests
    {
        [Test]
        public void RuntimeEntryAndRuntimeTypesResolveForExternalConsumers()
        {
            Assert.That(typeof(EFrame).Namespace, Is.EqualTo("EFramework.Runtime"));
            Assert.That(typeof(EFrameContext).Namespace, Is.EqualTo("EFramework.Runtime"));
            Assert.That(typeof(IAssetService).Namespace, Is.EqualTo("EFramework.Runtime.Asset"));
            Assert.That(typeof(EFrameProcedure).Namespace, Is.EqualTo("EFramework.Runtime.Procedure"));
            Assert.That(typeof(UIControllerBase).Namespace, Is.EqualTo("EFramework.Runtime.UI"));
        }

        [Test]
        public void ConsumerAssemblyCanUseRuntimeEntryAndRuntimeNamespacesTogether()
        {
            Assert.That(typeof(ConsumerApiProbe), Is.Not.Null);
            Assert.That(typeof(ConsumerProcedure).BaseType, Is.EqualTo(typeof(EFrameProcedure)));
            Assert.That(typeof(ConsumerController).BaseType, Is.Not.Null);
            Assert.That(EFrame.Initialized, Is.False);
        }
    }
}
