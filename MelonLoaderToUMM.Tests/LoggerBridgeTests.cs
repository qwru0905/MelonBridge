using System.Collections.Generic;
using NUnit.Framework;
using MelonLoader;
using MelonBridge.Bridges;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class LoggerBridgeTests
    {
        private class FakeLogger : LoggerBridge.IUmmLogger
        {
            public List<string> Logs { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public List<string> Errors { get; } = new List<string>();

            public void Log(string msg) => Logs.Add(msg);
            public void Warning(string msg) => Warnings.Add(msg);
            public void Error(string msg) => Errors.Add(msg);
        }

        [TearDown]
        public void TearDown() => LoggerBridge.Detach();

        [Test]
        public void Attach_RoutesMsgToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Msg("hello");

            CollectionAssert.Contains(fake.Logs, "hello");
        }

        [Test]
        public void Attach_RoutesWarningToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Warning("warn");

            CollectionAssert.Contains(fake.Warnings, "warn");
        }

        [Test]
        public void Attach_RoutesErrorToUmmLogger()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);

            MelonLogger.Error("err");

            CollectionAssert.Contains(fake.Errors, "err");
        }

        [Test]
        public void Detach_StopsRouting()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);
            LoggerBridge.Detach();

            MelonLogger.Msg("after detach");

            CollectionAssert.IsEmpty(fake.Logs);
        }

        [Test]
        public void Instance_PrefixesName()
        {
            var fake = new FakeLogger();
            LoggerBridge.Attach(fake);
            var instance = new MelonLogger.Instance("MyMod");

            instance.Msg("test");

            CollectionAssert.Contains(fake.Logs, "[MyMod] test");
        }
    }
}
