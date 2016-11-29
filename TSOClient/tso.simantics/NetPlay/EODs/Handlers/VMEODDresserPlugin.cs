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
            PlaintextHandlers["dresser_set_default"] = SetDefault;
            PlaintextHandlers["dresser_delete_outfit"] = DeleteOutfit;
        }

        private void DeleteOutfit(string evt, string data, VMEODClient client)
        {
            uint outfitId = 0;
            if (!uint.TryParse(data, out outfitId))
            {
                return;
            }


            GetOutfits(client.vm, outfits =>
            {
                var outfit = outfits.FirstOrDefault(x => x.outfit_id == outfitId);
                if (outfit == null){
                    //You don't own this outfit!
                    return;
                }

                var isDecoration = VMPersonSuitsUtils.IsDecoration((VMPersonSuits)outfit.outfit_type);

                if (!isDecoration)
                {
                    var numInCategory = outfits.Count(x => x.outfit_type == outfit.outfit_type);
                    if (numInCategory <= 1)
                    {
                        //You must keep at least one item of clothing in clothes categories
                        return;
                    }

                    //If its the default, I need to change the default
                    ulong currentDefault = VMPersonSuitsUtils.GetValue(client.Avatar, (VMPersonSuits)outfit.outfit_type);

                    if (outfit.asset_id == currentDefault)
                    {
                        var firstOutfit = outfits.First(x => x.outfit_type == outfit.outfit_type && x.outfit_id != outfitId);
                        //Change the default outfit to the first one in the list for the category
                        client.vm.SendCommand(new VMNetSetOutfitCmd
                        {
                            UID = client.Avatar.PersistID,
                            Scope = (VMPersonSuits)outfit.outfit_type,
                            Outfit = firstOutfit.asset_id
                        });
                    }
                }

                client.vm.GlobalLink.DeleteOutfit(client.vm, outfit.outfit_id, VMGLOutfitOwner.AVATAR, client.Avatar.PersistID, success => {
                    BroadcastOutfits(client.vm, false);
                });
            });
        }

        private void SetDefault(string evt, string data, VMEODClient client)
        {
            var split = data.Split(',');
            short categoryId = 0;
            uint outfitId = 0;

            if(!short.TryParse(split[0], out categoryId) ||
                !uint.TryParse(split[1], out outfitId)){
                return;
            }

            if (!VMPersonSuitsUtils.IsDefaultSuit((VMPersonSuits)categoryId)){
                //Can only set defaults on default suit types
                return;
            }

            var category = (VMPersonSuits)categoryId;
            StoreDefaultSuit(client, category, outfitId);
        }

        private void StoreDefaultSuit(VMEODClient client, VMPersonSuits category, uint outfitId)
        {
            GetOutfit(client.vm, outfitId, outfit => {
                if (outfit == null){
                    //You don't own this outfit!
                    return;
                }

                if(outfit.outfit_type != (byte)category){
                    //Wrong category, no swimming in PJs!
                    return;
                }

                //Set the default in the vm, this will be persisted on exit avatar persist
                client.vm.SendCommand(new VMNetSetOutfitCmd {
                    UID = client.Avatar.PersistID,
                    Scope = category,
                    Outfit = outfit.asset_id
                });

                //Have the UI update the default
                client.Send("dresser_refresh_default", "");
            });
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
