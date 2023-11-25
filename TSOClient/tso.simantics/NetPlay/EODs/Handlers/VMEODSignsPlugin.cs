using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Linq;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODSignsPlugin : VMEODHandler
    {
        //temp 0 contains VMEODSignsMode
        //temp 1 contains max chars

        public VMEODSignsData Data;
        public VMEODSignsMode Mode;
        public int MaxLength;
        public bool SentMessage = false; //if false and we have a message, send it to the client.
        private bool DisableRead = false; //don't have read permission

        public VMEODSignsPlugin(VMEODServer server) : base(server)
        {
            //we have data. load from global server.
            server.vm.GlobalLink.LoadPluginPersist(server.vm, server.Object.PersistID, server.PluginID, (byte[] data) =>
            {
                lock (this)
                {
                    if (data == null)
                    {
                        Data = new VMEODSignsData();
                        Data.Flags = (ushort)(VMEODSignPermissionFlags.RoomieWrite | VMEODSignPermissionFlags.RoomieRead 
                        | VMEODSignPermissionFlags.FriendRead | VMEODSignPermissionFlags.VisitorRead);
                    }
                    else Data = new VMEODSignsData(data);
                }
            });

            BinaryHandlers["set_message"] = B_SetMessage;
            PlaintextHandlers["close"] = P_Close;
        }

        public void B_SetMessage(string evt, byte[] data, VMEODClient client)
        {
            lock (this)
            {
                VMEODSignsData newData;
                try
                {
                    newData = new VMEODSignsData(data);
                }
                catch (Exception) { return; }

                if (Mode == VMEODSignsMode.Read) return; //cannot change anything
                else if (Mode == VMEODSignsMode.Write) newData.Flags = Data.Flags; //cannot change permissions

                if (newData.Text.Length > 0) newData.Text = newData.Text.Substring(0, Math.Min(MaxLength, newData.Text.Length));
                Data = newData;

                Server.vm.GlobalLink.SavePluginPersist(Server.vm, Server.Object.PersistID, Server.PluginID, newData.Save());

                foreach (var cli in Server.Clients)
                    cli.SendOBJEvent(new VMEODEvent((short)VMEODSignsEvent.TurnOnWritingSign, (short)((Data.Text.Length > 0) ? 1 : 0)));
            }
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            Server.Shutdown();
        }

        public override void Tick()
        {
            lock (this) {
                if (Data != null && !SentMessage)
                {
                    //init mode. check client permissions...
                    var client = Server.Clients.FirstOrDefault();
                    if (client == null) return; //uh, what?

                    VMEODSignPermissionFlags avaFlags = 0;

                    if (Mode != VMEODSignsMode.OwnerWrite && Mode != VMEODSignsMode.OwnerPermissions)
                    {
                        if (client.Avatar.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate) avaFlags |= VMEODSignPermissionFlags.RoomieRead;
                        else avaFlags |= VMEODSignPermissionFlags.VisitorRead;

                        var inverseRead = (~(VMEODSignPermissionFlags)Data.Flags) & VMEODSignPermissionFlags.ReadFlags & avaFlags;
                        if (inverseRead > 0) {
                            DisableRead = true; Mode = VMEODSignsMode.Read;
                        }

                        avaFlags = (VMEODSignPermissionFlags)((short)avaFlags<<3);
                        var inverseWrite = (~(VMEODSignPermissionFlags)Data.Flags) & VMEODSignPermissionFlags.WriteFlags & avaFlags;
                        Mode = (inverseWrite > 0) ? VMEODSignsMode.Read : VMEODSignsMode.Write; 
                    }

                    if (DisableRead) Data.Text = "";

                    client.Send("signs_init", (short)Mode + "\n" + MaxLength);
                    client.Send("signs_show", Data.Save());
                    SentMessage = true;
                }
            }
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            Mode = (VMEODSignsMode)param[0];
            MaxLength = param[1];
        }
    }

    public enum VMEODSignsMode
    {
        Erase = 0,
        Write = 1,
        Read = 2,
        OwnerPermissions = 3,
        OwnerWrite = 4
    }

    [Flags]
    public enum VMEODSignPermissionFlags : ushort
    {
        RoomieRead = 1,
        FriendRead = 1<<1,
        VisitorRead = 1<<2,
        RoomieWrite = 1<<3,
        FriendWrite = 1<<4,
        VisitorWrite = 1<<5,

        ReadFlags = 7,
        WriteFlags = 56
    }

    public enum VMEODSignsEvent
    {
        TurnOnWritingSign = 1, //bool in temp 0 if writing 
        Connect = -2
    }
}
