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
    public class RunTreeByNameDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Control; } }

        public override Type OperandType { get { return typeof(VMRunTreeByNameOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRunTreeByNameOperand)Operand;
            var result = new StringBuilder();

            var str = scope.GetResource<STR>(op.StringTable, (op.StringScope == 1) ? ScopeSource.Global : ScopeSource.Private);
            result.Append(EditorScope.Behaviour.Get<STR>(222).GetString(op.Destination) + "\r\n");

            if (str == null) result.Append("(Tree #" + op.StringID + " in "+ ((op.StringScope == 1)?"global":"local") + " STR#"+op.StringTable+")");
            else result.Append("("+str.GetString(Math.Max(0, op.StringID-1))+")");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Runs a named tree in the stack object. The name is sourced from an STR resource (normally 303).")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "STR Table:", "StringTable", new OpStaticValueBoundsProvider(0, 65535)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "String ID:", "StringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Destination:", "Destination", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(222), 0)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Scope:", "StringScope", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(159), 0)));
        }
    }
}
