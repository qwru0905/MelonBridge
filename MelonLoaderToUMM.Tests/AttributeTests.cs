using NUnit.Framework;
using MelonLoader;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void MelonInfoAttribute_StoresValues()
        {
            var attr = new MelonInfoAttribute(typeof(AttributeTests), "TestMod", "1.0.0", "Author", "http://example.com");

            Assert.AreEqual(typeof(AttributeTests), attr.SystemType);
            Assert.AreEqual("TestMod", attr.Name);
            Assert.AreEqual("1.0.0", attr.Version);
            Assert.AreEqual("Author", attr.Author);
            Assert.AreEqual("http://example.com", attr.DownloadLink);
        }

        [Test]
        public void MelonInfoAttribute_DownloadLink_DefaultsToNull()
        {
            var attr = new MelonInfoAttribute(typeof(AttributeTests), "TestMod", "1.0.0", "Author");
            Assert.IsNull(attr.DownloadLink);
        }

        [Test]
        public void MelonPriorityAttribute_DefaultIsZero()
        {
            var attr = new MelonPriorityAttribute();
            Assert.AreEqual(0, attr.Priority);
        }

        [Test]
        public void MelonPlatformDomainAttribute_DefaultIsAny()
        {
            var attr = new MelonPlatformDomainAttribute();
            Assert.AreEqual(MelonPlatformDomain.Any, attr.Domain);
        }

        [Test]
        public void MelonGameAttribute_AllowsMultiple()
        {
            var attrs = new[]
            {
                new MelonGameAttribute("Dev1", "Game1"),
                new MelonGameAttribute("Dev2", "Game2"),
            };
            Assert.AreEqual("Dev1", attrs[0].Developer);
            Assert.AreEqual("Dev2", attrs[1].Developer);
        }
    }
}
