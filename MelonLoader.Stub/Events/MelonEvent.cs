using System;
using System.Collections.Generic;

namespace MelonLoader
{
    public class MelonEvent
    {
        private readonly List<Action> _subscribers = new List<Action>();

        public void Subscribe(Action callback, int priority = 0, bool once = false)
        {
            if (callback != null)
                _subscribers.Add(callback);
        }

        public void Unsubscribe(Action callback)
        {
            _subscribers.Remove(callback);
        }

        internal void Invoke()
        {
            foreach (var sub in _subscribers.ToArray())
            {
                try { sub(); }
                catch (Exception e) { MelonLogger.Error(e.ToString()); }
            }
        }
    }

    public class MelonEvent<T1>
    {
        private readonly List<Action<T1>> _subscribers = new List<Action<T1>>();

        public void Subscribe(Action<T1> callback, int priority = 0, bool once = false)
        {
            if (callback != null)
                _subscribers.Add(callback);
        }

        public void Unsubscribe(Action<T1> callback)
        {
            _subscribers.Remove(callback);
        }

        internal void Invoke(T1 arg)
        {
            foreach (var sub in _subscribers.ToArray())
            {
                try { sub(arg); }
                catch (Exception e) { MelonLogger.Error(e.ToString()); }
            }
        }
    }
}
