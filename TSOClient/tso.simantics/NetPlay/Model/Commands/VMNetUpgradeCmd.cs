using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// Upgrade an object. Can only do for objects we own, or donated objects.
    /// </summary>
    public class VMNetUpgradeCmd : VMNetCommandBodyAbstract
    {
        public bool Verified = false;
        public uint ObjectPID;
        public byte TargetUpgradeLevel;
        public int AddedValue;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            var pobj = vm.GetObjectByPersist(ObjectPID);
            if (pobj == null) return false;
            var isDonated = false;
            foreach (var obj in pobj.MultitileGroup.Objects)
            {
                var state = obj?.PlatformState as VMTSOObjectState;
                if (state != null)
                {
                    if (state.ObjectFlags.HasFlag(VMTSOObjectFlags.FSODonated)) isDonated = true;
                    state.UpgradeLevel = TargetUpgradeLevel;
                    state.Wear = 20 * 4;
                    state.QtrDaysSinceLastRepair = 0;
                    obj.UpdateTuning(vm);
                }
            }

            if (!isDonated) pobj.MultitileGroup.InitialPrice += AddedValue;

            if (vm.IsServer)
                vm.GlobalLink.UpdateObjectPersist(vm, pobj.MultitileGroup, (worked, objid) => { });
            vm.SignalGenericVMEvt(VMEventType.TSOUpgraded, ObjectPID);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            var obj = vm.GetObjectByPersist(ObjectPID);
            var currentLevel = (obj.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
            if (obj == null || obj is VMAvatar || TargetUpgradeLevel <= currentLevel) return false;
            var price = Content.Content.Get().Upgrades.GetUpgradePrice(
                obj.Object.Resource.Iff.Filename,
                (obj.MasterDefinition ?? obj.Object.OBJ).GUID,
                TargetUpgradeLevel,
                currentLevel,
                obj.MultitileGroup.Price);

            if (price == null) return false;

            AddedValue = Math.Max(price.Value - (obj.MultitileGroup.InitialPrice - obj.MultitileGroup.Price), 0);

            //perform the transaction. If it succeeds, requeue the command
            vm.GlobalLink.PerformTransaction(vm, false, caller?.PersistID ?? uint.MaxValue, uint.MaxValue, price.Value,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
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
            writer.Write(TargetUpgradeLevel);
            writer.Write(AddedValue);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectPID = reader.ReadUInt32();
            TargetUpgradeLevel = reader.ReadByte();
            AddedValue = reader.ReadInt32();
        }

        #endregion
    }
}
