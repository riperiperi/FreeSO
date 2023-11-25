using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetTimeoutNotifyCmd : VMNetCommandBodyAbstract
    {
        public int TimeRemaining; //in 60th seconds

        public override bool Execute(VM vm)
        {
            //sent from a server. we should be a VM.
            vm.SignalGenericVMEvt(VMEventType.TSOTimeout, TimeRemaining);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            //the fact the client executed this command should stop them from being afk.
            return false;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TimeRemaining);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TimeRemaining = reader.ReadInt32();
        }

        #endregion
    }
}
