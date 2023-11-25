using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds Behavior code in SimAntics.
    /// </summary>
    public class BHAV : IffChunk
    {
        public TREE RuntimeTree;
        public BHAVInstruction[] Instructions = new BHAVInstruction[0];
        public byte Type;
        public byte Args;
        public ushort Locals;
        public ushort Flags;

        public uint RuntimeVer;

        /// <summary>
        /// Reads a BHAV from a stream.
        /// </summary>
        /// <param name="iff">Iff instance.</param>
        /// <param name="stream">A Stream instance holding a BHAV chunk.</param>
        public override void Read(IffFile iff, System.IO.Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var version = io.ReadUInt16();
                uint count = 0;

                if (version == 0x8000)
                {
                    count = io.ReadUInt16();
                    io.Skip(8);
                }
                else if (version == 0x8001)
                {
                    count = io.ReadUInt16();
                    var unknown = io.ReadBytes(8);
                }
                else if (version == 0x8002)
                {
                    count = io.ReadUInt16();
                    this.Type = io.ReadByte();
                    this.Args = io.ReadByte();
                    this.Locals = io.ReadUInt16();
                    this.Flags = io.ReadUInt16();
                    io.Skip(2);
                }
                else if (version == 0x8003)
                {
                    this.Type = io.ReadByte();
                    this.Args = io.ReadByte();
                    this.Locals = io.ReadByte();
                    io.Skip(2);
                    this.Flags = io.ReadUInt16();
                    count = io.ReadUInt32();
                }

                Instructions = new BHAVInstruction[count];
                for (var i = 0; i < count; i++)
                {
                    var instruction = new BHAVInstruction();
                    instruction.Opcode = io.ReadUInt16();
                    instruction.TruePointer = io.ReadByte();
                    instruction.FalsePointer = io.ReadByte();
                    instruction.Operand = io.ReadBytes(8);
                    Instructions[i] = instruction;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                if (IffFile.TargetTS1)
                { //version 0x8002
                    io.WriteUInt16(0x8002);
                    io.WriteUInt16((ushort)Instructions.Length);
                    io.WriteByte(Type);
                    io.WriteByte(Args);
                    io.WriteUInt16(Locals);
                    io.WriteUInt16(Flags);
                    io.WriteBytes(new byte[] { 0, 0 });

                    foreach (var inst in Instructions)
                    {
                        io.WriteUInt16(inst.Opcode);
                        io.WriteByte(inst.TruePointer);
                        io.WriteByte(inst.FalsePointer);
                        io.WriteBytes(inst.Operand);
                    }
                }
                else
                {
                    io.WriteUInt16(0x8003);
                    io.WriteByte(Type);
                    io.WriteByte(Args);
                    io.WriteByte((byte)Locals);
                    io.WriteBytes(new byte[] { 0, 0 });
                    io.WriteUInt16(Flags);
                    io.WriteUInt32((ushort)Instructions.Length);

                    foreach (var inst in Instructions)
                    {
                        io.WriteUInt16(inst.Opcode);
                        io.WriteByte(inst.TruePointer);
                        io.WriteByte(inst.FalsePointer);
                        io.WriteBytes(inst.Operand);
                    }
                }
            }
            return true;
        }

    }

    public class BHAVInstruction 
    {
        public ushort Opcode;
        public byte TruePointer;
        public byte FalsePointer;
        public byte[] Operand;
        public bool Breakpoint; //only used at runtime
    }
}
