namespace FSO.SimAntics.Engine
{
    public interface VMPrimitiveOperand
    {
        void Read(byte[] bytes);
        void Write(byte[] bytes);
    }
}
