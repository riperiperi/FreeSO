using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class TestObjectTypeDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMTestObjectTypeOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMTestObjectTypeOperand)Operand;
            var result = new StringBuilder();

            result.Append("Object with ID: ");
            result.Append(scope.GetVarName(op.IdOwner, (short)op.IdData));
            result.Append("\r\n has (master) type ");
            var obj = Content.Content.Get().WorldObjects.Get(op.GUID);

            result.Append((obj == null) ? ("0x" + Convert.ToString(op.GUID.ToString("x8"))) : obj.OBJ.ChunkLabel);

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Tests if the object with ID in the specific scope has the specified type (or master type, if selected).")));
            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Object ID:", "IdOwner", "IdData"));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Object GUID: ", "GUID", new OpStaticValueBoundsProvider(Int32.MinValue, Int32.MaxValue)));
        }
    }
}
