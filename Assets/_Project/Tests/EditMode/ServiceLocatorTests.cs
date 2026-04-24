#nullable enable
using GeminiLab.Core;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ServiceLocatorTests
    {
        private sealed class SampleService
        {
            public int Value { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();
        }

        [Test]
        public void RegisterAndResolve_ReturnsServiceInstance()
        {
            SampleService service = new() { Value = 42 };
            ServiceLocator.Register(service);

            SampleService resolved = ServiceLocator.Resolve<SampleService>();

            Assert.AreSame(service, resolved);
            Assert.AreEqual(42, resolved.Value);
        }

        [Test]
        public void Resolve_MissingService_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => ServiceLocator.Resolve<SampleService>());
        }
    }
}
