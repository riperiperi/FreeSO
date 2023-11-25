namespace FSO.SimAntics.NetPlay.Model
{
    public class VMNetMessage
    {
        public VMNetMessageType Type;
        public byte[] Data;

        public VMNetMessage(VMNetMessageType type, byte[] data)
        {
            Type = type;
            Data = data;
        }
    }
}
