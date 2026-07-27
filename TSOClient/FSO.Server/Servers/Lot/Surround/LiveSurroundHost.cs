using FSO.Common.Domain.Realestate;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Servers.Lot.Domain;
using System.Diagnostics;

namespace FSO.Server.Servers.Lot.Surround
{
    internal class LiveSurroundHost : IDisposable
    {
        private Lock ConnectionsLock = new();
        private readonly Dictionary<uint, LiveSurroundLotConnection> ConnectionsById = [];
        private readonly Dictionary<uint, List<LiveSurroundLotConnection>> AdjacencyById = [];
        private long LastTick;
        private bool Active = true;

        private uint TickID = 0;

        private readonly Dictionary<uint, SurroundPuppetLot?> EvaluatedLots = [];

        public LiveSurroundHost()
        {
            var thread = new Thread(Run);
            thread.IsBackground = true;

            thread.Start();
        }

        private uint OffsetCoords(uint location, int x, int y)
        {
            var loc = MapCoordinates.Unpack(location);
            var loc2 = MapCoordinates.Offset(loc, x, y);
            return MapCoordinates.Pack(loc2.X, loc2.Y);
        }

        public void Run()
        {
            var tickPerMs = Stopwatch.Frequency / 1000;
            LastTick = Stopwatch.GetTimestamp();
            while (Active)
            {
                SendSurrounds();

                long nextTick = LastTick + (33 * tickPerMs);
                long sleepTime = nextTick - Stopwatch.GetTimestamp();

                if (sleepTime < (-33 * tickPerMs))
                {
                    // Too far in the past, reset the timer.
                    sleepTime = 0;
                }

                if (sleepTime > 0)
                {
                    Thread.Sleep((int)Math.Max(0, sleepTime / tickPerMs));
                }

                LastTick = nextTick;
            }
        }

        private void SendSurrounds()
        {
            var evaluatedLots = EvaluatedLots;
            evaluatedLots.Clear();

            lock (ConnectionsLock)
            {
                // Any lots with adjacency should send their tick data to the adjacent lots.
                foreach (var target in AdjacencyById)
                {
                    if (ConnectionsById.TryGetValue(target.Key, out var conn))
                    {
                        // Build a broadcast packet for users in target, using all the surround data
                        FSOVMSurroundPuppets puppets = new();
                        var lots = new List<SurroundPuppetLot>();

                        bool adjUpdated = conn.ConsumeDirty();

                        foreach (var adj in target.Value)
                        {
                            if (!evaluatedLots.TryGetValue(adj.LotLocation, out SurroundPuppetLot? value))
                            {
                                value = adj.PullTick();

                                evaluatedLots[adj.LotLocation] = value;
                            }

                            if (value.HasValue)
                            {
                                var lot = value.Value;

                                if (adjUpdated)
                                {
                                    lot.ForceDirty = true;
                                }

                                lots.Add(lot);
                            }
                        }

                        puppets.Ticks = [
                            new SurroundPuppetTick()
                            {
                                TickID = TickID,
                                Lots = [.. lots]
                            }
                        ];

                        conn.Broadcast(puppets);
                    }
                }
            }

            TickID++;
        }

        public LiveSurroundLotConnection Connect(uint location, LotContainer lotContainer, ILotHost lotHost)
        {
            var connection = new LiveSurroundLotConnection(this, lotContainer, lotHost, location);

            lock (ConnectionsLock)
            {
                if (ConnectionsById.ContainsKey(location))
                {
                    Disconnect(location);
                }

                ConnectionsById[location] = connection;

                // Try and work out adjacency.
                for (int y = -1; y < 2; y++)
                {
                    for (int x = -1; x < 2; x++)
                    {
                        if (x == 0 && y == 0) continue;

                        uint otherLocation = OffsetCoords(location, x, y);
                        if (ConnectionsById.TryGetValue(otherLocation, out var other))
                        {
                            if (!AdjacencyById.TryGetValue(otherLocation, out var otherAdj))
                            {
                                otherAdj = [];
                                AdjacencyById.Add(otherLocation, otherAdj);
                            }

                            otherAdj.Add(connection);

                            if (!AdjacencyById.TryGetValue(location, out var adj))
                            {
                                adj = [];
                                AdjacencyById.Add(location, adj);
                            }

                            adj.Add(other);

                            connection.NotifyAdjacent();
                            other.NotifyAdjacent();
                        }
                    }
                }
            }

            return connection;
        }

        public void Disconnect(uint location)
        {
            lock (ConnectionsLock)
            {
                if (ConnectionsById.TryGetValue(location, out var connection))
                {
                    ConnectionsById.Remove(location);

                    if (AdjacencyById.TryGetValue(location, out var adj))
                    {
                        AdjacencyById.Remove(location);

                        foreach (var otherConn in adj)
                        {
                            if (AdjacencyById.TryGetValue(otherConn.LotLocation, out var adj2))
                            {
                                adj2.Remove(connection);

                                if (adj2.Count == 0)
                                {
                                    AdjacencyById.Remove(otherConn.LotLocation);
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool HollowBroadcast(LiveSurroundLotConnection conn, Action<Action<byte[]>> generateHollow)
        {
            LiveSurroundLotConnection[] adj = null;

            lock (ConnectionsLock)
            {
                if (AdjacencyById.TryGetValue(conn.LotLocation, out var adjList) && adjList.Count > 0)
                {
                    adj = [.. adjList];
                }
            }

            if (adj != null)
            {
                generateHollow((data) =>
                {
                    foreach (var conn2 in adj)
                    {
                        conn2.SendHollowLotData(conn.LotLocation, data);
                    }
                });

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            Active = false;
        }
    }
}
