using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Utils;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODDresserPlugin : VMAbstractEODRackPlugin
    {
        public VMEODDresserPlugin(VMEODServer server) : base(server)
        {
            Lobby.OnJoinSend("dresser_show");
            PlaintextHandlers["dresser_change_outfit"] = ChangeOutfit;
        }

        private void ChangeOutfit(string evt, string data, VMEODClient client)
        {
            uint outfitId = 0;
            if(!uint.TryParse(data, out outfitId)){
                return;
            }

            GetOutfit(client.vm, outfitId, outfit =>
            {
                if (outfit == null) { return; }
                var type = (VMPersonSuits)outfit.outfit_type;

                VMPersonSuits storageType = VMPersonSuits.DynamicDaywear;
                VMDresserOutfitTypes dresserOutfitType = VMDresserOutfitTypes.DynamicDaywear;

                switch (type)
                {
                    case VMPersonSuits.DefaultDaywear:
                        storageType = VMPersonSuits.DynamicDaywear;
                        dresserOutfitType = VMDresserOutfitTypes.DynamicDaywear;
                        break;
                    case VMPersonSuits.DefaultSleepwear:
                        storageType = VMPersonSuits.DynamicSleepwear;
                        dresserOutfitType = VMDresserOutfitTypes.DynamicSleepwear;
                        break;
                    case VMPersonSuits.DefaultSwimwear:
                        storageType = VMPersonSuits.DynamicSwimwear;
                        dresserOutfitType = VMDresserOutfitTypes.DynamicSwimwear;
                        break;
                    case VMPersonSuits.DecorationHead:
                        storageType = VMPersonSuits.DecorationHead;
                        dresserOutfitType = VMDresserOutfitTypes.DecorationHead;
                        break;
                    case VMPersonSuits.DecorationBack:
                        storageType = VMPersonSuits.DecorationBack;
                        dresserOutfitType = VMDresserOutfitTypes.DecorationBack;
                        break;
                    case VMPersonSuits.DecorationShoes:
                        storageType = VMPersonSuits.DecorationShoes;
                        dresserOutfitType = VMDresserOutfitTypes.DecorationShoes;
                        break;
                    case VMPersonSuits.DecorationTail:
                        storageType = VMPersonSuits.DecorationTail;
                        dresserOutfitType = VMDresserOutfitTypes.DecorationTail;
                        break;
                }

                client.vm.SendCommand(new VMNetSetOutfitCmd
                {
                    UID = client.Avatar.PersistID,
                    Scope = storageType,
                    Outfit = outfit.asset_id
                });
                client.SendOBJEvent(new VMEODEvent((short)VMEODDresserEvent.ChangeClothes, (short)dresserOutfitType));
            });
        }

        protected override void GetOutfits(VM vm, Callback<VMGLOutfit[]> callback)
        {
            vm.GlobalLink.GetOutfits(vm, VMGLOutfitOwner.AVATAR, Controller.Avatar.PersistID, x =>
            {
                callback(x);
            });
        }

        public override void OnDisconnection(VMEODClient client)
        {
            client.SendOBJEvent(new VMEODEvent((short)VMEODDresserEvent.CloseDresser));
            base.OnDisconnection(client);
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                Lobby.Join(client, 0);
                BroadcastOutfits(client.vm, false);
            }
        }
    }

    public enum VMEODDresserEvent : short
    {
        ChangeClothes = 1,
        CloseDresser = 2
    }


    public enum VMDresserOutfitTypes : byte
    {
        DefaultDaywear = 0,
        DefaultSleepwear = 5,
        DefaultSwimwear = 2,
        DynamicDaywear = 100,
        DynamicSleepwear = 101,
        DynamicSwimwear = 102,
        DecorationHead = 3,
        DecorationBack = 4,
        DecorationShoes = 5,
        DecorationTail = 6
    }
}
