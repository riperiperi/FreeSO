using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs
{
    /*
        just text dumping here to make it obvious how this should work

        When Invoke Plugin is called, the calling thread blocks in the format of an animation.
        The plugin returns EVENTS (short code, short[] dataForTemps) on the false branch.
        These events obviously queue up, multiple per frame and have priority over the final "close".
         - these MUST be synchronised across clients (using commands)! all other comms (UI) are done async

        Plugins can either run on an object, with connections (Thread, ObjectID) (joinable, objects share same plugin)
        ...or run just on their thread alone. (Thread)
        AvatarID specifies who sees the UI and "connects" to the plugin. This can be 0. (Dance Floor Controller connects to itself to get events)

        ID for non-joinable EODs should be (ThreadOwnerID). For joinable, the ID should be (ObjectID).
    */
    public class VMEODServer
    {
        public static Dictionary<uint, Type> IDToHandler = new Dictionary<uint, Type>()
        {
            { 0x2a6356a0, typeof(VMEODSignsPlugin) }
        };

        public List<VMEODClient> Clients;
        public VMEODHandler Handler;
        public VMEntity Object;
        public bool Joinable;
        public VM vm;
        public uint PluginID;

        public VMEODServer(uint UID, VMEntity obj, bool joinable, VM vm)
        {
            PluginID = UID;
            Clients = new List<VMEODClient>();
            Type handleType = null;
            if (!IDToHandler.TryGetValue(UID, out handleType))
            {
                handleType = typeof(VMEODStubPlugin);
            }
            Object = obj;
            Joinable = joinable;
            this.vm = vm;
            Handler = (VMEODHandler)Activator.CreateInstance(handleType, this);
        }

        public void Tick()
        {
            Handler.Tick();
        }

        public void Deliver(VMNetEODMessageCmd msg)
        {
            if (msg.Binary)
            {
                EODBinaryEventHandler handle = null; 
                if (Handler.BinaryHandlers.TryGetValue(msg.EventName, out handle))
                {
                    handle(msg.EventName, msg.BinData);
                }
            } else
            {
                EODPlaintextEventHandler handle = null;
                if (Handler.PlaintextHandlers.TryGetValue(msg.EventName, out handle))
                {
                    handle(msg.EventName, msg.TextData);
                }
            }
        }

        public void Connect(VMEODClient client)
        {
            client.SendOBJEvent(new VMEODEvent(-2)); //connect code
            Clients.Add(client);
            client.Send("eod_enter", "");
            Handler.OnConnection(client);
        }

        public void Shutdown()
        {
            var tempCli = new List<VMEODClient>(Clients);
            foreach (var cli in tempCli)
                Disconnect(cli);
        }

        public void Disconnect(VMEODClient client)
        {
            client.SendOBJEvent(new VMEODEvent(-3)); //disconnect code
            Clients.Remove(client);
            Handler.OnDisconnection(client);
            client.Send("eod_leave", "");
            vm.EODHost.UnregisterAvatar(client.Avatar); //avatar no longer using plugin
        }

        public void BroadcastObjectEvent(VMEODEvent evt)
        {
            foreach (var cli in Clients)
                cli.SendOBJEvent(evt);
        }
        
    }

    public class VMEODClient
    {
        public VM vm;
        public VMEntity Invoker; //quas, wex, exort!
        public VMAvatar Avatar;
        public uint ActivePID;

        public VMEODClient(VMEntity invoker, VMAvatar avatar, VM vm, uint PID)
        {
            Invoker = invoker; Avatar = avatar; this.vm = vm; ActivePID = PID;
        }

        public void SendOBJEvent(VMEODEvent evt) {
            vm.SendCommand(new VMNetEODEventCmd
            {
                ObjectID = Invoker.ObjectID,
                Event = evt
            });
        }

        public void Send(string evt, string body)
        {
            if (Avatar == null) return;
            var cmd = new VMNetEODMessageCmd
            {
                PluginID = ActivePID,
                ActorUID = Avatar.PersistID,
                Binary = false,
                EventName = evt,
                TextData = body,
                Verified = true
            };
            vm.ForwardCommand(cmd);
        }

        public void Send(string evt, byte[] body)
        {
            if (Avatar == null) return;
            var cmd = new VMNetEODMessageCmd
            {
                PluginID = ActivePID,
                ActorUID = Avatar.PersistID,
                Binary = true,
                EventName = evt,
                BinData = body,
                Verified = true
            };
            vm.ForwardCommand(cmd);
        }
    }
}
