using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MelonLoader;
using MelonBridge;

namespace MelonLoaderToUMM.Tests
{
    public class DummyMod : MelonMod
    {
        public bool InitCalled;
        public override void OnInitializeMelon() => InitCalled = true;
    }

    public class DummyPlugin : MelonPlugin { }

    [TestFixture]
    public class ModLoaderTests
    {
        [Test]
        public void FindMelonTypes_FindsSubclassesInAssembly()
        {
            var types = ModLoader.FindMelonTypes(Assembly.GetExecutingAssembly());
            Assert.IsTrue(types.Count >= 2, "DummyMod and DummyPlugin should be found");
        }

        [Test]
        public void FindMelonTypes_ExcludesAbstractTypes()
        {
            var types = ModLoader.FindMelonTypes(Assembly.GetExecutingAssembly());
            foreach (var t in types)
                Assert.IsFalse(t.IsAbstract, $"{t.Name} should not be abstract");
        }

        [Test]
        public void CreateInstance_ReturnsMelonBase()
        {
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            Assert.IsInstanceOf<DummyMod>(instance);
        }

        [Test]
        public void InjectInfo_SetsInfoAndLogger()
        {
            var instance = ModLoader.CreateInstance(typeof(DummyMod));
            var attr = new MelonInfoAttribute(typeof(DummyMod), "TestMod", "1.0", "Author");
            ModLoader.InjectInfo(instance, attr);

            Assert.AreEqual("TestMod", instance.Info.Name);
            Assert.IsNotNull(instance.LoggerInstance);
            Assert.AreEqual("TestMod", instance.LoggerInstance.Name);
        }

        [Test]
        public void SortByPriority_SortsAscending()
        {
            var mod1 = ModLoader.CreateInstance(typeof(DummyMod));
            var mod2 = ModLoader.CreateInstance(typeof(DummyMod));
            var mod3 = ModLoader.CreateInstance(typeof(DummyMod));

            var list = new List<(MelonBase, int)>
            {
                (mod1, 10),
                (mod2, 0),
                (mod3, 5),
            };

            var sorted = ModLoader.SortByPriority(list);

            Assert.AreEqual(0, sorted[0].priority);
            Assert.AreEqual(5, sorted[1].priority);
            Assert.AreEqual(10, sorted[2].priority);
        }

        [Test]
        public void LoadAll_NonexistentFolder_ReturnsEmpty()
        {
            var result = ModLoader.LoadAll(@"C:\does_not_exist_12345");
            Assert.IsEmpty(result);
        }
    }
}
