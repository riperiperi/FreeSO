using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetUpdatePersistStateCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public uint PersistID;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (obj == null || (obj is VMAvatar)) return false;
            if (obj.PersistID > 0) vm.Context.ObjectQueries.RemoveMultitilePersist(vm, obj.PersistID); //in case persist is reassigned somehow
            foreach (var e in obj.MultitileGroup.Objects)
                e.PersistID = PersistID;
            vm.Context.ObjectQueries.RegisterMultitilePersist(obj.MultitileGroup, obj.PersistID);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectID);
            writer.Write(PersistID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectID = reader.ReadInt16();
            PersistID = reader.ReadUInt32();
        }

        #endregion
    }
}
