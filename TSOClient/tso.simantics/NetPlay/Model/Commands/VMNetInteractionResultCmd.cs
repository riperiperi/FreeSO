using System.IO;
using System.Linq;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetInteractionResultCmd : VMNetCommandBodyAbstract
    {
        public ushort ActionUID;
        public bool Accepted;
        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;
            var interaction = caller.Thread.Queue.FirstOrDefault(x => x.UID == ActionUID);
            if (interaction != null)
            {
                interaction.InteractionResult = (sbyte)(Accepted ? 2 : 1);
            }
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ActionUID);
            writer.Write(Accepted);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ActionUID = reader.ReadUInt16();
            Accepted = reader.ReadBoolean();
        }

        #endregion
    }
}
