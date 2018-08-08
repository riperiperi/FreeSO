﻿using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.IO;
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
            { 0x2a6356a0, typeof(VMEODSignsPlugin) },
            { 0x4a5be8ab, typeof(VMEODDanceFloorPlugin) },
            { 0xea47ae39, typeof(VMEODPizzaMakerPlugin) },
            { 0xca418206, typeof(VMEODPaperChasePlugin) },
            { 0x2b58020b, typeof(VMEODRackOwnerPlugin) },
            { 0xcb492685, typeof(VMEODRackPlugin) },
            { 0x8b300068, typeof(VMEODDresserPlugin) },
            { 0x0949E698, typeof(VMEODScoreboardPlugin) },
            { 0x0A69F29F, typeof(VMEODPermissionDoorPlugin) },
            { 0xCB2819CB, typeof(VMEODSlotsPlugin) },
            { 0xAA5E36DC, typeof(VMEODTrunkPlugin) },
            { 0x2D642D39, typeof(VMEODWarGamePlugin) },
            { 0xAA65FE9E, typeof(VMEODTimerPlugin) },
            { 0x895C1CEB, typeof(VMEODGameCompDrawACardPlugin) },
            { 0x8ADFC7A2, typeof(VMEODBandPlugin) },
            { 0x0B2A6B83, typeof(VMEODRoulettePlugin) },
            { 0x897f82f5, typeof(VMEODSecureTradePlugin) },
            { 0x2B2FC514, typeof(VMEODBlackjackPlugin) },

            { 0x6D113845, typeof(VMEODNCDanceFloorPlugin) },
            { 0xEC55D705, typeof(VMEODDancePlatformPlugin) },
            { 0x6C5C7555, typeof(VMEODDJStationPlugin) },
            { 0xCCC5BC43, typeof(VMEODNightclubControllerPlugin) },

            //freeso specific
            { 0x00001000, typeof(VMEODFNewspaperPlugin) },
            { 0x00001001, typeof(VMEODHoldEmCasinoPlugin) }
        };

        public List<VMEODClient> Clients;
        public VMEODHandler Handler;
        public VMEntity Object;
        public bool Joinable;
        public VM vm;
        public uint PluginID;
        public bool CanBeActionCancelled = false; //set true if the object does not deal with interaction cancelling itself

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

        public void Deliver(VMNetEODMessageCmd msg, VMEODClient client)
        {
            if (msg.Binary)
            {
                EODBinaryEventHandler handle = null; 
                if (Handler.BinaryHandlers.TryGetValue(msg.EventName, out handle))
                {
                    handle(msg.EventName, msg.BinData, client);
                }
            } else
            {
                EODPlaintextEventHandler handle = null;
                if (Handler.PlaintextHandlers.TryGetValue(msg.EventName, out handle))
                {
                    handle(msg.EventName, msg.TextData, client);
                }
            }
        }

        public void SimanticsDeliver(short evt, VMEODClient client)
        {
            EODSimanticsEventHandler handle = null;
            if (Handler.SimanticsHandlers.TryGetValue(evt, out handle))
            {
                handle(evt, client);
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
            client.SendOBJEvent(new VMEODEvent(-1)); //disconnect code
            Clients.Remove(client);
            Handler.OnDisconnection(client);
            client.Send("eod_leave", "");
            vm.EODHost.UnregisterAvatar(client.Avatar); //avatar no longer using plugin
            vm.EODHost.UnregisterInvoker(client.Invoker);
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
            if (Invoker.Thread.EODConnection == null) return; //shouldn't bother, we already closed it
            vm.SendCommand(new VMNetEODEventCmd
            {
                ObjectID = Invoker.ObjectID,
                Event = evt
            });
        }

        public void Send(string evt, string body)
        {
            if (Avatar == null || body == null) return;
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
            if (Avatar == null || body == null) return;
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

        public void Send(string evt, VMSerializable body)
        {
            using(var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                body.SerializeInto(writer);
                stream.Seek(0, SeekOrigin.Begin);
                Send(evt, stream.ToArray()); 
            }
        }
    }
}
