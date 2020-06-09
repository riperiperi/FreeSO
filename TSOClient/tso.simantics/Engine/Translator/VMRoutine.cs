/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics
{
    public class VMRoutine
    {
        public VMRoutine(){
        }

        public byte Type;
        public VMInstruction[] Instructions;
        public ushort Locals;
        public ushort Arguments;
        public ushort ID;

        /** Run time info **/
        public VMFunctionRTI Rti;

        public BHAV Chunk;
        public uint RuntimeVer;

        public virtual VMPrimitiveExitCode Execute(VMStackFrame frame, out VMInstruction instruction)
        {
            instruction = frame.GetCurrentInstruction();
            var opcode = instruction.Opcode;

            if (opcode >= 256)
            {
                frame.Thread.ExecuteSubRoutine(frame, opcode, (VMSubRoutineOperand)instruction.Operand);
                return VMPrimitiveExitCode.CONTINUE;
            }


            var primitive = VMContext.Primitives[opcode];
            if (primitive == null)
            {
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            VMPrimitiveHandler handler = primitive.GetHandler();
            return handler.Execute(frame, instruction.Operand);
        }
    }


    public class VMFunctionRTI
    {
        public string Name;
    }
}
