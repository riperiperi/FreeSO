using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.JIT.Translation.CSharp.Engine;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSExpressionPrimitive : AbstractTranslationPrimitive
    {
        public override PrimitiveReturnType ReturnType
        {
            get
            {
                var operand = GetOperand<VMExpressionOperand>();
                switch (operand.Operator)
                {
                    case VMExpressionOperator.AndEquals:
                    case VMExpressionOperator.Assign:
                    case VMExpressionOperator.ClearFlag:
                    case VMExpressionOperator.DivEquals:
                    case VMExpressionOperator.MinusEquals:
                    case VMExpressionOperator.ModEquals:
                    case VMExpressionOperator.MulEquals:
                    case VMExpressionOperator.PlusEquals:
                    case VMExpressionOperator.Push:
                    case VMExpressionOperator.SetFlag:
                        return PrimitiveReturnType.NativeStatementTrue;

                    case VMExpressionOperator.Pop:
                        return (VM.GlobTS1) ? PrimitiveReturnType.NativeStatementTrue :
                            PrimitiveReturnType.NativeStatementTrueFalse;

                    case VMExpressionOperator.Equals:
                    case VMExpressionOperator.GreaterThan:
                    case VMExpressionOperator.GreaterThanOrEqualTo:
                    case VMExpressionOperator.IsFlagSet:
                    case VMExpressionOperator.LessThan:
                    case VMExpressionOperator.LessThanOrEqualTo:
                    case VMExpressionOperator.NotEqualTo:
                        return PrimitiveReturnType.NativeExpressionTrueFalse;

                    case VMExpressionOperator.DecAndGreaterThan:
                    case VMExpressionOperator.IncAndLessThan:
                        return (CSScopeMemory.ScopeMutable(operand.LhsOwner)) ?
                            PrimitiveReturnType.NativeExpressionTrueFalse :
                            PrimitiveReturnType.NativeStatementTrueFalse;
                }
                return PrimitiveReturnType.NativeStatementTrue; //return true if invalid expression
            }
        }

        public bool IsSwitchStatementViable()
        {
            var operand = GetOperand<VMExpressionOperand>();
            return operand.Operator == VMExpressionOperator.Equals && (
                operand.RhsOwner == VMVariableScope.Literal ||
                operand.RhsOwner == VMVariableScope.Tuning
                );
        }

        public bool IsSwitchStatementViable(VMVariableScope lastScope)
        {
            var operand = GetOperand<VMExpressionOperand>();
            return operand.LhsOwner == lastScope && operand.Operator == VMExpressionOperator.Equals && (
                operand.RhsOwner == VMVariableScope.Literal ||
                operand.RhsOwner == VMVariableScope.Tuning
                );
        }

        public CSExpressionPrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
        }

        public override List<string> CodeGen(TranslationContext context)
        {
            var csContext = (CSTranslationContext)context;
            var csClass = csContext.CurrentClass;
            var operand = GetOperand<VMExpressionOperand>();

            var big = CSScopeMemory.IsBig(operand.LhsOwner) || CSScopeMemory.IsBig(operand.RhsOwner);

            string basicOp = null;
            string basicExpressionOp = null;
            switch (operand.Operator)
            {
                case VMExpressionOperator.AndEquals:
                    basicOp = "&="; break;
                case VMExpressionOperator.Assign:
                    basicOp = "="; break;
                case VMExpressionOperator.DivEquals:
                    basicOp = "/="; break;
                case VMExpressionOperator.MinusEquals:
                    basicOp = "-="; break;
                case VMExpressionOperator.ModEquals:
                    basicOp = "%="; break;
                case VMExpressionOperator.MulEquals:
                    basicOp = "*="; break;
                case VMExpressionOperator.PlusEquals:
                    basicOp = "+="; break;
                case VMExpressionOperator.TS1OrEquals:
                    basicOp = "|="; break;
                case VMExpressionOperator.TS1XorEquals:
                    basicOp = "^="; break;

                case VMExpressionOperator.TS1AssignSqrtRHS:
                    return Line(CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "&=",
                        $"(short)Math.Sqrt({CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)})", big));

                case VMExpressionOperator.ClearFlag:
                case VMExpressionOperator.SetFlag:
                    string flag;
                    if (operand.RhsOwner == VMVariableScope.Literal)
                    {
                        int intFlag = (1 << (operand.RhsData - 1));
                        if (operand.Operator == VMExpressionOperator.ClearFlag) intFlag = ~intFlag;
                        if (big) flag = intFlag.ToString();
                        else flag = ((short)intFlag).ToString();
                    } 
                    else
                    {
                        if (operand.Operator == VMExpressionOperator.ClearFlag)
                            flag = $"~(1 << ({CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)} - 1))";
                        else
                            flag = $"(1 << ({CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)} - 1))";
                        if (!CSScopeMemory.IsBig(operand.LhsOwner))
                        {
                            flag = $"(short)({flag})";
                        }
                    }

                    if (operand.Operator == VMExpressionOperator.ClearFlag)
                    {
                        return Line(CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "&=", flag, big));
                    }
                    else
                    {
                        return Line(CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "|=", flag, big));
                    }

                case VMExpressionOperator.Equals:
                    basicExpressionOp = "=="; break;
                case VMExpressionOperator.GreaterThan:
                    basicExpressionOp = ">"; break;
                case VMExpressionOperator.GreaterThanOrEqualTo:
                    basicExpressionOp = ">="; break;
                case VMExpressionOperator.LessThan:
                    basicExpressionOp = "<"; break;
                case VMExpressionOperator.LessThanOrEqualTo:
                    basicExpressionOp = "<="; break;
                case VMExpressionOperator.NotEqualTo:
                    basicExpressionOp = "!="; break;

                case VMExpressionOperator.IsFlagSet:
                    var lExp = CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big);
                    var rExp = $"(1 << ({CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)} - 1))";
                    return Line($"({lExp} & {rExp}) > 0");

                case VMExpressionOperator.DecAndGreaterThan:
                    if (CSScopeMemory.ScopeMutable(operand.LhsOwner))
                    {
                        return Line($"--{CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big)} > {CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)}");
                    }
                    else
                    {
                        return new List<string>()
                        {
                            CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "-=", "1", big),
                            $"_bResult = {CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big)} > {CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)};"
                        };
                    }

                case VMExpressionOperator.IncAndLessThan:
                    if (CSScopeMemory.ScopeMutable(operand.LhsOwner))
                    {
                        return Line($"++{CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big)} < {CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)}");
                    }
                    else
                    {
                        return new List<string>()
                        {
                            CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "+=", "1", big),
                            $"_bResult = {CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big)} < {CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big)};"
                        };
                    }
            }

            if (VM.GlobTS1)
            {
                if (operand.Operator == VMExpressionOperator.Push)
                {
                    string list;
                    switch (operand.LhsOwner)
                    {
                        case VMVariableScope.MyList:
                            list = "context.Caller.MyList"; break;
                        case VMVariableScope.StackObjectList:
                            list = "context.StackObject.MyList"; break;
                        default:
                            throw new Exception($"Invalid scope for list operation push! LHS: {operand.LhsOwner}");
                    }

                    var rhsValue = CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, false);

                    switch (operand.LhsData)
                    {
                        case 0: //front
                            return Line($"{list}.AddFirst({rhsValue});");
                        case 1: //back
                            return Line($"{list}.AddLast({rhsValue});");
                        case 2:
                            throw new Exception("Unknown list push destination: " + operand.LhsData);
                    }
                }
                else if (operand.Operator == VMExpressionOperator.Pop)
                {
                    string list;
                    switch (operand.RhsOwner)
                    {
                        case VMVariableScope.MyList:
                            list = "context.Caller.MyList"; break;
                        case VMVariableScope.StackObjectList:
                            list = "context.StackObject.MyList"; break;
                        default:
                            throw new Exception($"Invalid scope for list operation pop! RHS: {operand.RhsOwner}");
                    }

                    switch (operand.RhsData)
                    {
                        case 0: //front
                            return new List<string>() {
                                "{ //list pop",
                                $"var list = {list};",
                                "_bResult = list.Count > 0;",
                                "if (_bResult) {",
                                CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "=", $"list.First.Value", false),
                                $"{list}.RemoveFirst();",
                                "}",
                                "} //end list pop"
                            };
                        case 1: //back
                            return new List<string>() {
                                "{ //list pop",
                                $"var list = {list};",
                                "_bResult = list.Count > 0;",
                                "if (_bResult) {",
                                CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, "=", $"list.Last.Value", false),
                                $"{list}.RemoveLast();",
                                "}",
                                "} //end list pop"
                            };
                        default:
                            throw new Exception("Unknown list pop source: " + operand.LhsData);
                    }
                }
            }

            if (basicOp != null)
            {
                return Line(CSScopeMemory.SetStatement(csContext, operand.LhsOwner, operand.LhsData, basicOp,
                    CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big), big));
            }

            if (basicExpressionOp != null)
            {
                var lExp = CSScopeMemory.GetExpression(csContext, operand.LhsOwner, operand.LhsData, big);
                var rExp = CSScopeMemory.GetExpression(csContext, operand.RhsOwner, operand.RhsData, big);
                return Line($"{lExp} {basicExpressionOp} {rExp}");
            }

            return Line($"//unknown expression type {operand.Operator}");
        }
    }
}
