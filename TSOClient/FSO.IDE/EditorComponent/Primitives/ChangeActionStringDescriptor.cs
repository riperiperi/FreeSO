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
    public class ChangeActionStringDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Control; } }

        public override Type OperandType { get { return typeof(VMChangeActionStringOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMChangeActionStringOperand)Operand;
            var result = new StringBuilder();

            var str = scope.GetResource<STR>(op.StringTable, (ScopeSource)op.Scope);

            if (str == null) result.Append("String #" + op.StringID + " in " + ((ScopeSource)op.Scope).ToString() + " STR#" + op.StringTable);
            else result.Append(str.GetString(op.StringID - 1));
        
            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("If we're running as an interaction's TEST function, "
                + "adds this interaction with the specified string to the pie menu, with the current Stack Object ID in parameter 0 of the interaction. "
                + "The first run overwrites - the following runs add additional entries.")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "STR Table:", "StringTable", new OpStaticValueBoundsProvider(0, 65535)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "String ID:", "StringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Scope:", "Scope", new OpStaticNamedPropertyProvider(typeof(ScopeSource))));
        }

    }
}
