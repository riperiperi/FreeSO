using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using System.IO;

namespace FSO.SimAntics.Primitives
{

    /// <summary>
    /// There isnt actually a private call handler, This is part of the 
    /// </summary>
    public class VMSubRoutineOperand : VMPrimitiveOperand
    {
        public VMSubRoutineOperand()
        {
            Arguments = new short[4];
        }

        public VMSubRoutineOperand(short[] Args)
        {
            Arguments = Args;
        }

        public short[] Arguments;

        private void UpdateUseTemp0()
        {
            UseTemp0 = !(Arg1 == 0 && Arg2 == 0 && Arg3 == 0);
        }
        public short Arg0 { get { return Arguments[0]; } set { Arguments[0] = value; UpdateUseTemp0(); } }
        public short Arg1 { get { return Arguments[1]; } set { Arguments[1] = value; UpdateUseTemp0(); } }
        public short Arg2 { get { return Arguments[2]; } set { Arguments[2] = value; UpdateUseTemp0(); } }
        public short Arg3 { get { return Arguments[3]; } set { Arguments[3] = value; UpdateUseTemp0(); } }
        public bool UseTemp0;


        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Arguments = new short[4];
                Arguments[0] = io.ReadInt16();
                Arguments[1] = io.ReadInt16();
                Arguments[2] = io.ReadInt16();
                Arguments[3] = io.ReadInt16();
                UseTemp0 = !(Arg1 == 0 && Arg2 == 0 && Arg3 == 0);
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Arg0);
                io.Write(Arg1);
                io.Write(Arg2);
                io.Write(Arg3);
            }
        }
        #endregion
    }
}
