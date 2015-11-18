using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
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
    public class RefreshDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Looks; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.Done; } }
        public override Type OperandType { get { return typeof(VMRefreshOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRefreshOperand)Operand;
            var result = new StringBuilder();

            result.Append(EditorScope.Behaviour.Get<STR>(211).GetString(op.TargetObject));
            result.Append(" ");
            result.Append(EditorScope.Behaviour.Get<STR>(212).GetString(op.RefreshType));
            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, 
                new OpStaticTextProvider("Refreshes the specified property of the object on demand.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Target Object:", "TargetObject", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(211))));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Property:", "RefreshType", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(212))));
        }
    }
}
