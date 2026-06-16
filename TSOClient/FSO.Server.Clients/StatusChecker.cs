using FSO.Server.Protocol.Aries.Packets;
using Ninject;

namespace FSO.Server.Clients
{
    public readonly struct StatusCheckResult(bool isOnline, string name = "", string version = "", int players = 0)
    {
        public readonly bool IsOnline = isOnline;
        public readonly string Name = name;
        public readonly string Version = version;
        public readonly int Players = players;
    }

    public static class StatusChecker
    {
        private class ArchiveStatusChecker : IAriesEventSubscriber, IAriesMessageSubscriber
        {
            private readonly TaskCompletionSource<StatusCheckResult> Source = new();
            public Task<StatusCheckResult> Task => Source.Task;

            public void MessageReceived(AriesClient client, object message)
            {
                if (message is RequestClientSessionArchive data)
                {
                    Source.TrySetResult(new StatusCheckResult(
                        true,
                        data.Name,
                        "v1.0.0",
                        data.PlayerCount
                    ));
                }
            }

            public void InputClosed(AriesClient session)
            {
                // Note: if there's already a result by the time the socket closes, it won't be overwritten.
                Source.TrySetResult(new StatusCheckResult(false));
            }

            public void SessionClosed(AriesClient client)
            {
                Source.TrySetResult(new StatusCheckResult(false));
            }

            public void SessionCreated(AriesClient client)
            {
            }

            public void SessionIdle(AriesClient client)
            {
            }

            public void SessionOpened(AriesClient client)
            {
            }
        }

        public static async Task<StatusCheckResult> FreeSOStatus(string address)
        {
            try
            {
                using (var client = new ApiClient(address))
                {
                    ApiStatus status = await client.GetStatus();

                    if (status == null)
                    {
                        return new StatusCheckResult(false);
                    }
                    else
                    {
                        return new StatusCheckResult(true, status.name, status.version, status.onlineCount);
                    }
                }
            }
            catch (Exception)
            {
                return new StatusCheckResult(false);
            }
        }

        public static async Task<StatusCheckResult> ArchiveStatus(IKernel kernel, string address)
        {
            var client = new AriesClient(kernel)
            {
                Timeout = 2000
            };

            var checker = new ArchiveStatusChecker();

            // Add handlers.
            client.AddSubscriber(checker);

            try
            {
                client.Connect(address);
            }
            catch (Exception)
            {
                return new StatusCheckResult(false);
            }

            var result = await checker.Task;

            client.Disconnect();

            return result;
        }
    }
}
