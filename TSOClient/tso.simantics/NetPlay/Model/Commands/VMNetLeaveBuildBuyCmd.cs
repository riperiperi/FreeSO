namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetLeaveBuildBuyCmd : VMNetCommandBodyAbstract
    {
        public bool Build;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            vm.SignalGenericVMEvt(VMEventType.TSOUserLeaveBuildBuy, this);

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Build);

            base.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Build = reader.ReadBoolean();

            base.Deserialize(reader);
        }

        #endregion
    }
}
