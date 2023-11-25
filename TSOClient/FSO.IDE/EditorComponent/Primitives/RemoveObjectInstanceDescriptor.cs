using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Primitives;
using System;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class RemoveObjectInstanceDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object ; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMRemoveObjectInstanceOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRemoveObjectInstanceOperand)Operand;
            var result = new StringBuilder();
            
            result.Append(EditorScope.Behaviour.Get<STR>(156).GetString((int)op.Target));

            var flagStr = new StringBuilder();
            string prepend = "";
            if (op.ReturnImmediately) { flagStr.Append(prepend + "Return Immediately"); prepend = ", "; }
            if (op.CleanupAll) { flagStr.Append(prepend + "Cleanup Multitile"); prepend = ", "; }

            if (flagStr.Length != 0)
            {
                result.Append("\r\n(");
                result.Append(flagStr);
                result.Append(")");
            }
            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Removes the specified object instance.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Target Object:", "Target", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(156))));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Return Immediately", "ReturnImmediately"),
                new OpFlag("Cleanup Multitile", "CleanupAll"),
                }));
        }
    }
}
