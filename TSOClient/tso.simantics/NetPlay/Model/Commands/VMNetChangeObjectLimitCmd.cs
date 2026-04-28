using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetChangeObjectLimitCmd : VMNetCommandBodyAbstract
    {
        // Raw bonus value (multiple of ObjectLimitBonusStep, range 0..ObjectLimitBonusMax)
        public int TargetBonus;

        private bool Verified;

        public override bool Execute(VM vm)
        {
            vm.TSOState.ObjectLimitBonus = TargetBonus;
            vm.GlobalLink?.SetObjectLimitBonus(vm, TargetBonus);
            VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;

            if (caller == null || caller.AvatarState.Permissions < VMTSOAvatarPermissions.Owner)
                return false;

            TargetBonus = Math.Min(TargetBonus, VMBuildableAreaInfo.ObjectLimitBonusMax);
            TargetBonus = Math.Max(TargetBonus, 0);
            // must be a multiple of the step size
            TargetBonus = (TargetBonus / VMBuildableAreaInfo.ObjectLimitBonusStep) * VMBuildableAreaInfo.ObjectLimitBonusStep;

            var currentBonus = vm.TSOState.ObjectLimitBonus;
            if (TargetBonus <= currentBonus) return false; // can only upgrade, not downgrade

            var fromStep = currentBonus / VMBuildableAreaInfo.ObjectLimitBonusStep;
            var toStep = TargetBonus / VMBuildableAreaInfo.ObjectLimitBonusStep;
            var cost = VMBuildableAreaInfo.CalculateObjectLimitBonusCost(fromStep, toStep);

            vm.GlobalLink.PerformTransaction(vm, false, caller.PersistID, uint.MaxValue, cost,
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

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TargetBonus);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetBonus = reader.ReadInt32();
        }
    }
}