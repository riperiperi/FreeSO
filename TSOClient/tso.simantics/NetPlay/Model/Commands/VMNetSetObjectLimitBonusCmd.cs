using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// Server-only command that pushes a new ObjectLimitBonus into the live VM. Used by
    /// the lot host when an external system (the portal API) has already debited the
    /// avatar's budget and persisted the new value to the DB — this just brings the
    /// running VM and connected clients in sync. Refuses any submission that came from
    /// the network (Verify returns !FromNet) so a client can't forge a free upgrade.
    ///
    /// Distinct from VMNetChangeObjectLimitCmd (the in-game purchase path), which runs
    /// owner-permission checks and a budget-debit transaction inside the VM.
    /// </summary>
    public class VMNetSetObjectLimitBonusCmd : VMNetCommandBodyAbstract
    {
        public int TargetBonus;

        public override bool Execute(VM vm)
        {
            vm.TSOState.ObjectLimitBonus = TargetBonus;
            VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller) => !FromNet;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetBonus = reader.ReadInt32();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TargetBonus);
        }
    }
}