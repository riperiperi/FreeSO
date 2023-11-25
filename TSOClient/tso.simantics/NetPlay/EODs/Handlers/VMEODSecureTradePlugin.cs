using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.IO;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODSecureTradePlugin : VMEODHandler
    {
        private EODLobby<VMEODSecureTradePlayer> Lobby;
        private bool Locked;
        private bool Kill = false;
        private bool HadTwoPlayers = false;
        private int TicksToAcceptable = 0;

        public VMEODSecureTradePlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODSecureTradePlayer>(server, 2)
                .BroadcastPlayersOnChange("trade_players")
                .OnJoinSend("trade_show")
                .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
            PlaintextHandlers["trade_offer"] = TradeOffer; 
        }

        private void OnConnected(VMEODClient client)
        {
            Lobby.GetSlotData(client).PlayerPersist = client.Avatar.PersistID;
            if (Lobby.IsFull())
            {
                HadTwoPlayers = true;
                BroadcastTradeData(false);
            }
        }

        private void ResetTradeTime()
        {
            TicksToAcceptable = 5*30;
            Lobby.Broadcast("trade_time", "5");
        }

        public override void Tick()
        {
            base.Tick();
            if (TicksToAcceptable > 0)
            {
                TicksToAcceptable--;
                if (TicksToAcceptable % 30 == 0)
                    Lobby.Broadcast("trade_time", (TicksToAcceptable/30).ToString());
            }
            if (Kill || (HadTwoPlayers && !Lobby.IsFull())) Server.Shutdown();
        }

        public void TryCompleteTrade()
        {
            Locked = true;
            Lobby.Broadcast("trade_inprogress", "");
            Server.vm.GlobalLink.SecureTrade(Server.vm, Lobby.GetSlotData(0), Lobby.GetSlotData(1), Content.Content.Get().WorldCatalog.GetUntradableGUIDs(), (result) =>
            {
                switch (result)
                {
                    case VMEODSecureTradeError.SUCCESS:
                        Lobby.Broadcast("trade_message", "0|14");
                        break;
                    default:
                        Lobby.Broadcast("trade_message", "0|16");
                        break;
                }

                Kill = true;
            });
        }

        public void BroadcastTradeData(bool clearAccepted)
        {
            lock (this)
            {
                var one = Lobby.GetSlotData(0);
                var two = Lobby.GetSlotData(1);
                if (clearAccepted)
                {
                    one.Accepted = false;
                    two.Accepted = false;
                }

                Lobby.Players[0].Send("trade_me", one);
                Lobby.Players[0].Send("trade_other", two);
                Lobby.Players[1].Send("trade_me", two);
                Lobby.Players[1].Send("trade_other", one);
            }
        }

        public void TradeOffer(string evt, string data, VMEODClient client)
        {
            if (data.Length == 0 || !Lobby.IsFull() || Locked) return;
            var mySlot = Lobby.GetPlayerSlot(client);
            var other = Lobby.Players[mySlot ^ 1];

            var myData = Lobby.GetSlotData(mySlot);

            switch (data[0])
            {
                case 'i':
                    //inventory item
                    //get this inventory item and add it to the slot
                    var inv = data.Substring(1).Split('|');
                    if (inv.Length < 2) return;
                    uint itemID;
                    if (!uint.TryParse(inv[0], out itemID)) return;
                    int slotID;
                    if (!int.TryParse(inv[1], out slotID) || slotID > 4) return;

                    if (itemID == 0)
                    {
                        //clear a slot
                        lock (this)
                        {
                            ResetTradeTime();
                            myData.ObjectOffer[slotID] = null;
                            BroadcastTradeData(true);
                        }
                    }
                    else
                    {
                        client.vm.GlobalLink.RetrieveFromInventory(client.vm, itemID, client.Avatar.PersistID, false,
                            (info) =>
                        {
                            if (info.GUID != 0)
                            {
                                lock (this)
                                {
                                    ResetTradeTime();
                                    //if this item is already on the offer, do nothing.
                                    if (Array.FindIndex(myData.ObjectOffer, x => x != null && x.PID == itemID) > -1)
                                    {
                                        client.Send("trade_error", ((int)VMEODSecureTradeError.ALREADY_PRESENT).ToString());
                                        BroadcastTradeData(false);
                                        return;
                                    }
                                    var item = Content.Content.Get().WorldCatalog.GetItemByGUID(info.GUID);
                                    if (item != null && item.Value.DisableLevel > 1 && client.Avatar.AvatarState.Permissions < VMTSOAvatarPermissions.Admin)
                                    {
                                        client.Send("trade_error", ((int)VMEODSecureTradeError.UNTRADABLE_OBJECT).ToString());
                                        BroadcastTradeData(false);
                                        return;
                                    }
                                    myData.ObjectOffer[slotID] = new VMEODSecureTradeObject(info.GUID, itemID, info.Data);
                                    BroadcastTradeData(true);
                                }
                            }
                            else
                            {
                                client.Send("trade_error", ((int)VMEODSecureTradeError.MISSING_OBJECT).ToString());
                                BroadcastTradeData(false);
                            }
                        });
                    }

                    break;
                case 'm':
                    //money
                    //do we have the correct amount of money?
                    int amount;
                    if (!int.TryParse(data.Substring(1), out amount) || amount < 0) return;
                    if (amount != 0)
                    {
                        //check if the player does have this money. it will be checked again when the transaction occurs.
                        client.vm.GlobalLink.PerformTransaction(client.vm, true, client.Avatar.PersistID, other.Avatar.PersistID, amount,
                            (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                        {
                            if (success)
                            {
                                lock (this)
                                {
                                    ResetTradeTime();
                                    myData.MoneyOffer = amount;
                                    BroadcastTradeData(true);
                                }
                            }
                            else
                            {
                                client.Send("trade_error", ((int)VMEODSecureTradeError.MISSING_MONEY).ToString());
                                BroadcastTradeData(false);
                            }
                        });
                    }
                    break;
                case 'p':
                    //property
                    //first of all... do we actually own a property? what's its id?
                    bool withObjects = data[1] == 'o';

                    int pSlotID;
                    if (!int.TryParse(data.Substring(2), out pSlotID) || pSlotID < 0) return;

                    //if we're already offering a property and it's not in our slot, fail.
                    //will need to redo this after we find the property and count its objects
                    var index = -1;
                    lock (this)
                        index = Array.FindIndex(myData.ObjectOffer, x => x != null && x.LotID > 0);
                    if (index > 0 && index != pSlotID)
                    {
                        client.Send("trade_error", ((int)VMEODSecureTradeError.ALREADY_PRESENT).ToString());
                        return;
                    }

                    client.vm.GlobalLink.FindLotAndValue(client.vm, client.Avatar.PersistID, Content.Content.Get().WorldCatalog.GetUntradableGUIDs(),
                    (uint lotID, int objectCount, long objectValue, string lotName) =>
                    {
                        if (lotID != 0)
                        {
                            lock (this)
                            {
                                ResetTradeTime();
                                index = -1;
                                index = Array.FindIndex(myData.ObjectOffer, x => x != null && x.LotID > 0);
                                if (index > 0 && index != pSlotID)
                                {
                                    client.Send("trade_error", ((int)VMEODSecureTradeError.ALREADY_PRESENT).ToString());
                                    return;
                                }

                                var obj = new VMEODSecureTradeObject((uint)((withObjects)?2:1), 1, null);
                                obj.LotID = lotID;
                                obj.LotName = lotName;
                                if (withObjects)
                                {
                                    obj.ObjectCount = objectCount;
                                    obj.ObjectValue = objectValue;
                                }
                                myData.ObjectOffer[pSlotID] = obj;
                                BroadcastTradeData(true);
                            }
                        }
                        else
                        {
                            client.Send("trade_error", ((int)VMEODSecureTradeError.MISSING_MONEY).ToString());
                            BroadcastTradeData(false);
                        }
                    });

                    break;

                case 'a':
                    if (TicksToAcceptable > 0) return;
                    lock (this)
                    {
                        myData.Accepted = true;
                        if (Lobby.GetSlotData(mySlot ^ 1).Accepted)
                        {
                            TryCompleteTrade();
                        }
                        else
                        {
                            BroadcastTradeData(false);
                        }
                    }
                    break;
            }
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                if (Lobby.Join(client, -1))
                {
                    OnConnected(client);
                }
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
        }
    }

    public class VMEODSecureTradePlayer : VMSerializable
    {
        public uint PlayerPersist;
        public VMEODSecureTradeObject[] ObjectOffer = new VMEODSecureTradeObject[5];
        public int MoneyOffer;
        public bool Accepted;

        public VMEODSecureTradePlayer() { }

        public VMEODSecureTradePlayer(byte[] data)
        {
            using (var str = new BinaryReader(new MemoryStream(data)))
            {
                Deserialize(str);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            PlayerPersist = reader.ReadUInt32();
            for (int i = 0; i < 5; i++)
            {
                if (reader.ReadBoolean())
                {
                    ObjectOffer[i] = new VMEODSecureTradeObject();
                    ObjectOffer[i].Deserialize(reader);
                }
            }
            MoneyOffer = reader.ReadInt32();
            Accepted = reader.ReadBoolean();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(PlayerPersist);
            for (int i = 0; i < 5; i++)
            {
                writer.Write(ObjectOffer[i] != null);
                ObjectOffer[i]?.SerializeInto(writer);
            }
            writer.Write(MoneyOffer);
            writer.Write(Accepted);
        }
    }

    public class VMEODSecureTradeObject : VMSerializable
    {
        public uint GUID;
        public uint PID;
        public byte[] Data;
        public uint LotID;
        public int ObjectCount;
        public long ObjectValue;
        public string LotName;

        public VMEODSecureTradeObject() { }

        public VMEODSecureTradeObject(uint guid, uint pid, byte[] data)
        {
            GUID = guid;
            PID = pid;
            Data = data;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(GUID);
            writer.Write(PID);
            writer.Write(Data?.Length ?? 0);
            if (Data != null) writer.Write(Data);
            writer.Write(LotID);
            if (LotID != 0)
            {
                writer.Write(ObjectCount);
                writer.Write(ObjectValue);
                writer.Write(LotName);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            GUID = reader.ReadUInt32();
            PID = reader.ReadUInt32();
            var len = reader.ReadInt32();
            if (len != 0) Data = reader.ReadBytes(len);
            LotID = reader.ReadUInt32();
            if (LotID != 0)
            {
                ObjectCount = reader.ReadInt32();
                ObjectValue = reader.ReadInt64();
                LotName = reader.ReadString();
            }
        }
    }

    public enum VMEODSecureTradeError : int
    {
        SUCCESS = 0,
        MISSING_MONEY,
        MISSING_OBJECT,
        NO_SECOND_PARTY,
        ALREADY_PRESENT,
        WRONG_OWNER_LOT,
        MISSING_OBJECT_LOT,
        UNTRADABLE_OBJECT,
        CANNOT_TRADE_COMMUNITY_LOT,
        UNKNOWN
    }
}
