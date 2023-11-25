using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Linq;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODPermissionDoorPlugin : VMEODHandler
    {
        //temp 0: mode (0 = edit, 1 = view, 2 = code input)
        //temp 1: max fee
        //temp 2: permission state
        //temp 3: door fee
        //temp 4: flags
        //temp 5: codeMS
        //temp 6: codeLS

        public uint? Code;

        public VMEODPermissionDoorMode Mode;
        public int MaxFee;
        public int PermissionState;
        public int DoorFee;
        public int Flags;
        //ignore code ms and ls

        public bool SentMessage = false; //if false and we have a message, send it to the client.

        public VMEODPermissionDoorPlugin(VMEODServer server) : base(server)
        {
            //we have data. load from global server.
            server.vm.GlobalLink.LoadPluginPersist(server.vm, server.Object.PersistID, server.PluginID, (byte[] data) =>
            {
                lock (this)
                {
                    Code = 0;
                    if (data != null)
                    {
                        uint result = 0;
                        uint.TryParse(System.Text.Encoding.UTF8.GetString(data), out result);
                        Code = result;
                    }
                }
            });

            PlaintextHandlers["set_code"] = P_SetCode;
            PlaintextHandlers["set_state"] = P_SetState;
            PlaintextHandlers["set_fee"] = P_SetFee;
            PlaintextHandlers["set_flags"] = P_SetFlags;
            PlaintextHandlers["try_code"] = P_TryCode;
            PlaintextHandlers["close"] = P_Close;
        }

        public void P_SetCode(string evt, string data, VMEODClient client)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;
            lock (this)
            {
                //verify that the code is at most a 4 digit number

                uint code = 0;
                if (!uint.TryParse(data, out code) || code > 999999999) return;

                var newData = System.Text.Encoding.UTF8.GetBytes(code.ToString());

                Server.vm.GlobalLink.SavePluginPersist(Server.vm, Server.Object.PersistID, Server.PluginID, newData);
                //only saved on server
            }
        }

        public void P_TryCode(string evt, string data, VMEODClient client)
        {
            if (Mode != VMEODPermissionDoorMode.CodeInput) return;
            lock (this)
            {
                if (!SentMessage) { Server.Shutdown(); return; }
                //verify that the code is at most a 4 digit number
                uint code = 0;
                if (!uint.TryParse(data, out code) || code > 999999999)
                { Server.Shutdown(); return; }

                foreach (var cli in Server.Clients)
                    cli.SendOBJEvent(new VMEODEvent((short)VMEODPermissionDoorEvent.ValidateResult, (short)((code == Code)?1:0)));

                Server.Shutdown();
            }
        }

        public void P_SetState(string evt, string data, VMEODClient client)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;

            ushort code;
            if (!ushort.TryParse(data, out code) || code > 2) return;

            foreach (var cli in Server.Clients)
                cli.SendOBJEvent(new VMEODEvent((short)VMEODPermissionDoorEvent.PermStateInTemp0, (short)code));
        }

        public void P_SetFee(string evt, string data, VMEODClient client)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;

            ushort code;
            if (!ushort.TryParse(data, out code) || code > MaxFee) return;

            foreach (var cli in Server.Clients)
                cli.SendOBJEvent(new VMEODEvent((short)VMEODPermissionDoorEvent.FeeInTemp0, (short)code));
        }

        public void P_SetFlags(string evt, string data, VMEODClient client)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;

            ushort flags;
            if (!ushort.TryParse(data, out flags)) return;

            flags &= (ushort)VMEODPermissionDoorFlags.All;

            foreach (var cli in Server.Clients)
                cli.SendOBJEvent(new VMEODEvent((short)VMEODPermissionDoorEvent.FlagsInTemp0, (short)flags));
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            if (Mode == VMEODPermissionDoorMode.Edit)
            {
                foreach (var cli in Server.Clients)
                    cli.SendOBJEvent(new VMEODEvent((short)VMEODPermissionDoorEvent.Save));
            }
            Server.Shutdown();
        }

        public override void Tick()
        {
            lock (this)
            {
                if (Code != null && !SentMessage)
                {
                    //start the plugin
                    var client = Server.Clients.FirstOrDefault();
                    if (client == null) return; //uh, what?

                    client.Send("door_init", (short)Mode + "\n" + MaxFee + "\n" + PermissionState + "\n" + DoorFee + "\n" + Flags);
                    if (Mode == VMEODPermissionDoorMode.Edit) client.Send("door_code", Code.ToString());
                    SentMessage = true;
                }
            }
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            Mode = (VMEODPermissionDoorMode)param[0];
            if (param.Length > 1) MaxFee = param[1];
            if (param.Length > 2) PermissionState = param[2];
            if (param.Length > 3) DoorFee = param[3];
            if (param.Length > 4) Flags = param[4];
        }
    }

    public enum VMEODPermissionDoorMode
    {
        Edit = 0,
        View = 1,
        CodeInput = 2,
    }

    [Flags]
    public enum VMEODPermissionDoorFlags : ushort
    {
        AllowRoommate = 1 << 2,
        AllowEmployee = 1 << 3,
        AllowFriend = 1 << 4,
        AllowVisitor = 1 << 5,
        MoneyExemptFriend = 1 << 6,
        MoneyExemptEmployee = 1 << 7,

        All = 4 | 8 | 16 | 32 | 64 | 128
    }

    public enum VMEODPermissionDoorEvent
    {
        Connect = -2,
        Save = 1,
        PermStateInTemp0 = 2,
        FeeInTemp0 = 3,
        FlagsInTemp0 = 4,
        CodeLSInTemp0 = 5,
        CodeMSInTemp0 = 6,

        ValidateResult = 7
    }
}
