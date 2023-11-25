using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class SnapDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Position; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMSnapOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSnapOperand)Operand;
            var result = new StringBuilder();
            

            switch (op.Mode)
            {
                case VMSnapSlotScope.Global:
                    var gslots = scope.GetResource<STR>(257, ScopeSource.Global);
                    result.Append(gslots?.GetString(op.Index) ?? "Global slot " + op.Index);
                    break;
                case VMSnapSlotScope.Literal:
                    var slotNs = scope.GetResource<STR>(257, ScopeSource.Private);
                    result.Append((slotNs != null && slotNs.GetString(op.Index) != null) ? slotNs.GetString(op.Index) : "Private slot " + op.Index);
                    break;
                case VMSnapSlotScope.StackVariable:
                    result.Append("Private slot indexed by " + scope.GetVarName(VMVariableScope.Parameters, (short)op.Index));
                    break;
                case VMSnapSlotScope.BeContained:
                    result.Append("Be contained in Stack Object");
                    break;
                case VMSnapSlotScope.InFront:
                    result.Append("In front of Stack Object");
                    break;
            }
            
            //if (op.NoFailureTrees) result.Append("\r\n(No Failure Trees)");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Attempts to snap the Caller object to a specified SLOT on the Stack Object. Returns true on success, false on failure. An error code for failure is placed into 'Primitive Result'.")));

            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Source: ", "Mode", new OpStaticNamedPropertyProvider(new string[] { "Private[parameter]", "Contained in Stack Obj", "In front of Stack Obj", "Private", "Global" }, 0)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Slot: ", "Index", new OpSnapSlotNameProvider()));

            /*
            TODO: Flags for Snap

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("No Failure Trees", "NoFailureTrees"),
                }));*/
        }
    }

    public class OpSnapSlotNameProvider : OpNamedPropertyProvider
    {
        public override Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand operand)
        {
            var map = new Dictionary<int, string>();
            var op = (VMSnapOperand)operand;
            switch (op.Mode)
            {
                case VMSnapSlotScope.Global:
                    var gslots = scope.GetResource<STR>(257, ScopeSource.Global);
                    if (gslots == null)
                    {
                        var gslotsres = scope.GetResource<SLOT>(100, ScopeSource.Global);
                        for (int i = 0; i < gslotsres.Chronological.Count; i++)
                            map.Add(i, i.ToString());
                    } else
                    {
                        for (int i = 0; i < gslots.Length; i++)
                            map.Add(i, gslots.GetString(i));
                    }
                    return map;
                case VMSnapSlotScope.Literal:
                    var slotNs = scope.GetResource<STR>(257, ScopeSource.Private);
                    var slotRes = scope.GetResource<SLOT>(scope.GetOBJD().SlotID, ScopeSource.Private);
                    if (slotRes == null || !slotRes.Slots.ContainsKey(3)) return map;
                    var slots = slotRes.Slots[3];
                    for (int i = 0; i < slots.Count; i++)
                        map.Add(i, (slotNs != null && slotNs.GetString(i) != null)?slotNs.GetString(i):"slot "+i);
                    return map;
                case VMSnapSlotScope.StackVariable:
                    var str = scope.GetVarScopeDataNames(VMVariableScope.Parameters);
                    for (int i = 0; i < str.Count; i++)
                        map.Add(str[i].Value, str[i].Name);
                    return map;
                default:
                    map.Add(0, "---");
                    return map;
            }
        }
    }
}
