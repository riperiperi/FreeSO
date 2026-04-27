using FSO.Common.Domain;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Content.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Utils;
using Microsoft.Xna.Framework;
using NLog;
using System.Collections.Concurrent;

namespace FSO.Server.Servers.City.Handlers
{
    internal class CityUpdateHandler : IDisposable
    {
        private struct CityUpdate(int shardId, Color[] roads, Color[] elevation, Color[] forestDensity, Color[] forestType, Color[] terrainType)
        {
            public readonly int ShardID = shardId;
            public readonly Color[] Roads = roads;
            public readonly Color[] Elevation = elevation;
            public readonly Color[] ForestDensity = forestDensity;
            public readonly Color[] ForestType = forestType;
            public readonly Color[] TerrainType = terrainType;
        }

        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly CityServerContext Context;
        private readonly IRealestateDomain Realestate;
        private readonly IDAFactory DAFactory;
        private readonly IServerNFSProvider NFS;

        private bool Running;

        private readonly ConcurrentQueue<Action> SerialActions = [];
        private readonly AutoResetEvent ActionReady;
        private readonly Thread SerialThread;

        private readonly HashSet<IShardRealestateDomain> ModifiedShards = [];
        private readonly Dictionary<int, CityUpdate> UpdateByShard = [];
        private readonly AutoResetEvent UpdateReady;
        private readonly Thread UpdateThread;

        public CityUpdateHandler(CityServerContext context, IRealestateDomain realestate, IDAFactory daFactory, IServerNFSProvider nfs)
        {
            Context = context;
            Realestate = realestate;
            DAFactory = daFactory;
            NFS = nfs;

            Running = true;

            ActionReady = new AutoResetEvent(false);

            // All city actions (and handling initial distribution of the city data + delta list) happen in sequence.
            // This ensures that the sequence is completely synchronized for all clients.
            SerialThread = new Thread(ThreadLoop);
            SerialThread.Start();

            UpdateReady = new AutoResetEvent(false);

            // This thread saves the city data to PNG.
            UpdateThread = new Thread(UpdateLoop);
            UpdateThread.Start();
        }

        private void TrySaveLot()
        {
            foreach (var shard in ModifiedShards)
            {
                var shardId = shard.ID;
                var map = shard.GetMap();

                lock (UpdateByShard)
                {
                    if (UpdateByShard.ContainsKey(shardId))
                    {
                        continue;
                    }
                }

                var dirty = map.ConsumeDirty();

                if (dirty != CityMapAspects.None)
                {
                    var update = new CityUpdate(
                        shardId,
                        dirty.HasFlag(CityMapAspects.Road) ? [.. map.RoadData.Select(x => new Color(x, x, x, (byte)255))] : null,
                        dirty.HasFlag(CityMapAspects.Elevation) ? [.. map.ElevationData.Select(x => new Color(x, x, x, (byte)255))] : null,
                        dirty.HasFlag(CityMapAspects.Forest) ? [.. map.ForestDensityData.Select(x => new Color(x, x, x, (byte)255))] : null,
                        dirty.HasFlag(CityMapAspects.Forest) ? [.. map.ForestTypeColorData] : null,
                        dirty.HasFlag(CityMapAspects.TerrainType) ? [.. map.TerrainTypeColorData] : null);

                    lock (UpdateByShard)
                    {
                        UpdateByShard.Add(shardId, update);
                    }
                    UpdateReady.Set();
                }
            }
        }

        private void ThreadLoop()
        {
            while (Running)
            {
                while (SerialActions.TryDequeue(out var action))
                {
                    action();
                }

                TrySaveLot();

                ActionReady.WaitOne();
            }
        }

        private void SaveCityPNGs(in CityUpdate update)
        {
            var baseDir = NFS.GetShardMapDirectory(update.ShardID);

            SaveTex(baseDir, "roadmap", update.Roads);

            SaveTex(baseDir, "elevation", update.Elevation);

            SaveTex(baseDir, "forestdensity", update.ForestDensity);
            SaveTex(baseDir, "foresttype", update.ForestType);

            SaveTex(baseDir, "terraintype", update.TerrainType);

            lock (UpdateByShard)
            {
                UpdateByShard.Remove(update.ShardID);
            }
        }

        private void UpdateLoop()
        {
            List<CityUpdate> updates = [];
            while (Running)
            {
                lock (UpdateThread)
                {
                    updates.AddRange(UpdateByShard.Values);
                }

                if (updates.Count > 0)
                {
                    foreach (var update in updates)
                    {
                        SaveCityPNGs(in update);
                    }

                    updates.Clear();

                    // Process the action thread again in case there are some dirty aspects that need saving again.
                    ActionReady.Set();
                }

                UpdateReady.WaitOne();
            }
        }

        private void QueueAction(Action action)
        {
            SerialActions.Enqueue(action);

            ActionReady.Set();
        }

        private IShardRealestateDomain GetShard(IVoltronSession session)
        {
            if (session.IsAnonymous)
                return null;

            if (!session.HasModerationLevel(1))
                return null;

            if (!(Context.Config.Archive?.Flags.HasFlag(FSO.Common.ArchiveConfigFlags.CityEditor) ?? false))
                return null;

            var shard = Realestate.GetByShard(Context.ShardId);

            return shard.Dynamic ? shard : null;
        }

        public async void Handle(IVoltronSession session, CityUpdateCommand packet)
        {
            var shard = GetShard(session);

            if (shard == null || Running == false)
                return;

            QueueAction(() =>
            {
                switch (packet.Mode)
                {
                    case CityUpdateCommandMode.SetCityName:
                        // TODO: validate

                        using (var da = DAFactory.Get())
                        {
                            da.Shards.UpdateInfo(Context.ShardId, packet.CityName, "dynamic");
                        }

                        // Make sure everyone knows about the change.
                        Context.Broadcast(packet);
                        break;
                    case CityUpdateCommandMode.SetThumbnail:
                        if (CoreImageLoader.ValidatePNG(packet.Thumbnail, 180, 135))
                        {
                            var dir = NFS.GetShardMapDirectory(Context.ShardId);
                            var imgpath = Path.Combine(dir, "thumbnail.png");

                            Directory.CreateDirectory(dir);

                            using (FileStream fs = File.Open(imgpath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                fs.Write(packet.Thumbnail, 0, packet.Thumbnail.Length);
                            }
                        }
                        break;
                    case CityUpdateCommandMode.Undo:
                        if (session.AvatarId != packet.AvatarID)
                            return;

                        // TODO: proper ordering, thread safety
                        if (shard.HandleUserCommand(packet))
                        {
                            Context.Broadcast(packet);
                            ModifiedShards.Add(shard);
                        }

                        break;
                }
            });
        }

        public async void Handle(IVoltronSession session, CityInitRequest packet)
        {
            var shard = GetShard(session);

            if (shard == null)
                return;

            var attr = session.GetAttribute("hasInitCity");

            if (!(attr is string strAttr && strAttr == "true"))
            {
                session.SetAttribute("hasInitCity", "true");

                QueueAction(() =>
                {
                    var init = shard.GetInit();

                    session.Write(init);
                });
            }
        }

        public async void Handle(IVoltronSession session, CityUpdateRequest packet)
        {
            var shard = GetShard(session);

            if (shard == null)
                return;

            var cmd = packet.Command.Command;
            cmd.AvatarId = session.AvatarId;

            if (cmd.IsTemp)
            {
                // Temp commands aren't reflected in the city - they are forwarded to everyone else though.

                return;
            }

            // TODO: Add reserved locations from the server (any open lots)

            QueueAction(() =>
            {
                int id = shard.AppendCommand(cmd);

                if (id != -1)
                {
                    Context.Broadcast(new CityUpdateResponse()
                    {
                        StartIndex = id,
                        Commands = [new(cmd)]
                    });

                    ModifiedShards.Add(shard);
                }
            });
        }

        private static void SaveTex(string baseDir, string filename, Color[] data)
        {
            // Save as a temp file, then rename over the existing one.
            // This avoids the target file ever being half written.

            if (data == null)
            {
                return;
            }

            string tempPath = Path.Combine(baseDir, $"{filename}-temp.png");
            string filePath = Path.Combine(baseDir, $"{filename}.png");

            Directory.CreateDirectory(baseDir);

            using (FileStream fs = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CoreImageLoader.SavePNG(data, 512, 512, fs);
            }

            File.Move(tempPath, filePath, true);
        }

        public void Dispose()
        {
            Running = false;
            ActionReady.Set();
            UpdateReady.Set();

            SerialThread.Join();
            UpdateThread.Join();

            ActionReady.Dispose();
            UpdateReady.Dispose();
        }
    }
}
