using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.utils;
using TSO.Simantics.engine.scopes;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.model;
using TSO.Content;

namespace TSO.Simantics.engine.primitives
{

    public class VMFindBestObjectForFunction : VMPrimitiveHandler
    {
        public static uint[] FunctionToEntryPoint = {
            18, //prepare food
            19, //cook food
            20, //flat surface
            21, //dispose
            22, //eat
            23, //pick up from slot
            24, //wash dish
            25, //eating surface
            26, //sit
            27, //stand
            14, //serving surface
            28, //clean
            16, //gardening
            17, //wash hands
            29 //repair
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMFindBestObjectForFunctionOperand>();

            var entities = context.VM.Entities;
            var entry = VMFindBestObjectForFunction.FunctionToEntryPoint[operand.Function];
            for (int i=0; i<entities.Count; i++) {
                var ent = entities[i];
                if (ent.ObjectData[(int)VMStackObjectVariable.LockoutCount] > 0) continue; //this object is not important!!!
                if (ent.EntryPoints[entry].ActionFunction != 0) {
                    bool Execute;
                    if (ent.EntryPoints[entry].ConditionFunction != 0) {

                        var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entry].ConditionFunction, context.VM.Context);

                        var test = VMThread.EvaluateCheck(context.VM.Context, context.Caller, new VMQueuedAction(){
                            Callee = ent,
                            CodeOwner = Behavior.owner,
                            StackObject = ent,
                            Routine = context.VM.Assemble(Behavior.bhav),
                        });
                        
                        Execute = (test == VMPrimitiveExitCode.RETURN_TRUE);

                    } else {
                        Execute = true;
                    }

                    if (Execute)
                    {
                        //we can run this, it's suitable, yes I'LL TAKE IT
                        context.StackObject = ent;
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    //if not just keep searching
                }
            }

            return VMPrimitiveExitCode.GOTO_FALSE; //couldn't find an object! :'(
        }

    }

    public class VMFindBestObjectForFunctionOperand : VMPrimitiveOperand
    {
        public ushort Function;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Function = io.ReadUInt16();
            }
        }
        #endregion
    }
}
