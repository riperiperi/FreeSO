using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class TS1InventoryOperationsDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMTS1InventoryOperationsOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMTS1InventoryOperationsOperand)Operand;
            var result = new StringBuilder();

            var obj = Content.Content.Get().WorldObjects.Get(op.GUID);
            result.Append("Mode: " + op.Mode.ToString() + "\r\n");
            result.Append("Type: " + op.TokenType.ToString() + "\r\n");

            if (op.GUID != 0)
            {
                result.Append("Object: ");
                result.Append((obj == null) ? ("0x" + Convert.ToString(op.GUID.ToString("x8"))) : obj.OBJ.ChunkLabel);
            }

            var flagStr = new StringBuilder();
            flagStr.Append(op.Flags.ToString("X2") + " " + op.Flags2.ToString("X2"));
            string prepend = ", ";
            if (op.CountInTemp) { flagStr.Append(prepend + "Count in Temp 0"); prepend = ", "; }
            if (op.UseObjectInTemp4) { flagStr.Append(prepend + "Use Object in Temp 4"); prepend = ", "; }

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
                new OpStaticTextProvider("Largely unknown. Implement cases as you see them! (and flags)")));
            
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Mode:", "Mode", new OpStaticNamedPropertyProvider(typeof(VMTS1InventoryMode))));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Token Type:", "TokenType", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpObjectControl(master, escope, Operand, "Token Object:", "GUID"));

            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Flags1:", "Flags", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Flags2:", "Flags2", new OpStaticValueBoundsProvider(0, 255)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Count in Temp", "CountInTemp"),
                new OpFlag("Use Object in Temp 4", "UseObjectInTemp4"),
                }));
        }
    }
}
