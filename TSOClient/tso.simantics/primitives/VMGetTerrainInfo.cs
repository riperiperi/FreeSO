using FSO.Files.Utils;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Primitives
{
    public class VMGetTerrainInfo : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMGetTerrainInfoOperand)args;
            var obj = (operand.UseMe)?context.Caller:context.StackObject;

            if (obj.Position == LotTilePos.OUT_OF_WORLD) return VMPrimitiveExitCode.GOTO_FALSE;

            if (operand.UseMe) { }
            for (int i=0; i<5; i++)
            {
                if (operand.Unknown[i] != 0) { }
            }
            

            //TODO: all.

            switch (operand.Mode)
            {
                case 0: //four altitudes around stack object
                    //3 is flat in pre-alpha. TODO when terrain is in progress.
                    context.Thread.TempRegisters[0] = 3;
                    context.Thread.TempRegisters[1] = 3;
                    context.Thread.TempRegisters[2] = 3;
                    context.Thread.TempRegisters[3] = 3;
                    break;
                case 1: //slope at stack object
                    //unverifiable since terrain tool is a little broken in pre-alpha
                    //most likely altitude difference. +x slope down towards positive x, -x slope down towards -x. 
                    context.Thread.TempRegisters[0] = 0;
                    context.Thread.TempRegisters[1] = 0;
                    break;
                case 2: //fixed coordinates of stack object

                    //VERIFIED
                    var pos = obj.Position;
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
        #endregion
    }
}
