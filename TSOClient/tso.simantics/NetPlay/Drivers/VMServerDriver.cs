using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.NetPlay.SandboxMode;
using System.Threading;
using FSO.SimAntics.Engine.TSOTransaction;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public class VMServerDriver : VMNetDriver
    {
        private List<VMNetCommand> QueuedCmds;

        private const int TICKS_PER_PACKET = 4;
        private const int INACTIVITY_TICKS_WARN = 15 * 60 * 30;
        private const int INACTIVITY_TICKS_KICK = 20 * 60 * 30;
        private uint ProblemTick;
        private List<VMNetTick> TickBuffer;

        // Networking Abstractions
        private uint LastDesyncTick;
        private List<float> LastDesyncPcts = new List<float>();
        private const int DESYNC_LOOP_FREQ = 90 * 30; //less than 1.5 mins between desyncs indicates there ight be a problem.

        private Dictionary<uint, VMNetClient> Clients;

        private HashSet<VMNetClient> ClientsToDC;
        private HashSet<VMNetClient> ClientsToSync;
        //a subset of ClientsToSync which we should NOT send intermediate ticks to. (since they don't have the lot yet)
        private HashSet<VMNetClient> NewClients;
        //resyncing is a second class action - we will only provide state to resynced clients when there is a minimal amount of history.
        //this is to make sure they do not spend too long waiting for their game to catch up, and to avoid replaying sound effects.
        private HashSet<VMNetClient> ResyncClients;

        //Sync and sync history
        private const int MAX_HISTORY = (30 * 30) / TICKS_PER_PACKET;
        private bool SyncSerializing; //this is set when we begin serializing the state on another thread.
        private byte[] LastSync;
        private List<byte[]> TicksSinceSync;

        public event VMServerBroadcastHandler OnTickBroadcast;
        public delegate void VMServerBroadcastHandler(VMNetMessage msg, HashSet<VMNetClient> ignore);

        public event VMServerDirectHandler OnDirectMessage;
        public delegate void VMServerDirectHandler(VMNetClient target, VMNetMessage msg);

        /// <summary>
        /// Fired when the VM wishes to drop a specific client.
        /// </summary>
        public event VMServerRemoveClientHandler OnDropClient;
        public delegate void VMServerRemoveClientHandler(VMNetClient target);

        public BanList SandboxBans;
        public bool SelfResync;

        private uint TickID = 1;

        public VMServerDriver(IVMTSOGlobalLink globalLink)
        {
            GlobalLink = globalLink;

            Clients = new Dictionary<uint, VMNetClient>();
            ClientsToDC = new HashSet<VMNetClient>();
            ClientsToSync = new HashSet<VMNetClient>();
            NewClients = new HashSet<VMNetClient>();
            ResyncClients = new HashSet<VMNetClient>();
            QueuedCmds = new List<VMNetCommand>();
            TickBuffer = new List<VMNetTick>();

            SandboxBans = new BanList();
        }

        public void ConnectClient(VMNetClient client)
        {
            lock (Clients)
            {
                Clients.Add(client.PersistID, client);
                SendCommand(new VMNetSimJoinCmd
                {
                    ActorUID = client.PersistID,
                    AvatarState = client.AvatarState,
                });
            }
            lock (ClientsToSync)
            {
                if (client.PersistID != uint.MaxValue)
                {
                    ClientsToSync.Add(client);
                    NewClients.Add(client); //note that the lock for clientstosync is valid for newclients too.
                }
            }
        }

        public void RefreshClient(uint id)
        {
            //if connection was lost, resends the lot state to the client as if they connected for the first time.
            //(in case they dropped any packets)
            VMNetClient client = null;
            lock (Clients)
            {
                if (!Clients.TryGetValue(id, out client)) return;
            }

            lock (ClientsToSync)
            {
                if (client.PersistID != uint.MaxValue)
                {
                    ClientsToSync.Add(client);
                    NewClients.Add(client); //note that the lock for clientstosync is valid for newclients too.
                }
            }
        }

        public void DisconnectClient(uint id)
        {
            lock (Clients)
            {
                VMNetClient client = null;
                if (Clients.TryGetValue(id, out client)) DisconnectClient(client);
            }
        }

        public void DisconnectClient(VMNetClient Client)
        {
            lock (Clients) {
                Clients.Remove(Client.PersistID);
                SendCommand(new VMNetSimLeaveCmd
                {
                    ActorUID = Client.PersistID
                });
            }
        }

        private void SendState(VM vm)
        {
            if (ResyncClients.Count != 0 && LastSync == null && !SyncSerializing)
            {
                //only add resync clients when we can give them a (near) clean sync.
                if (TickID - LastDesyncTick > DESYNC_LOOP_FREQ) LastDesyncPcts.Clear();
                LastDesyncTick = TickID;
                LastDesyncPcts.Add(ResyncClients.Count / (float)vm.Context.ObjectQueries.AvatarsByPersist.Count);
                
                foreach (var cli in ResyncClients) //under clientstosync lock
                {
                    ClientsToSync.Add(cli);
                }
                ResyncClients.Clear();
            }
            if (ClientsToSync.Count == 0) return;

            if (LastSync == null && !SyncSerializing)
            {
                SyncSerializing = true;
                TicksSinceSync = new List<byte[]>(); //start saving a history.

                var state = vm.Save(); //must be saved on lot thread. we can serialize elsewhere tho.
                var statecmd = new VMStateSyncCmd { State = state };
                if (vm.Trace != null)
                    statecmd.Traces = vm.Trace.History;
                var cmd = new VMNetCommand(statecmd);

                //currently just hack this on the tick system. might switch later
                var ticks = new VMNetTickList
                {
                    Ticks = new List<VMNetTick> {
                        new VMNetTick {
                            Commands = new List<VMNetCommand> { cmd },
                            RandomSeed = 0, //will be restored by client from cmd
                            TickID = TickID
                        }
                    }
                };

                Task.Run(() =>
                {
                    byte[] data;
                    using (var stream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            ticks.SerializeInto(writer);
                        }
                        data = stream.ToArray();
                    }
                    LastSync = data;
                    SyncSerializing = false;
                });
            }
            else if (LastSync != null)
            {
                foreach (var client in ClientsToSync) {
                    Send(client, new VMNetMessage(VMNetMessageType.BroadcastTick, LastSync));
                    foreach (var tick in TicksSinceSync) //catch this client up with what happened since the last state was created.
                        Send(client, new VMNetMessage(VMNetMessageType.BroadcastTick, tick));
                }
                ClientsToSync.Clear();
                NewClients.Clear(); //note that the lock for clientstosync is valid for newclients too.

                //don't clear last sync and history, since it can be used again if someone wants to join soon.
            }
            //if neither of the above happen, we're waiting for the serialization to complete (in the task)
        }

        public override void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            lock (QueuedCmds)
            {
                QueuedCmds.Add(new VMNetCommand(cmd));
            }
        }

        public override bool Tick(VM vm)
        {
            HandleClients(vm);

            //copy the queue when we can acquire a lock
            List<VMNetCommand> cmdQueue;
            lock (QueuedCmds)
            {
                cmdQueue = new List<VMNetCommand>(QueuedCmds);
                QueuedCmds.Clear();
            }

            //verify the queued commands. Remove ones which fail (or defer til later)
            for (int i = 0; i < cmdQueue.Count; i++)
            {
                var caller = vm.GetAvatarByPersist(cmdQueue[i].Command.ActorUID);
                try
                {
                    if (!cmdQueue[i].Command.Verify(vm, caller)) cmdQueue.RemoveAt(i--);
                }
                catch
                {
                    //verification of a command threw an exception - remove. (and perhaps set a warning on sending client
                    cmdQueue.RemoveAt(i--);
                }
            }

            var tick = new VMNetTick();
            tick.Commands = new List<VMNetCommand>(cmdQueue);
            tick.TickID = TickID;
            if (vm.SpeedMultiplier > 0) tick.TickID = TickID++;
            tick.RandomSeed = vm.Context.RandomSeed;
            cmdQueue.Clear();
            InternalTick(vm, tick);
            
            if (LastDesyncPcts.Count == 6 && LastDesyncPcts.Average() > 0.5f)
            {
                vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, 
                    "Automatic self resync - "+ LastDesyncPcts.Count+" desyncs close by with an average of "+
                    (LastDesyncPcts.Average()*100) + "% affected."));
                LastDesyncPcts.Clear();
                SelfResync = true;
            }
            if (SelfResync)
            {
                SelfResync = false;
                var save = vm.Save();
                vm.Load(save);
                vm.EODHost.SelfResync();
            }

            TickBuffer.Add(tick);

            if (TickBuffer.Count >= TICKS_PER_PACKET)
            {
                lock (ClientsToSync)
                {
                    SendTickBuffer();
                    SendState(vm);
                }
            }

            return true;
        }

        private void SendTickBuffer()
        {
            var ticks = new VMNetTickList { Ticks = TickBuffer };
            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    ticks.SerializeInto(writer);
                }

                data = stream.ToArray();
            }

            if (TicksSinceSync != null)
            {
                if (TicksSinceSync.Count > MAX_HISTORY && !SyncSerializing)
                {
                    //when we have many seconds of ticks for the player to get through,
                    //it might take them a while to catch up, even after assets load
                    //at some point, we need to give up on the state and just save a new one
                    TicksSinceSync = null;
                    LastSync = null;
                }
                else
                {
                    TicksSinceSync.Add(data);
                }
            }

            Broadcast(new VMNetMessage(VMNetMessageType.BroadcastTick, data), NewClients);

            TickBuffer.Clear();
        }

        public override void SendDirectCommand(uint pid, VMNetCommandBodyAbstract acmd)
        {
            VMNetClient cli = null;
            lock (Clients)
            {
                Clients.TryGetValue(pid, out cli);
            }
            if (cli != null) SendDirectCommand(cli, acmd);
        }

        public void SendDirectCommand(VMNetClient client, VMNetCommandBodyAbstract acmd) 
        {
            if (client.RemoteIP == "local")
            {
                //just breaking sandbox a little
                SendCommand(acmd);
                return;
            }

            var cmd = new VMNetCommand(acmd);
            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    cmd.SerializeInto(writer);
                }

                data = stream.ToArray();
            }

            Send(client, new VMNetMessage(VMNetMessageType.Direct, data));
        }

        private void Send(VMNetClient client, VMNetMessage message)
        {
            if (OnDirectMessage != null) OnDirectMessage(client, message);
        }

        private void Broadcast(VMNetMessage message, HashSet<VMNetClient> ignore)
        {
            if (OnTickBroadcast != null) OnTickBroadcast(message, ignore);
        }

        private void DropClient(VMNetClient client)
        {
            if (OnDropClient != null) OnDropClient(client);
        }

        private void HandleClients(VM vm)
        {
            lock (Clients)
            {
                ClientsToDC.Clear();
                foreach (var client in Clients)
                {
                    //does client have an avatar? did they have one before?
                    if (vm.GetAvatarByPersist(client.Key) != null) { client.Value.HadAvatar = true; }
                    else if (client.Value.HadAvatar)
                    {
                        //something removed this avatar. They have disconnected.
                        ClientsToDC.Add(client.Value);
                        continue;
                    }
                    if (++client.Value.InactivityTicks >= INACTIVITY_TICKS_WARN)
                    {
                        if (client.Value.InactivityTicks >= INACTIVITY_TICKS_KICK) ClientsToDC.Add(client.Value);
                        else if (client.Value.InactivityTicks == INACTIVITY_TICKS_WARN) SendDirectCommand(client.Value,
                            new VMNetTimeoutNotifyCmd() { TimeRemaining = (INACTIVITY_TICKS_KICK - INACTIVITY_TICKS_WARN) / 30 });
                    }
                    var packets = client.Value.GetMessages();
                    while (packets.Count > 0)
                    {
                        HandleMessage(client.Value, packets.Dequeue());
                    }
                }
                foreach (var client in ClientsToDC)
                {
                    if (client.FatalDCMessage != null)
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, client.FatalDCMessage));
                    DropClient(client);
                }
            }
        }

        public void SubmitMessage(uint id, VMNetMessage msg)
        {
            lock (Clients)
            {
                VMNetClient cli = null;
                if (Clients.TryGetValue(id, out cli))
                {
                    cli.SubmitMessage(msg);
                }
            }
        }

        public void SendGenericMessage(uint id, string title, string msg)
        {
            lock (Clients)
            {
                VMNetClient client = null;
                if (Clients.TryGetValue(id, out client)) SendGenericMessage(client, title, msg);
            }
        }

        private void SendGenericMessage(VMNetClient client, string title, string msg)
        {
            SendDirectCommand(client, new VMGenericDialogCommand
            {
                Title = title,
                Message = msg
            });
        }

        public void HandleMessage(VMNetClient client, VMNetMessage message)
        {
            var cmd = new VMNetCommand();
            try {
                using (var reader = new BinaryReader(new MemoryStream(message.Data))) {
                    if (!cmd.TryDeserialize(reader, false)) return; //ignore things that should never be sent to the server
                }
            }
            catch (Exception e)
            {
                //corrupt commands are currently a death sentence for the client. nothing should be corrupt over TCP except in rare cases.
                client.FatalDCMessage = "RECEIVED BAD COMMAND: " + e.ToString();
                ClientsToDC.Add(client);
                return;
            }

            if (cmd.Type == VMCommandType.SimJoin)
            {
                //note: currently avatars no longer send sim join commands, the server does when it gets the database info.
                if (SandboxBans.Contains(client.RemoteIP))
                {
                    SendGenericMessage(client, "Banned", "You have been banned from this sandbox server!");
                    ClientsToDC.Add(client);
                    return;
                }
                else if (((VMNetSimJoinCmd)cmd.Command).Version != VMNetSimJoinCmd.CurVer)
                {
                    SendGenericMessage(client, "Version Mismatch", "Your game version does not match the server's. Please update your game.");
                    ClientsToDC.Add(client);
                    return;
                }
                SendCommand(cmd.Command); //just go for it. will start the avatar retrieval process.
                return;
            }
            else if (cmd.Type == VMCommandType.RequestResync)
            {
                lock (ClientsToSync)
                {
                    //todo: throttle
                    ProblemTick = ((VMRequestResyncCmd)cmd.Command).TickID;
                    ResyncClients.Add(client); //under clientstosync lock
                }
            } else if (cmd.Type != VMCommandType.ChatParameters)
            {
                client.InactivityTicks = 0;
            }

            cmd.Command.ActorUID = client.PersistID;
            SendCommand(cmd.Command);
        }

        public void BanUser(VM vm, string name)
        {
            var sims = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == name.ToLowerInvariant().Trim(' '));
            lock (Clients) {
                foreach (var sim in sims)
                {
                    if (Clients.ContainsKey(sim.PersistID))
                    {
                        var client = Clients[sim.PersistID];
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Found and banned " + name + ", with IP " + client.RemoteIP + "."));
                        BanIP(client.RemoteIP);
                    }
                }
            }
        }

        public void DropAvatar(VMAvatar avatar)
        {
            if (avatar == null || avatar.PersistID == 0) return;
            lock (Clients)
            {
                if (Clients.ContainsKey(avatar.PersistID))
                {
                    
                    var client = Clients[avatar.PersistID];
                    Task.Run(() =>
                    {
                        DropClient(client);
                    });
                }
            }
        }

        public void KickUser(VM vm, string name)
        {
            var sims = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == name.ToLowerInvariant().Trim(' '));
            foreach (var sim in sims)
            {
                DropAvatar((VMAvatar)sim);
            }
        }

        public void BanIP(string ip)
        {
            var cleanIP = ip.Trim(' ').ToLowerInvariant();
            var badClients = new List<VMNetClient>();
            lock (Clients)
            {
                foreach (var client in Clients.Values)
                {
                    if (client.RemoteIP.ToLowerInvariant() == cleanIP) {
                        SendGenericMessage(client, "Yikes", "You have just been banned from this sandbox server!");
                        badClients.Add(client);
                    }
                }
            }
            foreach (var client in badClients) {
                new Thread(() =>
                {
                    DropClient(client);
                }).Start();
            }
            SandboxBans.Add(cleanIP);
        }

        public override string GetUserIP(uint uid)
        {
            lock (Clients)
            {
                VMNetClient client = null;
                Clients.TryGetValue(uid, out client);
                if (client == null) return "unknown";
                return client.RemoteIP;
            }
        }
    }
}
