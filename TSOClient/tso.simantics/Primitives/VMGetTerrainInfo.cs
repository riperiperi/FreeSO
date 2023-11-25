using FSO.Files.Utils;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using System;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMGetTerrainInfo : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGetTerrainInfoOperand)args;
            var obj = (operand.UseMe)?context.Caller:context.StackObject;

            if (obj.Position == LotTilePos.OUT_OF_WORLD) return VMPrimitiveExitCode.GOTO_FALSE;

            for (int i = 0; i < 5; i++)
            {
                if (operand.Unknown[i] != 0) { }
            }

            //TODO: all.
            var pos = obj.Position;
            var arch = context.VM.Context.Architecture;
            switch (operand.Mode)
            {
                case 0: //four altitudes around stack object
                        //3 is flat in pre-alpha. to convert back to tso/ts1 units we must divide through by 10.
                    context.Thread.TempRegisters[0] = (short)(arch.GetTerrainHeight(pos.TileX, pos.TileY)/10);
                    context.Thread.TempRegisters[1] = (short)(arch.GetTerrainHeight((short)Math.Min(arch.Width - 1, pos.TileX + 1), pos.TileY) / 10);
                    context.Thread.TempRegisters[2] = (short)(arch.GetTerrainHeight(pos.TileX, (short)Math.Min(arch.Height - 1, pos.TileY + 1)) / 10);
                    context.Thread.TempRegisters[3] = (short)(arch.GetTerrainHeight((short)Math.Min(arch.Width - 1, pos.TileX + 1), (short)Math.Min(arch.Height - 1, pos.TileY + 1)) / 10);
                    break;
                case 1: //slope at stack object
                    //unverifiable since terrain tool is a little broken in pre-alpha
                    //most likely altitude difference. +x slope down towards positive x, -x slope down towards -x. 
                    if (pos == LotTilePos.OUT_OF_WORLD) break;
                    var h1 = arch.GetTerrainHeight(pos.TileX, pos.TileY);
                    var h2 = arch.GetTerrainHeight((short)Math.Min(arch.Width-1, pos.TileX+1), pos.TileY);
                    var h3 = arch.GetTerrainHeight(pos.TileX, (short)Math.Min(arch.Height - 1, pos.TileY + 1));
                    var h4 = arch.GetTerrainHeight((short)Math.Min(arch.Width - 1, pos.TileX + 1), (short)Math.Min(arch.Height - 1, pos.TileY + 1));
                    context.Thread.TempRegisters[0] = (short)(((h1 - h2) + (h3 - h4)) / 20);
                    context.Thread.TempRegisters[1] = (short)(((h3 - h1) + (h4 - h2)) / 20);
                    break;
                case 2: //fixed coordinates of stack object

                    //VERIFIED
                    context.Thread.TempRegisters[0] = pos.TileX; //tile x
                    context.Thread.TempRegisters[1] = (short)(pos.x%16); //sub-tile x
                    context.Thread.TempRegisters[2] = pos.TileY; //tile y
                    context.Thread.TempRegisters[3] = (short)(pos.y%16); //sub-tile y
                    context.Thread.TempRegisters[4] = pos.Level; //level

                    break;
                case 3: //grass height under stack object
                    // 0-255, depending on terrain colour. (dead-alive)
                    context.Thread.TempRegisters[0] = 0;
                    //used for extra ball friction on (short) grass. 
                    break;
                case 4: //ceiling or roof exists above stack object. unused.
                    context.Thread.TempRegisters[0] = 0;
                    break;
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMGetTerrainInfoOperand : VMPrimitiveOperand
    {
        public byte Mode;
        public byte Unknown1;
        public byte Flags;
        public byte[] Unknown;

        public bool Interpolate
        {
            get
            {
                return (Flags & 1) > 0;
            }
        }
        public bool UseMe
        {
            get
            {
                return (Flags & 2) > 0;
            }
        }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Mode = io.ReadByte();
                Unknown1 = io.ReadByte();
                Flags = io.ReadByte();
                Unknown = io.ReadBytes(5);
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Mode);
                io.Write(Unknown1);
                io.Write(Flags);
            }
        }
        #endregion
    }
}
