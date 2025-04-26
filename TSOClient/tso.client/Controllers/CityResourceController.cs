using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Packets;
using Ninject.Activation;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FSO.Client.Controllers
{
    public class CityResourceController : IAriesMessageSubscriber, IDisposable
    {
        private struct CityResourceCallback
        {
            public uint RequestID;
            public Action<byte[]> Callback;

            public CityResourceCallback(uint requestID, Action<byte[]> callback)
            {
                RequestID = requestID;
                Callback = callback;
            }
        }

        private Network.Network Network;
        private ConcurrentDictionary<uint, CityResourceCallback> Callbacks;
        private int CallbackID = 0;

        public CityResourceController(Network.Network network)
        {
            Network = network;
            Callbacks = new ConcurrentDictionary<uint, CityResourceCallback>();

            Network.CityClient.AddSubscriber(this);
        }

        private uint GetRequestID()
        {
            return (uint)Interlocked.Increment(ref CallbackID);
        }

        private Action<byte[]> CallbackOnMainThread(Action<byte[]> callback)
        {
            return (data) =>
            {
                GameThread.NextUpdate(x =>
                {
                    callback(data.Length == 0 ? null : data);
                });
            };
        }

        public void GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            callback = CallbackOnMainThread(callback);
            var requestId = GetRequestID();

            Network.CityClient.Write(new CityResourceRequest()
            {
                Type = CityResourceRequestType.LOT_THUMBNAIL,
                RequestID = requestId,
                ResourceID = location,
            });

            Callbacks.TryAdd(requestId, new CityResourceCallback(requestId, callback));
        }

        public void GetFacadeAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            callback = CallbackOnMainThread(callback);
            var requestId = GetRequestID();

            Network.CityClient.Write(new CityResourceRequest()
            {
                Type = CityResourceRequestType.LOT_FACADE,
                RequestID = requestId,
                ResourceID = location,
            });

            Callbacks.TryAdd(requestId, new CityResourceCallback(requestId, callback));
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is CityResourceResponse res)
            {
                if (Callbacks.TryRemove(res.RequestID, out CityResourceCallback cb))
                {
                    cb.Callback(res.Data);
                }
            }
        }

        public void Dispose()
        {
            Network.CityClient.RemoveSubscriber(this);
        }
    }
}
