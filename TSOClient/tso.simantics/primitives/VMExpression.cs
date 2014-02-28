using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;
using tso.simantics.engine.scopes;
using tso.simantics.engine.utils;

namespace tso.simantics.engine.primitives
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
                case VMExpressionOperator.MinMinAndGreaterThan:
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

        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMExpressionOperand>();

            var description = "expression: " + VMMemory.DescribeVariable(context, operand.LhsOwner, operand.LhsData);
            description += " ";
            description += OperatorToString(operand.Operator);
            description += " ";
            description += VMMemory.DescribeVariable(context, operand.RhsOwner, operand.RhsData);
            Trace(description);

            short rhsValue = 0;
            short lhsValue = 0;
            bool setResult = false;

            switch (operand.Operator){
                /** Modifiers **/
                case VMExpressionOperator.Assign:
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);
                    setResult = VMMemory.SetVariable(context, operand.LhsOwner, operand.LhsData, rhsValue);
                    if (setResult)
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                /** ++ and < **/
                case VMExpressionOperator.IncAndLessThan:
                    lhsValue = VMMemory.GetVariable(context, operand.LhsOwner, operand.LhsData);
                    lhsValue++;
                    VMMemory.SetVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);

                    if (lhsValue < rhsValue){
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                case VMExpressionOperator.SetFlag:
                    lhsValue = VMMemory.GetVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);
                    var bitval = 1 << (rhsValue - 1);
                    lhsValue |= (short)bitval;
                    if (VMMemory.SetVariable(context, operand.LhsOwner, operand.LhsData, lhsValue))
                    {
                        return VMPrimitiveExitCode.GOTO_TRUE;
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }

                case VMExpressionOperator.ClearFlag:
                    lhsValue = VMMemory.GetVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);
                    var clearBitval = ~(1 << (rhsValue - 1));
                    lhsValue &= (short)clearBitval;
                    if (VMMemory.SetVariable(context, operand.LhsOwner, operand.LhsData, lhsValue))
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
                    lhsValue = VMMemory.GetVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);
                    switch (operand.Operator)
                    {
                        case VMExpressionOperator.ModEquals:
                            lhsValue %= rhsValue;
                            break;
                        case VMExpressionOperator.PlusEquals:
                            lhsValue += rhsValue;
                            break;
                        case VMExpressionOperator.MinusEquals:
                            lhsValue -= rhsValue;
                            break;
                        case VMExpressionOperator.DivEquals:
                            lhsValue /= rhsValue;
                            break;
                        case VMExpressionOperator.MulEquals:
                            lhsValue *= rhsValue;
                            break;
                        case VMExpressionOperator.AndEquals:
                            lhsValue &= rhsValue;
                            break;
                    }
                    VMMemory.SetVariable(context, operand.LhsOwner, operand.LhsData, lhsValue);
                    return VMPrimitiveExitCode.GOTO_TRUE;

                /** == **/
                case VMExpressionOperator.Equals:
                case VMExpressionOperator.LessThan:
                case VMExpressionOperator.GreaterThan:
                case VMExpressionOperator.GreaterThanOrEqualTo:
                case VMExpressionOperator.NotEqualTo:
                case VMExpressionOperator.LessThanOrEqualTo:
                case VMExpressionOperator.IsFlagSet:
                    lhsValue = VMMemory.GetVariable(context, operand.LhsOwner, operand.LhsData);
                    rhsValue = VMMemory.GetVariable(context, operand.RhsOwner, operand.RhsData);

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
                            result = lhsValue >= rhsValue;
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
                    }else{
                        return VMPrimitiveExitCode.GOTO_FALSE;
                    }
                default:
                    throw new Exception("Unknown expression type");
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
        MinMinAndGreaterThan = 17,
        Push = 18,
        Pop = 19
    }
}
