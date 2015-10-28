/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Model;
using System.IO;

namespace FSO.SimAntics.Engine.Primitives
{
    public class VMExpression : VMPrimitiveHandler
    {
        private string OperatorToString(VMExpressionOperator op)
        {
            switch (op){
                case VMExpressionOperator.AndEquals:
                    return "&=";
                case VMExpressionOperator.Assign:
                    return "=";
                case VMExpressionOperator.ClearFlag:
                    return "clearFlag";
                case VMExpressionOperator.DivEquals:
                    return "/=";
                case VMExpressionOperator.Equals:
                    return "==";
                case VMExpressionOperator.GreaterThan:
                    return ">";
                case VMExpressionOperator.GreaterThanOrEqualTo:
                    return ">=";
                case VMExpressionOperator.IncAndLessThan:
                    return "++ & <";
                case VMExpressionOperator.IsFlagSet:
                    return "flagSet";
                case VMExpressionOperator.LessThan:
                    return "<";
                case VMExpressionOperator.LessThanOrEqualTo:
                    return "<=";
                case VMExpressionOperator.DecAndGreaterThan:
                    return "-- & >";
                case VMExpressionOperator.MinusEquals:
                    return "-=";
                case VMExpressionOperator.ModEquals:
                    return "%=";
                case VMExpressionOperator.MulEquals:
                    return "*=";
                case VMExpressionOperator.NotEqualTo:
                    return "!=";
                case VMExpressionOperator.PlusEquals:
                    return "+=";
                case VMExpressionOperator.Pop:
                    return "pop";
                case VMExpressionOperator.Push:
                    return "push";
                case VMExpressionOperator.SetFlag:
                    return "setFlag";
            }
            return "unknown";
        }

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMExpressionOperand)args;

            int rhsValue = 0;
            int lhsValue = 0;
            bool setResult = false;

            switch (operand.Operator){
                /** Modifiers **/
                case VMExpressionOperator.Assign:
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);
                    setResult = VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, rhsValue);

                    
                    if (setResult)
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                /** ++ and < **/
                case VMExpressionOperator.IncAndLessThan:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    lhsValue++;
                    VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);

                    if (lhsValue < rhsValue){
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                /** -- and > **/
                case VMExpressionOperator.DecAndGreaterThan:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    lhsValue -= 1;
                    VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);

                    if (lhsValue > rhsValue)
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }
                    else
                    {
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                case VMExpressionOperator.SetFlag:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);
                    var bitval = 1 << (rhsValue - 1);
                    lhsValue |= bitval;
                    if (VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue))
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                case VMExpressionOperator.ClearFlag:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);
                    var clearBitval = ~(1 << (rhsValue - 1));
                    lhsValue &= (short)clearBitval;
                    if (VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue))
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                /** %= **/
                case VMExpressionOperator.PlusEquals:
                case VMExpressionOperator.ModEquals:
                case VMExpressionOperator.MinusEquals:
                case VMExpressionOperator.DivEquals:
                case VMExpressionOperator.MulEquals:
                case VMExpressionOperator.AndEquals:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);
                    switch (operand.Operator)
                    {
                        case VMExpressionOperator.ModEquals:
                            if (rhsValue == 0) break;
                            lhsValue = ((lhsValue % rhsValue + rhsValue) % rhsValue);
                            break;
                        case VMExpressionOperator.PlusEquals:
                            lhsValue += rhsValue;
                            break;
                        case VMExpressionOperator.MinusEquals:
                            lhsValue -= rhsValue;
                            break;
                        case VMExpressionOperator.DivEquals:
                            if (rhsValue == 0)
                            {
                                lhsValue = -1;
                                break;
                            }
                            lhsValue /= rhsValue;
                            break;
                        case VMExpressionOperator.MulEquals:
                            lhsValue *= rhsValue;
                            break;
                        case VMExpressionOperator.AndEquals:
                            lhsValue &= rhsValue;
                            break;
                    }
                    VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                /** == **/
                case VMExpressionOperator.Equals:
                case VMExpressionOperator.LessThan:
                case VMExpressionOperator.GreaterThan:
                case VMExpressionOperator.GreaterThanOrEqualTo:
                case VMExpressionOperator.NotEqualTo:
                case VMExpressionOperator.LessThanOrEqualTo:
                case VMExpressionOperator.IsFlagSet:
                    lhsValue = VMMemory.GetBigVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);

                    bool result = false;
                    switch (operand.Operator){
                        case VMExpressionOperator.Equals:
                            result = lhsValue == rhsValue;
                            break;
                        case VMExpressionOperator.LessThan:
                            result = lhsValue < rhsValue;
                            break;
                        case VMExpressionOperator.GreaterThan:
                            result = lhsValue > rhsValue;
                            break;
                        case VMExpressionOperator.GreaterThanOrEqualTo:
                            if (rhsValue == 1024 && operand.LhsData == (int)(VMStackObjectVariable.Room) && operand.LhsOwner == VMVariableScope.MyObject)
                                //HACK: rooms >= 1024 is "upstairs"... check only used in stairs. Hacked to work with >2 floors
                                result = context.Caller.Position.Level - (context.Callee.Position.Level - context.Callee.Object.OBJ.LevelOffset) > 0;
                            else result = lhsValue >= rhsValue;
                            break;
                        case VMExpressionOperator.NotEqualTo:
                            result = lhsValue != rhsValue;
                            break;
                        case VMExpressionOperator.LessThanOrEqualTo:
                            result = lhsValue <= rhsValue;
                            break;
                        case VMExpressionOperator.IsFlagSet:
                            result = ((lhsValue & (1<<(rhsValue-1))) > 0);
                            break;
                    }

                    if (result){
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    } else {
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                case VMExpressionOperator.Push:
                    var lhsList = VMMemory.GetList(context, operand.LhsOwner);
                    rhsValue = VMMemory.GetBigVariable(context, operand.RhsOwner, operand.RhsData);

                    switch (operand.LhsData)
                    {
                        case 0: //front
                            lhsList.AddFirst((short)rhsValue);
                            break;
                        case 1: //back
                            lhsList.AddLast((short)rhsValue);
                            break;
                        case 2:
                            throw new VMSimanticsException("Unknown list push destination: "+operand.LhsData, context);
                    }
                    return VMPrimitiveExitCode.GOTO_TRUE;

                case VMExpressionOperator.Pop:
                    var rhsList = VMMemory.GetList(context, operand.RhsOwner);
                    if (rhsList.Count == 0) return VMPrimitiveExitCode.GOTO_FALSE;

                    switch (operand.LhsData)
                    {
                        case 0: //front
                            lhsValue = rhsList.First.Value;
                            rhsList.RemoveFirst();
                            break;
                        case 1: //back
                            lhsValue = rhsList.Last.Value;
                            rhsList.RemoveLast();
                            break;
                        case 2:
                            throw new VMSimanticsException("Unknown list pop source: "+operand.LhsData, context);
                    }

                    VMMemory.SetBigVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                default:
                    throw new VMSimanticsException("Unknown expression type", context);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class VMExpressionOperand : VMPrimitiveOperand
    {
        public ushort LhsData;
        public ushort RhsData;
        public byte IsSigned;
        public VMExpressionOperator Operator;
        public VMVariableScope LhsOwner;
        public VMVariableScope RhsOwner;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                LhsData = io.ReadUInt16();
                RhsData = io.ReadUInt16();
                IsSigned = io.ReadByte();
                Operator = (VMExpressionOperator)io.ReadByte();
                LhsOwner = (VMVariableScope)io.ReadByte();
                RhsOwner = (VMVariableScope)io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(LhsData);
                io.Write(RhsData);
                io.Write(IsSigned);
                io.Write((byte)Operator);
                io.Write((byte)LhsOwner);
                io.Write((byte)RhsOwner);
            }
        }
        #endregion
    }

    public enum VMExpressionOperator {
        GreaterThan = 0,
        LessThan = 1,
        Equals = 2,
        PlusEquals = 3,
        MinusEquals = 4,
        Assign = 5,
        MulEquals = 6,
        DivEquals = 7,
        IsFlagSet = 8,
        SetFlag = 9,
        ClearFlag = 10,
        IncAndLessThan = 11,
        ModEquals = 12,
        AndEquals = 13,
        GreaterThanOrEqualTo = 14,
        LessThanOrEqualTo = 15,
        NotEqualTo = 16,
        DecAndGreaterThan = 17,
        Push = 18,
        Pop = 19
    }
}
