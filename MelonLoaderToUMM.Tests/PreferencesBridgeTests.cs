using System.IO;
using NUnit.Framework;
using MelonLoader;
using MelonBridge.Bridges;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class PreferencesBridgeTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesBridge.Detach();
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        [Test]
        public void SaveAndLoad_RoundTripsStringEntry()
        {
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("TestCat_" + System.Guid.NewGuid().ToString("N"));
            var entry = category.CreateEntry("myKey", "defaultVal");
            entry.Value = "newVal";

            MelonPreferences.SaveAll();
            entry.Value = "defaultVal";
            MelonPreferences.LoadAll();

            Assert.AreEqual("newVal", entry.Value);
        }

        [Test]
        public void SaveAndLoad_RoundTripsIntEntry()
        {
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("IntCat_" + System.Guid.NewGuid().ToString("N"));
            var entry = category.CreateEntry("count", 0);
            entry.Value = 42;

            MelonPreferences.SaveAll();
            entry.Value = 0;
            MelonPreferences.LoadAll();

            Assert.AreEqual(42, entry.Value);
        }

        [Test]
        public void Load_MissingFile_UsesDefaultValue()
        {
            File.Delete(_tempPath);
            PreferencesBridge.Attach(_tempPath);
            var category = MelonPreferences.CreateCategory("DefaultCat_" + System.Guid.NewGuid().ToString("N"));
            var entry = category.CreateEntry("key", "fallback");

            MelonPreferences.LoadAll();

            Assert.AreEqual("fallback", entry.Value);
        }
    }
}
