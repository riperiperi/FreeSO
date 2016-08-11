using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSendToInventoryCmd : VMNetCommandBodyAbstract
    {
        public uint ObjectPID;
        public bool Verified;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //inventory move completed in verification, delete it now.
            //TODO: some modes for this command. LOCK object (so it cannot be used while we save it), DELETE LOCKED, UNLOCK object (something bad happened)

            var obj = vm.GetObjectByPersist(ObjectPID);
            if (obj != null)
            {
                //was this my sim's object? try add it to our local inventory representaton
                if (((VMTSOObjectState)obj.TSOState).OwnerID == vm.MyUID)
                {
                    vm.MyInventory.Add(new VMInventoryItem()
                    {
                        ObjectPID = ObjectPID,
                        GUID = (obj.MasterDefinition?.GUID) ?? obj.Object.OBJ.GUID,
                        Name = obj.MultitileGroup.Name,
                        Value = (uint)obj.MultitileGroup.Price,
                        Graphic = (ushort)obj.GetValue(VMStackObjectVariable.Graphic),
                        DynFlags1 = obj.DynamicSpriteFlags,
                        DynFlags2 = obj.DynamicSpriteFlags2,
                    });
                }
    
                obj.Delete(true, vm.Context);
            }


            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            if (ObjectPID == 0) return false;
            var targObj = vm.GetObjectByPersist(ObjectPID);
            if (caller == null || targObj == null || //caller must be on lot, be a roommate.
                ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate
                || targObj.PersistID == 0 || targObj is VMAvatar)
                return false;
            //todo: immediately lock this object
            vm.GlobalLink.MoveToInventory(vm, targObj.MultitileGroup, (bool success) =>
            {
                if (success)
                {
                    Verified = true;
                    vm.ForwardCommand(this);
                }
            });
            return false;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectPID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectPID = reader.ReadUInt32();
        }

        #endregion
    }
}
