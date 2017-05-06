using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODTrunkPlugin : VMEODHandler
    {
        Collection TrunkOutfits { get; set; }

        public VMEODTrunkPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["trunk_wear_costume"] = WearCostumeHandler;
            PlaintextHandlers["trunk_close_UI"] = TrunkUIClosedHandler;
            Server.CanBeActionCancelled = true;
        }

        public override void OnConnection(VMEODClient client)
        {
            base.OnConnection(client);
            var args = client.Invoker.Thread.TempRegisters;

            // check if the passed params match a trunk
            bool isValidTrunkParam = Enum.IsDefined(typeof(VMEODTrunkPluginCollections), args[0]);

            if (!isValidTrunkParam)
                Server.Disconnect(client);

            var typeString = Enum.GetName(typeof(VMEODTrunkPluginCollections), args[0]);

            // avatar gender affects collection & path
            var avatar = (VMAvatar)client.Avatar;
            bool isMale = (avatar.GetPersonData(VMPersonDataVariable.Gender) == 0);

            string collectionPath = typeString + (isMale ? "_" : "_fe" ) + "male.col";

            // get the matching trunk collection
            var content = Content.Content.Get();
            TrunkOutfits = content.AvatarCollections.Get(collectionPath);

            client.Send("trunk_fill_UI", collectionPath);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            base.OnDisconnection(client);
        }
        private void TrunkUIClosedHandler (string evt, string str, VMEODClient client)
        {
            Server.Disconnect(client);
        }
        private void WearCostumeHandler(string evt, string costumeID, VMEODClient client)
        {
            // make sure the requested item is valid and is found in the collection
            ulong parsedID;
            ulong assetID;
            var valid = ulong.TryParse(costumeID, out parsedID);
            if (valid)
                assetID = FindInCollection(parsedID);
            else
                assetID = 0;

            // send event to assign costume to avatar's dynamic costume and disconnect client
            if (assetID != 0)
            {
                client.vm.SendCommand(new VMNetSetOutfitCmd
                {
                    UID = client.Avatar.PersistID,
                    Scope = VMPersonSuits.DynamicCostume,
                    Outfit = assetID,
                });
                // now get out
                client.SendOBJEvent(new VMEODEvent((short)VMEODTrunkPluginEvents.Change));
                Server.Disconnect(client);
            }
        }
        private ulong FindInCollection(ulong outfitID)
        {
            foreach (var outfit in TrunkOutfits)
            {
                var purchasable = Content.Content.Get().AvatarPurchasables.Get(outfit.PurchasableOutfitId);
                if (purchasable.OutfitID == outfitID)
                    return purchasable.OutfitID;
            }
            return 0;
        }
    }
    [Flags]
    public enum VMEODTrunkPluginCollections: short
    {
        wedding = 0,
        scifi = 1,
        vegas = 2,
        vaudwest = 3,
        uniforms = 4,
        costumes = 5,
        oldworld = 6,
        sports = 7,
        toga = 8
    }
    public enum VMEODTrunkPluginEvents: short
    {
        Close = 0,
        Change = 1
    }
}
