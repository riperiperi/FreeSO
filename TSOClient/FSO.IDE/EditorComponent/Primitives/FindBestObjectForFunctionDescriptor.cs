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
    public class FindBestObjectForFunctionDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Control; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMFindBestObjectForFunctionOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMFindBestObjectForFunctionOperand)Operand;
            var result = new StringBuilder();

            result.Append("Find Best Object For Function: ");
            var function = EditorScope.Behaviour.Get<STR>(201).GetString((int)op.Function);
            result.Append(function);

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Finds the 'best' object for a specified entry point, ranked by a combination of distance and the relevant score in its Object Data.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Function: ", "Function", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(201), 0)));
        }
    }
}
