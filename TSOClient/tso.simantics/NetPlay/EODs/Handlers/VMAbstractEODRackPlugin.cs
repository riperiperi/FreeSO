using FSO.Common.Serialization;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMAbstractEODRackPlugin : VMEODHandler
    {
        protected EODLobby<VMEODPaperChaseSlot> Lobby;
        protected RackType RackType;


        public VMAbstractEODRackPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODPaperChaseSlot>(server, 1)
                    .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
            server.CanBeActionCancelled = true;
        }

        protected virtual void GetOutfits(VM vm, Callback<VMGLOutfit[]> callback)
        {
            vm.GlobalLink.GetOutfits(vm, VMGLOutfitOwner.OBJECT, Server.Object.PersistID, x =>
            {
                callback(x);
            });
        }

        protected void GetOutfit(VM vm, uint outfitPID, Callback<VMGLOutfit> callback)
        {
            GetOutfits(vm, x =>
            {
                callback(x.FirstOrDefault(y => y.outfit_id == outfitPID));
            });
        }

        /// <summary>
        /// Send the list of items stocked to the eod plugin
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="updateNumOutfits"></param>
        protected void BroadcastOutfits(VM vm, bool updateNumOutfits)
        {
            GetOutfits(vm, x =>
            {
                var packet = new VMEODRackStockResponse()
                {
                    Outfits = x
                };
                Lobby.Broadcast("set_outfits", packet);

                if (updateNumOutfits)
                {
                    var controller = Controller;
                    if (controller != null)
                    {
                        controller.SendOBJEvent(new VMEODEvent((short)VMEODRackEvent.SetOutfitCount, (short)x.Length));
                    }
                }
            });
        }


        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                var rackType = param[0];
                if (!Lobby.Join(client, 0)){
                    return;
                }
                RackType = (RackType)rackType;
                client.Send("rack_show", ((short)RackType).ToString());
                BroadcastOutfits(client.vm, false);
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
        }

        public VMEODClient Controller
        {
            get
            {
                return Lobby.Players[0];
            }
        }

        protected VMPersonSuits GetSuitType(RackType type)
        {
            switch (type)
            {
                case RackType.Formalwear:
                case RackType.Daywear:
                    return VMPersonSuits.DefaultDaywear;
                case RackType.Sleepwear:
                    return VMPersonSuits.DefaultSleepwear;
                case RackType.Swimwear:
                    return VMPersonSuits.DefaultSwimwear;
                case RackType.Decor_Head:
                    return VMPersonSuits.DecorationHead;
                case RackType.Decor_Back:
                    return VMPersonSuits.DecorationBack;
                case RackType.Decor_Shoe:
                    return VMPersonSuits.DecorationShoes;
                case RackType.Decor_Tail:
                    return VMPersonSuits.DecorationTail;
                default:
                    throw new Exception("Illegal state");
            }
        }

        protected VMPersonSuits GetSuitSlot(bool tryOn)
        {
            if (tryOn)
            {
                switch (this.RackType)
                {
                    case RackType.Daywear:
                    case RackType.Formalwear:
                    case RackType.Sleepwear:
                    case RackType.Swimwear:
                        return VMPersonSuits.DynamicCostume;
                }
            }

            return GetSuitType(this.RackType);
        }
    }


    public enum VMEODRackEvent : short
    {
        TryOnOutfit = 1,
        PurchaseOutfit = 2,
        PutOnNow = 3,
        StockOutfit = 4,
        SetOutfitCount = 8,
        PutClothesBack = 10
    }


    public class VMEODRackStockResponse : IoBufferSerializable, IoBufferDeserializable
    {
        public VMGLOutfit[] Outfits;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var length = input.GetInt32();
            Outfits = new VMGLOutfit[length];
            for (var i = 0; i < length; i++)
            {
                Outfits[i] = new VMGLOutfit();
                Outfits[i].Deserialize(input, context);
            }
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(Outfits.Length);
            foreach (var outfit in Outfits)
            {
                outfit.Serialize(output, context);
            }
        }
    }
}
