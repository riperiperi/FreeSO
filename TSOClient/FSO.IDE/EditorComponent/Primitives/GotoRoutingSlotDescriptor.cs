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
using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class GotoRoutingSlotDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Position; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMGotoRoutingSlotOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMGotoRoutingSlotOperand)Operand;
            var result = new StringBuilder();

            switch (op.Type)
            {
                case VMSlotScope.Global:
                    var gslots = scope.GetResource<STR>(257, ScopeSource.Global);
                    result.Append(gslots?.GetString(op.Data) ?? ("Global slot " + op.Data));
                    break;
                case VMSlotScope.Literal:
                    var slotNs = scope.GetResource<STR>(257, ScopeSource.Private);
                    result.Append((slotNs != null && slotNs.GetString(op.Data) != null) ? slotNs.GetString(op.Data) : "Private slot " + op.Data);
                    break;
                case VMSlotScope.StackVariable:
                    result.Append("Private slot indexed by " + scope.GetVarName(VMVariableScope.Parameters, (short)op.Data));
                    break;
            }
            
            if (op.NoFailureTrees) result.Append("\r\n(No Failure Trees)");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Routes the Avatar to the specified Routing Slot, returning True on success and False on failure. Normally upon failure, the Avatar also performs a small explanatory animation, though this can be disabled.")));

            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Source: ", "Type", new OpStaticNamedPropertyProvider(new string[] { "Private[parameter]", "Private", "Global" }, 0)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Slot: ", "Data", new OpRoutingSlotNameProvider()));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("No Failure Trees", "NoFailureTrees"),
                }));
        }
    }

    public class OpRoutingSlotNameProvider : OpNamedPropertyProvider
    {
        public override Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand operand)
        {
            var map = new Dictionary<int, string>();
            var op = (VMGotoRoutingSlotOperand)operand;
            switch (op.Type)
            {
                case VMSlotScope.Global:
                    var gslots = scope.GetResource<STR>(257, ScopeSource.Global);
                    for (int i = 0; i < gslots.Length; i++)
                        map.Add(i, gslots.GetString(i));
                    return map;
                case VMSlotScope.Literal:
                    var slotNs = scope.GetResource<STR>(257, ScopeSource.Private);
                    var slotRes = scope.GetResource<SLOT>(scope.GetOBJD().SlotID, ScopeSource.Private);
                    if (slotRes == null) return map;
                    var slots = slotRes.Slots[3];
                    for (int i = 0; i < slots.Count; i++)
                        map.Add(i, (slotNs != null && slotNs.GetString(i) != null)?slotNs.GetString(i):"slot "+i);
                    return map;
                case VMSlotScope.StackVariable:
                    var str = scope.GetVarScopeDataNames(VMVariableScope.Parameters);
                    for (int i = 0; i < str.Count; i++)
                        map.Add(str[i].Value, str[i].Name);
                    return map;
                default:
                    return map;
            }
        }
    }
}
