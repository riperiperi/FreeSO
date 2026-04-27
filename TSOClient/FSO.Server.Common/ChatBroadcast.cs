using System;
using System.Collections.Generic;

namespace FSO.Server.Common
{
    public class ChatBroadcast
    {
        public static ChatBroadcast Instance;

        private readonly object _lock = new object();
        private readonly List<Action<string>> _subscribers = new List<Action<string>>();

        public IDisposable Subscribe(Action<string> handler)
        {
            lock (_lock)
                _subscribers.Add(handler);
            return new Subscription(this, handler);
        }

        public void Publish(string json)
        {
            Action<string>[] snapshot;
            lock (_lock)
                snapshot = _subscribers.ToArray();
            foreach (var h in snapshot)
            {
                try { h(json); }
                catch { }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ChatBroadcast _hub;
            private readonly Action<string> _handler;

            public Subscription(ChatBroadcast hub, Action<string> handler)
            {
                _hub = hub;
                _handler = handler;
            }

            public void Dispose()
            {
                lock (_hub._lock)
                    _hub._subscribers.Remove(_handler);
            }
        }
    }
}