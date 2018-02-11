using FSO.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Utils
{
    public class EODLobby
    {
        public static int[] ParsePlayers(string msg)
        {
            return msg.Split('\n').Select(x => {
                int result = 0;
                int.TryParse(x, out result);
                return result;
            }).ToArray();
        }
    }

    public class EODLobby<T> : EODLobby
    {
        private VMEODServer Server;
        public VMEODClient[] Players { get; internal set; }
        private T[] SlotData;

        private string BroadcastPlayersOnChangeHandler;
        private string OnJoinSendHandler;
        private bool _DisconnectIfSlotTaken = false;

        public EODLobby(VMEODServer server, uint numSlots)
        {
            this.Server = server;
            this.Players = new VMEODClient[numSlots];
            this.SlotData = new T[numSlots];
            for(var i=0; i < numSlots; i++){
                SlotData[i] = Activator.CreateInstance<T>();
            }
        }
        

        public T GetSlotData(int slot){
            return SlotData[slot];
        }

        public T GetSlotData(VMEODClient client)
        {
            var slot = GetPlayerSlot(client);
            if(slot != -1)
            {
                return GetSlotData(slot);
            }
            return default(T);
        }

        public short GetPlayerSlot(VMEODClient client)
        {
            var slot = Array.IndexOf(Players, client);
            return (short)slot;
        }

        public EODLobby<T> OnFailedToJoinDisconnect()
        {
            _DisconnectIfSlotTaken = true;
            return this;
        }

        public EODLobby<T> OnJoinSend(string handlerName)
        {
            OnJoinSendHandler = handlerName;
            return this;
        }

        public EODLobby<T> BroadcastPlayersOnChange(string handlerName)
        {
            BroadcastPlayersOnChangeHandler = handlerName;
            return this;
        }

        public void Close(string evt, string text, VMEODClient client)
        {
            Server.Disconnect(client);
        }

        public bool IsFull()
        {
            for(var i=0; i < Players.Length; i++)
            {
                if(Players[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsEmpty()
        {
            for (var i = 0; i < Players.Length; i++)
            {
                if (Players[i] != null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool Join(VMEODClient client, short slot)
        {
            if(slot >= 0 && slot < Players.Length && Players[slot] == null)
            {
                Players[slot] = client;
                if (OnJoinSendHandler != null)
                {
                    client.Send(OnJoinSendHandler, "");
                }
                BroadcastPlayers();
                return true;
            }
            else
            {
                if (_DisconnectIfSlotTaken)
                {
                    Server.Disconnect(client);
                }
                return false;
            }
        }

        public void Leave(VMEODClient client)
        {
            var playerIndex = Array.IndexOf(Players, client);
            if (playerIndex != -1)
            {
                Players[playerIndex] = null;
                BroadcastPlayers();
            }
        }

        private void BroadcastPlayers()
        {
            if(BroadcastPlayersOnChangeHandler != null)
            {
                var msg = "";
                for (var i=0; i < Players.Length; i++){
                    var player = Players[i];
                    if (i != 0)
                    {
                        msg += "\n";
                    }
                    msg += ((player == null) ? 0 : player.Avatar.ObjectID);
                }

                Broadcast(BroadcastPlayersOnChangeHandler, msg);
            }
        }

        public void Broadcast(string evt, string body)
        {
            foreach (var player in Players)
            {
                if (player != null)
                {
                    player.Send(evt, body);
                }
            }
        }

        public void Broadcast(string evt, byte[] body)
        {
            foreach (var player in Players)
            {
                if (player != null)
                {
                    player.Send(evt, body);
                }
            }
        }

        public void Broadcast(string evt, IoBufferSerializable body)
        {
            var buffer = IoBufferUtils.SerializableToIoBuffer(body, null);
            Broadcast(evt, buffer.GetBytes());
        }
    }
}
