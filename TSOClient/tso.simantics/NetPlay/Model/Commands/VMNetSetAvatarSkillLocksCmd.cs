using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// Server-only command that updates a single avatar's SkillLocks pool size in the
    /// live VM. Used by the lot host when the portal API has raised the avatar's
    /// skilllock_bonus and broadcast a SetAvatarSkillLockLimit Gluon packet — this just
    /// brings the in-VM PersonData[70] in sync so the new pool size is usable in the
    /// current session without requiring the avatar to leave and rejoin.
    ///
    /// Refuses any submission that came from the network (Verify returns !FromNet) so a
    /// client can't forge a free pool expansion.
    /// </summary>
    public class VMNetSetAvatarSkillLocksCmd : VMNetCommandBodyAbstract
    {
        public uint PersistID;
        public short NewLimit;

        public override bool Execute(VM vm)
        {
            var avatar = vm.GetAvatarByPersist(PersistID);
            if (avatar == null) return false;
            avatar.SkillLocks = NewLimit;
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller) => !FromNet;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PersistID = reader.ReadUInt32();
            NewLimit = reader.ReadInt16();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(PersistID);
            writer.Write(NewLimit);
        }
    }
}