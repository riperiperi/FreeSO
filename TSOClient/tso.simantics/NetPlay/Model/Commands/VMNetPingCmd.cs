using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// Sent by the client when the server has not given us any ticks in a while. Speeds up the process of detecting a disconnect.
    /// </summary>
    public class VMNetPingCmd : VMNetCommandBodyAbstract
    {
        public override bool Execute(VM vm, VMAvatar caller)
        {
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return false; //don't forward, just sent to make sure the connection was still alive.
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
        }

        #endregion
    }
}
