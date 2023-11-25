using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class ChangeSuitDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Sim; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMChangeSuitOrAccessoryOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMChangeSuitOrAccessoryOperand)Operand;
            var result = new StringBuilder();

            //TODO

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Changes the Caller's suit to the specified. Can change outfits and add accessories.")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Suit Data: ", "SuitData", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Suit Scope:", "SuitScope", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(227))));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Suit Op:", "Flags", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(228))));
        }
    }
}
