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
    public class SetToNextDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMSetToNextOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSetToNextOperand)Operand;
            var result = new StringBuilder();
            var nextObject = EditorScope.Behaviour.Get<STR>(164).GetString((int)op.SearchType);
            result.Append(nextObject);

            if (op.SearchType == VMSetToNextSearchType.ObjectOfType) { result.Append(" 0x"); result.Append(op.GUID.ToString("x8")); }
            if (op.SearchType == VMSetToNextSearchType.ObjectAdjacentToObjectInLocal) { result.Append(" "); result.Append(op.Local.ToString()); }

            var flagStr = new StringBuilder();
            if (op.TargetData != 0 || op.TargetOwner != VMVariableScope.StackObjectID) { flagStr.Append("Result in "+ scope.GetVarName(op.TargetOwner, (short)op.TargetData)); }

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
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Cycles the object with ID in the Target location to the next object of a specified type. Returns false when none or no more are available.")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Search Type: ", "SearchType", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(164), 0)));

            panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Target Scope: ", "TargetOwner", "TargetData"));

            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Properties for specific search types:")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Object Type: ", "GUID", new OpStaticValueBoundsProvider(int.MinValue, int.MaxValue)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Local: ", "Local", new OpStaticValueBoundsProvider(-32768, 32767)));
        }
    }
}
