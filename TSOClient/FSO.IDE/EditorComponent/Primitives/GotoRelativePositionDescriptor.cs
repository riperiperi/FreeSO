using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class GotoRelativePositionDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Position; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMGotoRelativePositionOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMGotoRelativePositionOperand)Operand;
            var result = new StringBuilder();

            var function = EditorScope.Behaviour.Get<STR>(130).GetString((int)op.Location+2);
            result.Append(function);
            result.Append(", ");
            result.Append(EditorScope.Behaviour.Get<STR>(131).GetString((int)op.Direction+2));

            var flagStr = new StringBuilder();
            string prepend = "";
            if (op.AllowDiffAlt) { flagStr.Append("Allow Different Altitudes"); prepend = ", "; }
            if (op.NoFailureTrees) { flagStr.Append(prepend + "No Failure Trees"); prepend = ", "; }

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
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Routes the Caller avatar to a position relative to the stack object. Returns true on success, false on failure.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Location:", "Location", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(130), -2)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Direction:", "Direction", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(131), -2)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Allow Different Alts", "AllowDiffAlt"),
                new OpFlag("No Failure Trees", "NoFailureTrees")
                }));
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Normally on failure routing primitives run a small routine which helps give the player more context on why the route failed. 'No Failure Trees' makes failed routes return false immediately.")));
        }
    }
}
