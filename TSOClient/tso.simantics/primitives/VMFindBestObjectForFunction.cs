/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Engine.Scopes;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.Content;
using FSO.LotView.Model;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
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

        public static VMStackObjectVariable[] ScoreVar =
        {
            VMStackObjectVariable.PrepValue,
            VMStackObjectVariable.CookValue,
            VMStackObjectVariable.SurfaceValue,
            VMStackObjectVariable.DisposeValue,
            VMStackObjectVariable.Invalid, //hunger  value?
            VMStackObjectVariable.Invalid,
            VMStackObjectVariable.WashDishValue,
            VMStackObjectVariable.EatingSurfaceValue,
            VMStackObjectVariable.Invalid, //sit, may score using comfort value?
            VMStackObjectVariable.Invalid,
            VMStackObjectVariable.ServingSurfaceValue,
            VMStackObjectVariable.DirtyLevel,
            VMStackObjectVariable.GardeningValue,
            VMStackObjectVariable.WashHandsValue,
            VMStackObjectVariable.RepairState
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMFindBestObjectForFunctionOperand)args;

            var entities = context.VM.Entities;

            int bestScore = int.MinValue;
            VMEntity bestObj = null;

            var entry = VMFindBestObjectForFunction.FunctionToEntryPoint[operand.Function];
            for (int i=0; i<entities.Count; i++) {
                var ent = entities[i];
                if (ent.ObjectData[(int)VMStackObjectVariable.LockoutCount] > 0
                    || (ent is VMGameObject && ((VMGameObject)ent).Disabled > 0)) continue; //this object is not important!!!
                if (ent.EntryPoints[entry].ActionFunction != 0) {
                    bool Execute;
                    if (ent.EntryPoints[entry].ConditionFunction != 0) {

                        var Behavior = ent.GetBHAVWithOwner(ent.EntryPoints[entry].ConditionFunction, context.VM.Context);
                        if (Behavior != null)
                        {
                            var test = VMThread.EvaluateCheck(context.VM.Context, context.Caller, new VMStackFrame()
                            {
                                Caller = context.Caller,
                                Callee = ent,
                                CodeOwner = Behavior.owner,
                                StackObject = ent,
                                Routine = context.VM.Assemble(Behavior.bhav),
                                Args = new short[4]
                            });

                            Execute = (test == VMPrimitiveExitCode.RETURN_TRUE);
                        } else Execute = true;

                    } else {
                        Execute = true;
                    }

                    if (Execute)
                    {
                        if (ent.IsInUse(context.VM.Context, false)) continue; //this object is in use. this check is more expensive than check trees, so do it last.
                        //calculate the score for this object.
                        int score = 0;
                        if (ScoreVar[operand.Function] != VMStackObjectVariable.Invalid) {
                            var funcVar = ScoreVar[operand.Function];
                            score = ent.GetValue(funcVar);
                            if (funcVar == VMStackObjectVariable.DirtyLevel && score < 800) continue; //only clean "dirty" things.
                            else if (funcVar == VMStackObjectVariable.RepairState && score < 600) continue; //only fix "broken" things.
                        }

                        LotTilePos posDiff = ent.Position - context.Caller.Position;
                        score -= (int)Math.Sqrt(posDiff.x*posDiff.x+posDiff.y*posDiff.y+(posDiff.Level*posDiff.Level*900*256))/3;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestObj = ent;
                        }
                    }
                }
            }

            if (bestObj != null)
            {
                context.StackObject = bestObj;
                return VMPrimitiveExitCode.GOTO_TRUE;
            } else return VMPrimitiveExitCode.GOTO_FALSE; //couldn't find an object! :'(
        }

    }

    public class VMFindBestObjectForFunctionOperand : VMPrimitiveOperand
    {
        public ushort Function { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                Function = io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Function);
            }
        }
        #endregion
    }
}
