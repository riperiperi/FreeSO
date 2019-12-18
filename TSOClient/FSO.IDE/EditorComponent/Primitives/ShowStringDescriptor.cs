using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class ShowStringDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Debug; } }

        public override Type OperandType { get { return typeof(VMShowStringOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMShowStringOperand)Operand;
            var result = new StringBuilder();

            var str = scope.GetResource<STR>(op.StringTable, ScopeSource.Private);

            if (str == null) result.Append("String #" + op.StringID + " STR#" + op.StringTable);
            else result.Append(str.GetString(op.StringID - 1));

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Allows the stack object to print a string. " +
                "In FreeSO, this message prints to chat, and can be used to make NPCs talk.")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "STR Table:", "StringTable", new OpStaticValueBoundsProvider(0, 65535)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "String ID:", "StringID", new OpStaticValueBoundsProvider(0, 65535)));
            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] { new OpFlag("No Chat History", "NoHistory") }));
        }
    }
}
