using System;
using System.Collections.Generic;
using NUnit.Framework;
using MelonLoader;

namespace MelonLoaderToUMM.Tests
{
    [TestFixture]
    public class MelonEventTests
    {
        [Test]
        public void MelonEvent_Subscribe_ReceivesInvoke()
        {
            var melonEvent = new MelonEvent();
            var called = false;
            melonEvent.Subscribe(() => called = true);

            melonEvent.Invoke();

            Assert.IsTrue(called);
        }

        [Test]
        public void MelonEvent_Unsubscribe_DoesNotReceiveInvoke()
        {
            var melonEvent = new MelonEvent();
            var called = false;
            Action callback = () => called = true;
            melonEvent.Subscribe(callback);
            melonEvent.Unsubscribe(callback);

            melonEvent.Invoke();

            Assert.IsFalse(called);
        }

        [Test]
        public void MelonEvent_MultipleSubscribers_AllCalled()
        {
            var melonEvent = new MelonEvent();
            var results = new List<int>();
            melonEvent.Subscribe(() => results.Add(1));
            melonEvent.Subscribe(() => results.Add(2));

            melonEvent.Invoke();

            CollectionAssert.AreEqual(new[] { 1, 2 }, results);
        }

        [Test]
        public void MelonEventT_PassesArgument()
        {
            var melonEvent = new MelonEvent<string>();
            string? received = null;
            melonEvent.Subscribe(s => received = s);

            melonEvent.Invoke("hello");

            Assert.AreEqual("hello", received);
        }

        [Test]
        public void MelonEvent_ThrowingSubscriber_DoesNotBreakOthers()
        {
            var melonEvent = new MelonEvent();
            var secondCalled = false;
            melonEvent.Subscribe(() => throw new Exception("boom"));
            melonEvent.Subscribe(() => secondCalled = true);

            melonEvent.Invoke();

            Assert.IsTrue(secondCalled);
        }
    }
}
