using FSO.Server.Common;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class LotServerPicker
    {
        private List<LotServerState> Servers = new List<LotServerState>();
        private Dictionary<string, LotServerState> ServersByCallsign = new Dictionary<string, LotServerState>();
        private TaskCompletionSource<bool> AllServersShutdown;
        private HashSet<IGluonSession> ServersShutdown;

        public Task<object> Pick(uint claimId)
        {
            return null;
        }

        public LotPickerAttempt PickServer()
        {
            lock (Servers)
            {
                var best = Servers.OrderByDescending(x => x.Rank).FirstOrDefault();
                if(best == null || best.Rank <= 0)
                {
                    return new LotPickerAttempt (null) { Success = false };
                }

                var attempt = new LotPickerAttempt(best) { Success = true };
                best.InFlight.Add(attempt);
                return attempt;
            }
        }

        public IGluonSession GetLotServerSession(string callSign)
        {
            lock (Servers)
            {
                IGluonSession result = null;
                LotServerState state = null;
                if (ServersByCallsign.TryGetValue(callSign, out state)) result = state.Session;
                return result;
            }
        }

        public async Task<bool> ShutdownAllLotServers(ShutdownType type)
        {
            if (AllServersShutdown != null) return false;
            AllServersShutdown = new TaskCompletionSource<bool>();
            ServersShutdown = new HashSet<IGluonSession>();
            lock (Servers)
            {
                lock (ServersShutdown) {
                    foreach (var server in Servers)
                    {
                        var s = server.Session;
                        ServersShutdown.Add(s);
                        s.Write(new ShardShutdownRequest
                        {
                            ShardId = 0,
                            Type = type
                        });
                    }
                }
            }
            return await AllServersShutdown.Task;
        }

        public void RegisterShutdown(IGluonSession session)
        {
            lock (ServersShutdown)
            {
                ServersShutdown.Remove(session);
                if (ServersShutdown.Count == 0) AllServersShutdown.SetResult(true);
            }
        }

        public void UpdateServerAdvertisement(IGluonSession session, AdvertiseCapacity request)
        {
            lock (Servers)
            {
                var state = GetState(session);
                if(state == null){
                    state = new LotServerState {
                        Session = session
                    };
                    Servers.Add(state);
                    if (ServersByCallsign.ContainsKey(session.CallSign))
                    {
                        ServersByCallsign[session.CallSign] = state;
                    }
                    else
                    {
                        ServersByCallsign.Add(session.CallSign, state);
                    }
                }
                state.Session = session; //can be hot-swapped if we re-establish connection. TODO: verify and look for race conditions
                state.MaxLots = request.MaxLots;
                state.CurrentLots = request.CurrentLots;
                state.CpuPercentAvg = request.CpuPercentAvg;
                state.RamAvaliable = request.RamAvaliable;
                state.RamUsed = request.RamUsed;
            }
        }

        private LotServerState GetState(IGluonSession session)
        {
            var server = Servers.FirstOrDefault(x => x.Session == session);
            return server;
        }
    }
    
    public class LotServerState
    {
        public IGluonSession Session;
        public List<LotPickerAttempt> InFlight = new List<LotPickerAttempt>();
        
        public short MaxLots;
        public short CurrentLots;
        public byte CpuPercentAvg;
        public long RamUsed;
        public long RamAvaliable;

        public int Rank
        {
            get
            {
                //Dumb rank for now
                var lots = CurrentLots + InFlight.Count;
                return MaxLots - lots;
            }
        }
    }

    public class LotPickerAttempt
    {
        public bool Success;
        private LotServerState State;

        public LotPickerAttempt(LotServerState state)
        {
            this.State = state;
        }

        public void Free()
        {
            State.InFlight.Remove(this);
        }

        public IGluonSession Session
        {
            get { return State.Session; }
        }
    }
}
