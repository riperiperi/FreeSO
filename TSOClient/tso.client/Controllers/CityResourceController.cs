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
        private static int CallbackID = 0;

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

        private void GetResourceAsync(CityResourceRequestType type, uint shardID, uint id, Action<byte[]> callback)
        {
            callback = CallbackOnMainThread(callback);
            var requestId = GetRequestID();

            Network.CityClient.Write(new CityResourceRequest()
            {
                Type = type,
                RequestID = requestId,
                ResourceID = id,
            });

            Callbacks.TryAdd(requestId, new CityResourceCallback(requestId, callback));
        }

        public void GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            GetResourceAsync(CityResourceRequestType.LOT_THUMBNAIL, shardID, location, callback);
        }

        public void GetFacadeAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            GetResourceAsync(CityResourceRequestType.LOT_FACADE, shardID, location, callback);
        }

        public void GetAvatarDescriptionAsync(uint shardID, uint avatarId, Action<byte[]> callback)
        {
            GetResourceAsync(CityResourceRequestType.AVATAR_DESCRIPTION, shardID, avatarId, callback);
        }

        public void GetCityThumbnailAsync(uint shardID, Action<byte[]> callback)
        {
            GetResourceAsync(CityResourceRequestType.CITY_THUMBNAIL, shardID, 0, callback);
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
