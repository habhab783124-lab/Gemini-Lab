#nullable enable
using System.IO;
using System.Text;
using GeminiLab.Modules.Persistence;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class SaveSystemTests
    {
        private string _tempRoot = string.Empty;

        private sealed class TestSnapshot
        {
            public string Name = string.Empty;
            public int Value;
        }

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "GeminiLabTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsSnapshot()
        {
            SaveSystem saveSystem = new(saveRootPath: _tempRoot);
            TestSnapshot payload = new() { Name = "demo", Value = 3 };

            saveSystem.SaveAsync("slotA", payload).GetAwaiter().GetResult();
            TestSnapshot? loaded = saveSystem.LoadAsync<TestSnapshot>("slotA").GetAwaiter().GetResult();

            Assert.NotNull(loaded);
            Assert.AreEqual("demo", loaded!.Name);
            Assert.AreEqual(3, loaded.Value);
        }

        [Test]
        public void AesEncryption_WritesEncryptedPayload()
        {
            byte[] key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            byte[] iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");
            SaveSystem saveSystem = new(new AesEncryptionStrategyPlaceholder(key, iv), _tempRoot);
            TestSnapshot payload = new() { Name = "secret", Value = 99 };

            saveSystem.SaveAsync("secureSlot", payload).GetAwaiter().GetResult();
            string raw = File.ReadAllText(Path.Combine(_tempRoot, "secureSlot.sav"));

            Assert.False(raw.Contains("secret"));
        }

        [TestCase("../escape")]
        [TestCase("..\\escape")]
        [TestCase("slot.with.dot")]
        [TestCase("slot/child")]
        public void SaveAsync_InvalidSlot_Throws(string invalidSlot)
        {
            SaveSystem saveSystem = new(saveRootPath: _tempRoot);
            TestSnapshot payload = new() { Name = "x", Value = 1 };

            Assert.Throws<System.ArgumentException>(() => saveSystem.SaveAsync(invalidSlot, payload).GetAwaiter().GetResult());
        }
    }
}
