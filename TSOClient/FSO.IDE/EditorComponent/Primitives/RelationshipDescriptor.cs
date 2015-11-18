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

    public class OldRelationshipDescriptor : RelationshipDescriptor
    {
        public override Type OperandType { get { return typeof(VMOldRelationshipOperand); } }
    }

    public class RelationshipDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMRelationshipOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMRelationshipOperand)Operand;
            var result = new StringBuilder();

            var old = (Operand is VMOldRelationshipOperand);

            if (op.SetMode == 0)
            {
                result.Append(scope.GetVarName(op.VarScope, op.VarData));
                result.Append(" := ");
            }
            result.Append(" Relationship Var #");
            result.Append(op.RelVar);
            result.Append("\r\n of ");
            result.Append(EditorScope.Behaviour.Get<STR>((ushort)(old?170:235)).GetString((int)op.Mode));
            if (op.Mode>1) result.Append(" ("+scope.GetVarScopeDataName(SimAntics.Engine.Scopes.VMVariableScope.Local, op.Local)+")");

            if (op.SetMode >= 1)
            {
                result.Append("\r\n ");
                result.Append((op.SetMode == 1) ? ":= " : "+= ");
                result.Append(scope.GetVarName(op.VarScope, op.VarData));
            }

            var flagStr = new StringBuilder();
            string prepend = "";
            if (op.FailIfTooSmall) { flagStr.Append(prepend + "Fail if too small"); prepend = ","; }
            if (op.UseNeighbor) { flagStr.Append(prepend + "Use Neighbor"); prepend = ","; }

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
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, 
                new OpStaticTextProvider("Tracks 'relationship' variables of one object to another. Can be used between both objects and avatars, and can optionally persist relationships in the city server. Returns False on failure if 'Fail if too small' is checked.")));

            var old = (Operand is VMOldRelationshipOperand);

            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Get/Set/Adjust:", "SetMode", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(169))));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Mode:", "Mode", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>((ushort)(old ? 170 : 235)))));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Relationship Var:", "RelVar", new OpStaticValueBoundsProvider(0, 255)));

            if (old) panel.Controls.Add(new OpComboControl(master, escope, Operand, "Target Parameter:", "VarData", new OpStaticNamedPropertyProvider(escope.GetVarScopeDataNames(VMVariableScope.Parameters))));
            else panel.Controls.Add(new OpScopeControl(master, escope, Operand, "Target Variable:", "VarScope", "VarData"));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Fail if too small", "FailIfTooSmall"),
                new OpFlag("Use Neighbor", "UseNeighbor"),
                }));

            if (!old) panel.Controls.Add(new OpComboControl(master, escope, Operand, "Object in Local:", "Local", new OpStaticNamedPropertyProvider(escope.GetVarScopeDataNames(VMVariableScope.Local))));
        }
    }
}
