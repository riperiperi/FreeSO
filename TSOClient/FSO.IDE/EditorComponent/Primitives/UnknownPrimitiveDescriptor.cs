using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class UnknownPrimitiveDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveRegistry.GetGroupOf((byte)PrimID); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMSubRoutineOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSubRoutineOperand)Operand;
            return (op.Arg0 & 0xFF).ToString("x2") + " " + (op.Arg0 >> 8).ToString("x2") + " " +
                (op.Arg1 & 0xFF).ToString("x2") + " " + (op.Arg1 >> 8).ToString("x2") + " " +
                (op.Arg2 & 0xFF).ToString("x2") + " " + (op.Arg2 >> 8).ToString("x2") + " " +
                (op.Arg3 & 0xFF).ToString("x2") + " " + (op.Arg3 >> 8).ToString("x2") + " ";
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("This primitive is not implemented, but its hexadecimal values may be edited directly.")));
            var provider = new OpStaticValueBoundsProvider(-32768, 32767);
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 1:", "Arg0", provider, true));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 2:", "Arg1", provider, true));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 3:", "Arg2", provider, true));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 4:", "Arg3", provider, true));
        }

    }
}
