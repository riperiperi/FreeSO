﻿using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs
{
    public class VMEODHost
    {
        public List<VMEODServer> Servers;
        public Dictionary<short, VMEODServer> JoinableEODs; //indexed by ObjectID

        //
        public Dictionary<short, VMEODServer> InvokerToEOD; //find EOD for an Invoker
        public Dictionary<short, VMEODServer> AvatarToEOD;

        public VMEODHost()
        {
            JoinableEODs = new Dictionary<short, VMEODServer>();
            InvokerToEOD = new Dictionary<short, VMEODServer>();
            AvatarToEOD = new Dictionary<short, VMEODServer>();
            Servers = new List<VMEODServer>();
        }

        public void Tick()
        {
            var curServers = new List<VMEODServer>(Servers);
            foreach (var server in curServers)
            {
                if (server.Clients.Count == 0)
                {
                    if (server.Joinable) JoinableEODs.Remove(server.Object.ObjectID);
                    Servers.Remove(server);
                }
                else
                    server.Tick();
            }
        }

        public void Connect(uint UID, VMEntity invoker, VMEntity obj, VMAvatar avatar, bool joinable, VM vm)
        {
            if (InvokerToEOD.ContainsKey(invoker.ObjectID)) return; //uh, what?

            VMEODServer server = null;
            if (avatar != null && AvatarToEOD.ContainsKey(avatar.ObjectID))
            {
                //avatar already using an EOD... quickly abort this attempt with the stub EOD.
                joinable = false;
                UID = 0;
                avatar = null;
            }

            if (joinable)
            {
                if (!JoinableEODs.TryGetValue(obj.ObjectID, out server))
                {
                    server = new VMEODServer(UID, obj, joinable, vm);
                    JoinableEODs[obj.ObjectID] = server;
                    Servers.Add(server);
                }
            }
            else
            {
                server = new VMEODServer(UID, obj, joinable, vm);
                Servers.Add(server);
            }

            if (avatar != null) RegisterAvatar(avatar, server);
            RegisterInvoker(invoker, server);
            server.Connect(new VMEODClient(invoker, avatar, vm, UID));
        }

        public void Deliver(VMNetEODMessageCmd msg, VMAvatar avatar)
        {
            VMEODServer server = null;
            if (AvatarToEOD.TryGetValue(avatar.ObjectID, out server))
            {
                var avatarClient = server.Clients.FirstOrDefault(x => x.Avatar == avatar);
                if (avatarClient != null)
                {
                    server.Deliver(msg, avatarClient);
                }
            }
        }

        public void SimanticsDeliver(short evt, VMEntity invoker)
        {
            VMEODServer server = null;
            if (InvokerToEOD.TryGetValue(invoker.ObjectID, out server))
            {
                var invokerClient = server.Clients.FirstOrDefault(x => x.Invoker == invoker);
                if (invokerClient != null)
                {
                    server.SimanticsDeliver(evt, invokerClient);
                }
            }
        }

        public void ForceDisconnectObj(VMEntity invoker)
        {
            VMEODServer server = null;
            if (InvokerToEOD.TryGetValue(invoker.ObjectID, out server))
            {
                var invokerClient = server.Clients.FirstOrDefault(x => x.Invoker == invoker);
                if (invokerClient != null)
                {
                    server.Disconnect(invokerClient);
                }
            }
        }

        public void ForceDisconnect(VMAvatar avatar)
        {
            VMEODServer server = null;
            if (AvatarToEOD.TryGetValue(avatar.ObjectID, out server))
            {
                var avatarClient = server.Clients.FirstOrDefault(x => x.Avatar == avatar);
                if (avatarClient != null)
                {
                    server.Disconnect(avatarClient);
                }
            }
        }

        public void ActionCancelDisconnect(VMAvatar avatar)
        {
            VMEODServer server = null;
            if (AvatarToEOD.TryGetValue(avatar.ObjectID, out server))
            {
                if (!server.CanBeActionCancelled) return;
                var avatarClient = server.Clients.FirstOrDefault(x => x.Avatar == avatar);
                if (avatarClient != null)
                {
                    server.Disconnect(avatarClient);
                }
            }
        }

        public void RegisterAvatar(VMAvatar avatar, VMEODServer server)
        {
            if (avatar == null) return;
            AvatarToEOD.Add(avatar.ObjectID, server);
        }

        public void RegisterInvoker(VMEntity invoker, VMEODServer server)
        {
            if (invoker == null) return;
            InvokerToEOD.Add(invoker.ObjectID, server);
        }

        public void UnregisterAvatar(VMAvatar avatar)
        {
            if (avatar == null) return;
            AvatarToEOD.Remove(avatar.ObjectID);
        }

        public void UnregisterInvoker(VMEntity invoker)
        {
            if (invoker == null) return;
            InvokerToEOD.Remove(invoker.ObjectID);
        }

        public T GetFirstHandler<T>() where T : VMEODHandler
        {
            return (T)Servers.FirstOrDefault(x => x.Handler is T)?.Handler;
        }

        public IEnumerable<T> GetHandlers<T>() where T : VMEODHandler
        {
            return Servers.Where(x => x.Handler is T).Select(x => (T)x.Handler);
        }
    }
}
