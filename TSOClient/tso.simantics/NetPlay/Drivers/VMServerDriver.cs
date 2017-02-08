/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.NetPlay.SandboxMode;
using System.Threading;
using FSO.SimAntics.Engine.TSOTransaction;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Drivers
{
    public class VMServerDriver : VMNetDriver
    {
        private List<VMNetCommand> QueuedCmds;

        private const int TICKS_PER_PACKET = 4;
        private uint ProblemTick;
        private List<VMNetTick> TickBuffer;

        // Networking Abstractions

        private Dictionary<uint, VMNetClient> Clients;

        private HashSet<VMNetClient> ClientsToDC;
        private HashSet<VMNetClient> ClientsToSync;

        private const int CLIENT_RESYNC_COOLDOWN = 30*30;
        private int LastResyncCooldown = 0;
        private HashSet<VMNetClient> ClientsToSyncLater;

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

        private uint TickID = 1;

        public VMServerDriver(IVMTSOGlobalLink globalLink)
        {
            GlobalLink = globalLink;

            Clients = new Dictionary<uint, VMNetClient>();
            ClientsToDC = new HashSet<VMNetClient>();
            ClientsToSync = new HashSet<VMNetClient>();
            ClientsToSyncLater = new HashSet<VMNetClient>();
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
                ClientsToSync.Add(client);
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
            if (ClientsToSync.Count == 0) return;

            var watch = new Stopwatch();
            watch.Start();

            var state = vm.Save();
            var statecmd = new VMStateSyncCmd { State = state };
#if VM_DESYNC_DEBUG
            statecmd.Trace = vm.Trace.GetTick(ProblemTick);
#endif
            var cmd = new VMNetCommand(statecmd);

            //currently just hack this on the tick system. might switch later
            var ticks = new VMNetTickList { Ticks = new List<VMNetTick> {
                new VMNetTick {
                    Commands = new List<VMNetCommand> { cmd },
                    RandomSeed = 0, //will be restored by client from cmd
                    TickID = TickID
                }
            } };

            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    ticks.SerializeInto(writer);
                }
                data = stream.ToArray();
            }

            foreach (var client in ClientsToSync)
                Send(client, new VMNetMessage(VMNetMessageType.BroadcastTick, data));

            ClientsToSync.Clear();

            watch.Stop();
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

            lock (QueuedCmds) {
                //verify the queued commands. Remove ones which fail (or defer til later)
                for (int i=0; i<QueuedCmds.Count; i++)
                {
                    var caller = vm.GetAvatarByPersist(QueuedCmds[i].Command.ActorUID);
                    if (!QueuedCmds[i].Command.Verify(vm, caller)) QueuedCmds.RemoveAt(i--);
                }

                var tick = new VMNetTick();
                tick.Commands = new List<VMNetCommand>(QueuedCmds);
                tick.TickID = TickID++;
                tick.RandomSeed = vm.Context.RandomSeed;
                QueuedCmds.Clear();
                InternalTick(vm, tick);
                
                TickBuffer.Add(tick);

                lock (ClientsToSyncLater)
                {
                    if (LastResyncCooldown > 0)
                    {
                        LastResyncCooldown--;
                    }
                    else if (ClientsToSyncLater.Count > 0)
                    {
                        lock (ClientsToSync)
                        {
                            foreach (var client in ClientsToSyncLater)
                            {
                                ClientsToSync.Add(client);
                            }
                            ClientsToSyncLater.Clear();
                        }
                        LastResyncCooldown = CLIENT_RESYNC_COOLDOWN;
                    }
                }
                if (TickBuffer.Count >= TICKS_PER_PACKET)
                {
                    lock (ClientsToSync)
                    {
                        SendTickBuffer();
                        SendState(vm);
                    }
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

            Broadcast(new VMNetMessage(VMNetMessageType.BroadcastTick, data), ClientsToSync);
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

                    var packets = client.Value.GetMessages();
                    while (packets.Count > 0)
                    {
                        HandleMessage(client.Value, packets.Dequeue());
                    }
                }
                foreach (var client in ClientsToDC)
                {
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
            catch (Exception)
            {
                //corrupt commands are currently a death sentence for the client. nothing should be corrupt over TCP except in rare cases.
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
                lock (ClientsToSyncLater)
                {
                    //todo: throttle
                    ProblemTick = ((VMRequestResyncCmd)cmd.Command).TickID;
                    ClientsToSyncLater.Add(client);
                }
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
                        vm.SignalChatEvent(new VMChatEvent(0, VMChatEventType.Generic, "Found and banned " + name + ", with IP " + client.RemoteIP + "."));
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
