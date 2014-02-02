using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;

namespace tso.files.formats.iff.chunks
{
    public class BHAV : AbstractIffChunk
    {
        public BHAVInstruction[] Instructions;
        public byte Type;
        public byte Args;
        public ushort Locals;
        public ushort Flags;

        public override void Read(Iff iff, System.IO.Stream stream){
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var version = io.ReadUInt16();
                uint count = 0;

                if (version == 0x8000){
                    count = io.ReadUInt16();
                    io.Skip(8);
                }else if (version == 0x8001){
                    count = io.ReadUInt16();
                    var unknown = io.ReadBytes(8);
                }else if (version == 0x8002){
                    count = io.ReadUInt16();
                    this.Type = io.ReadByte();
                    this.Args = io.ReadByte();
                    this.Locals = io.ReadUInt16();
                    this.Flags = io.ReadUInt16();
                    io.Skip(2);
                }else if (version == 0x8003){
                    this.Type = io.ReadByte();
                    this.Args = io.ReadByte();
                    this.Locals = io.ReadByte();
                    io.Skip(2);
                    this.Flags = io.ReadUInt16();
                    count = io.ReadUInt32();
                }

                Instructions = new BHAVInstruction[count];
                for (var i = 0; i < count; i++){
                    var instruction = new BHAVInstruction();
                    instruction.Opcode = io.ReadUInt16();
                    instruction.TruePointer = io.ReadByte();
                    instruction.FalsePointer = io.ReadByte();
                    instruction.Operand = io.ReadBytes(8);
                    Instructions[i] = instruction;
                }
            }
        }
    }

    public class BHAVInstruction {
        public ushort Opcode;
        public byte TruePointer;
        public byte FalsePointer;
        public byte[] Operand;
    }
}
