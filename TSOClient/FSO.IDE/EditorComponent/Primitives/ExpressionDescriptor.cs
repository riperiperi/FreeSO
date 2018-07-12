using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class ExpressionDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return (Returns == PrimitiveReturnTypes.Done)?PrimitiveGroup.Math:PrimitiveGroup.Control; } }
        public override PrimitiveReturnTypes Returns {
            get {
                var op = (VMExpressionOperand)Operand;
                var rt = PrimitiveReturnTypes.TrueFalse;
                OpReturn.TryGetValue(op.Operator, out rt);
                return rt;
            }
        }
        public override Type OperandType { get { return typeof(VMExpressionOperand); } }

        public static Dictionary<VMExpressionOperator, string> OperatorStr = new Dictionary<VMExpressionOperator, string>()
        {
            { VMExpressionOperator.GreaterThan, ">" },
            { VMExpressionOperator.LessThan, "<" },
            { VMExpressionOperator.Equals, "==" },
            { VMExpressionOperator.PlusEquals, "+=" },
            { VMExpressionOperator.MinusEquals, "-=" },
            { VMExpressionOperator.Assign, ":=" },
            { VMExpressionOperator.MulEquals, "*=" },
            { VMExpressionOperator.DivEquals, "/=" },
            { VMExpressionOperator.IsFlagSet, "Has Flag:" },
            { VMExpressionOperator.SetFlag, "Set Flag:" },
            { VMExpressionOperator.ClearFlag, "Clear Flag:" },
            { VMExpressionOperator.IncAndLessThan, "++ and <" },
            { VMExpressionOperator.ModEquals, "%=" },
            { VMExpressionOperator.AndEquals, "&=" },
            { VMExpressionOperator.GreaterThanOrEqualTo, ">=" },
            { VMExpressionOperator.LessThanOrEqualTo, "<=" },
            { VMExpressionOperator.NotEqualTo, "!=" },
            { VMExpressionOperator.DecAndGreaterThan, "-- and >" },
            { VMExpressionOperator.Push, "Push Into" },
            { VMExpressionOperator.Pop, "Pop From" },
        };

        public static Dictionary<VMExpressionOperator, PrimitiveReturnTypes> OpReturn = new Dictionary<VMExpressionOperator, PrimitiveReturnTypes>()
        {
            { VMExpressionOperator.GreaterThan, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.LessThan, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.Equals, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.PlusEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.MinusEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.Assign, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.MulEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.DivEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.IsFlagSet, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.SetFlag, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.ClearFlag, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.IncAndLessThan, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.ModEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.AndEquals, PrimitiveReturnTypes.Done },
            { VMExpressionOperator.GreaterThanOrEqualTo, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.LessThanOrEqualTo, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.NotEqualTo, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.DecAndGreaterThan, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.Push, PrimitiveReturnTypes.TrueFalse },
            { VMExpressionOperator.Pop, PrimitiveReturnTypes.TrueFalse },
        };

        public override string GetBody(EditorScope scope)
        {
            var op = (VMExpressionOperand)Operand;
            var result = new StringBuilder();

            result.Append(scope.GetVarName(op.LhsOwner, (short)op.LhsData));
            result.Append(" ");
            result.Append(OperatorStr[op.Operator]);
            result.Append(" ");
            result.Append(scope.GetVarName(op.RhsOwner, (short)op.RhsData));

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Performs the specified expression. Returns the result, which is true only for assignments and true/false for conditionals.")));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "LHS: ", "LhsOwner", "LhsData"));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Operator: ", "Operator", new OpStaticNamedPropertyProvider(OperatorStr.Values.ToArray(), 0)));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "RHS: ", "RhsOwner", "RhsData"));
        }

    }
}
