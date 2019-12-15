/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Engine
{
    public class VMTranslator
    {
        private static VMTranslator _INSTANCE;
        public static VMTranslator INSTANCE
        {
            get
            {
                if (_INSTANCE == null) _INSTANCE = new VMTranslator();
                return _INSTANCE;
            }
            set
            {
                _INSTANCE = value;
            }
        }

        public virtual VMRoutine Assemble(BHAV bhav, GameIffResource res){
            var routine = new VMRoutine();
            PopulateRoutineFields(bhav, routine);
            return routine;
        }

        protected void PopulateRoutineFields(BHAV bhav, VMRoutine routine)
        {
            routine.Locals = bhav.Locals;
            routine.Arguments = bhav.Args;
            routine.Type = bhav.Type;
            routine.ID = bhav.ChunkID;
            routine.Chunk = bhav;
            routine.Rti = new VMFunctionRTI
            {
                Name = bhav.ChunkLabel
            };

            VMInstruction[] instructions = new VMInstruction[bhav.Instructions.Length];
            for (var i = 0; i < bhav.Instructions.Length; i++)
            {
                var bhavInstruction = bhav.Instructions[i];
                var instruction = new VMInstruction();

                instruction.Index = (byte)i;
                instruction.Opcode = bhavInstruction.Opcode;
                instruction.Operand = null;
                instruction.FalsePointer = bhavInstruction.FalsePointer;
                instruction.TruePointer = bhavInstruction.TruePointer;
                instruction.Breakpoint = bhavInstruction.Breakpoint;
                instruction.Function = routine;

                /** Routine call **/
                if (instruction.Opcode >= 256)
                {
                    var operand = new VMSubRoutineOperand();
                    operand.Read(bhavInstruction.Operand);
                    instruction.Operand = operand;
                }
                else
                {
                    var primitive = VMContext.Primitives[instruction.Opcode];
                    if (primitive != null)
                    {
                        if (primitive.OperandModel != null)
                        {
                            VMPrimitiveOperand operand = (VMPrimitiveOperand)Activator.CreateInstance(primitive.OperandModel);
                            operand.Read(bhavInstruction.Operand);
                            instruction.Operand = operand;
                        }
                    }
                }
                instructions[i] = instruction;

            }
            routine.Instructions = instructions;
        }
    }
}
