﻿using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetAsyncPriceCmd : VMNetCommandBodyAbstract
    {
        public uint ObjectPID;
        public int NewPrice;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null || caller.AvatarState.Permissions < VMTSOAvatarPermissions.Roommate) return false;
            VMEntity obj = vm.GetObjectByPersist(ObjectPID);
            //object must not be in use to set it for sale (will be disabled).
            if (obj == null || (obj is VMAvatar) || (NewPrice > 0 && obj.IsUserMovable(vm.Context, true) != VMPlacementError.Success)) return false;
            if ((((VMGameObject)obj).Disabled & VMGameObjectDisableFlags.TransactionIncomplete) > 0) return false; //can't change price mid trasaction...
            //must own the object to set it for sale
            if (obj.PersistID == 0 || ((VMTSOObjectState)obj.TSOState).OwnerID != caller.PersistID) return false;

            if (NewPrice >= 0) {
                //get catalog item for the object
                var item = Content.Content.Get().WorldCatalog.GetItemByGUID((obj.MasterDefinition ?? obj.Object.OBJ).GUID);
                if (item != null && item.Value.DisableLevel > 1) return false;
                foreach (var o in obj.MultitileGroup.Objects) ((VMGameObject)o).Disabled |= VMGameObjectDisableFlags.ForSale;
                obj.MultitileGroup.SalePrice = NewPrice;
            }
            else
            {
                foreach (var o in obj.MultitileGroup.Objects) ((VMGameObject)o).Disabled &= ~VMGameObjectDisableFlags.ForSale;
                obj.MultitileGroup.SalePrice = -1;
            }
            vm.Context.RefreshLighting(vm.Context.GetObjectRoom(obj), true, new HashSet<ushort>());
            return true;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectPID = reader.ReadUInt32();
            NewPrice = reader.ReadInt32();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectPID);
            writer.Write(NewPrice);
        }
    }
}
