using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class InvokePluginDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.TSO; } }

        public override Type OperandType { get { return typeof(VMInvokePluginOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMInvokePluginOperand)Operand;
            var result = new StringBuilder();

            result.AppendLine(op.PluginID.ToString("x2"));

            result.Append("Avatar in local[" + op.PersonLocal + "], ");
            result.Append("Object in local[" + op.ObjectLocal + "], ");
            result.Append("Events in local[" + op.EventLocal + "] ");

            if (op.Joinable) result.Append("\r\n(Joinable)");

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Invokes a TSO UI 'plugin'. Sends an event in the Event Local to the plugin (eg, -2 = connect), and it will either return true for complete, or false for an event from the server. (with data in temp 0)")));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Avatar Local", "PersonLocal", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Object Local", "ObjectLocal", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Event Local", "EventLocal", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Plugin ID", "PluginID", new OpStaticValueBoundsProvider(int.MinValue, int.MaxValue)));
            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Joinable", "Joinable"),
                }));
        }
    }
}
