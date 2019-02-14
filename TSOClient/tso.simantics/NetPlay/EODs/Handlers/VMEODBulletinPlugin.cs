using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODBulletinPlugin : VMEODHandler
    {
        //simple wrapper around the bulletin dialog so it is accessible from the bulletin board object
        //also a simple example of how to handle 
        //temp 0 contains VMEODBulletinMode
        
        public VMEODSignsMode Mode;
        public bool Posted = false;
        private bool AnimInProgress = false;
        private short NextAnimChange = -1;

        public VMEODBulletinPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["bulletin_mode"] = P_Mode;
            PlaintextHandlers["bulletin_posted"] = P_Posted;
            PlaintextHandlers["close"] = P_Close;

            SimanticsHandlers[(short)VMEODBulletinEventIn.AnimChanged] = S_AnimChanged;
        }

        public void S_AnimChanged(short evt, VMEODClient client)
        {
            if (NextAnimChange == -1) AnimInProgress = false;
            else
            {
                client.SendOBJEvent(new Model.VMEODEvent(NextAnimChange));
                NextAnimChange = -1;
            }
        }

        public void P_Mode(string evt, string text, VMEODClient client)
        {
            //client has claimed they are in a different part of the bulletin board.
            //tell the object to change the animation

            short code;
            if (short.TryParse(text, out code))
            {
                if (code >= 0 && code < 3)
                {
                    if (AnimInProgress)
                    {
                        NextAnimChange = (short)(code + 2);
                    }
                    else
                    {
                        client.SendOBJEvent(new Model.VMEODEvent((short)(code + 2)));
                        AnimInProgress = true;
                    }
                }
            }
        }

        public void P_Posted(string evt, string text, VMEODClient client)
        {
            //client has claimed they have posted to the bulletin board.
            //get the updated state of the board (latest post id, post count in last 7 days) and tell the object.
            //note: only do this the first time the client requests, since the frequency limit
            //      should stop them from posting again for at least a day. 
            //      I doubt this plugin will be open for a day, and if it is i'm sure nobody will miss the graphical state not updating immediately.
            if (!Posted)
            {
                Posted = true;
                SendClientBulletinData(client);
            }
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            Server.Shutdown();
        }

        private void SendClientBulletinData(VMEODClient client)
        {
            //todo: get info from client
            Server.vm.GlobalLink.GetBulletinState(Server.vm, (lastID, activity) =>
            {
                client.SendOBJEvent(new Model.VMEODEvent((short)VMEODBulletinEvent.Info, new short[] { (short)lastID, (short)(lastID >> 16), (short)activity }));
            });
        }

        public override void Tick()
        {
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar == null)
            {
                //we're the controller!
                SendClientBulletinData(client);
            }
            else
            {
                var param = client.Invoker.Thread.TempRegisters;
                Mode = (VMEODSignsMode)param[0];
                client.Send("bulletin_show", (param[0]).ToString());
            }
        }
    }

    public enum VMEODBulletinMode
    {
        Read = 1,
        Write = 2,
    }

    public enum VMEODBulletinEvent
    {
        Info = 1, //(last post id -> 0/1, activity -> 2)
        ReadMode = 2,
        ReadSpecificMode = 3,
        WriteMode = 4,
        Connect = -2
    }

    public enum VMEODBulletinEventIn
    {
        AnimChanged = 1 //fired when an anim change completes.
    }
}
