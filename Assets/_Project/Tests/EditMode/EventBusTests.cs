#nullable enable
using GeminiLab.Core.Events;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_WhenSubscribed_InvokesCallback()
        {
            EventBus bus = new();
            int received = 0;
            bus.Subscribe<int>(value => received = value);

            bus.Publish(7);

            Assert.AreEqual(7, received);
        }

        [Test]
        public void DisposeSubscription_StopsReceivingEvents()
        {
            EventBus bus = new();
            int calls = 0;
            System.IDisposable token = bus.Subscribe<int>(_ => calls++);

            bus.Publish(1);
            token.Dispose();
            bus.Publish(2);

            Assert.AreEqual(1, calls);
        }
    }
}
