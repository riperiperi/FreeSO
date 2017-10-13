using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class DialogDescriptors : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Looks; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMDialogOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMDialogOperand)Operand;
            var result = new StringBuilder();

            result.Append("Type: " + op.Type.ToString() + "\r\n");

            var flagStr = new StringBuilder();

            STR source;
            switch (PrimID)
            {
                case 38:
                    source = scope.GetResource<STR>(301, ScopeSource.Global); break;
                case 39:
                    source = scope.GetResource<STR>(301, ScopeSource.SemiGlobal); break;
                default:
                    source = scope.GetResource<STR>(301, ScopeSource.Private); break;
            }

            if (source == null)
            {
                result.Append("String Table #301 Missing!");
            } else
            {
                string rprepend = "";
                if (op.TitleStringID > 0) { result.Append("[" + source.GetString(op.TitleStringID-1) + "]"); rprepend = "\r\n"; };
                if (op.MessageStringID > 0) { result.Append(rprepend + source.GetString(op.MessageStringID-1)); rprepend = "\r\n"; };

                var def = new string[] { "Yes", "No", "Cancel" };
                var answers = new byte[] { op.YesStringID, op.NoStringID, op.CancelStringID }.Zip(def, (x, defn) => (x == 0) ? defn : source.GetString(x-1)).ToList();

                switch (op.Type)
                {
                    case VMDialogType.Message:
                        answers = new List<string>() { (op.YesStringID == 0) ? "OK" : answers[0] }; break;
                    case VMDialogType.YesNo:
                        answers.RemoveAt(2); break;
                    case VMDialogType.YesNoCancel:
                        break;
                    default:
                        answers.Clear(); break;
                }

                result.Append(rprepend);
                if (answers.Count > 0)
                {
                    result.Append("(");
                    for (int i=0; i<answers.Count; i++)
                    {
                        result.Append(answers[i]);
                        if (i + 1 < answers.Count) result.Append(", ");
                    }
                    result.Append(")");
                }
            }

            string prepend = "";
            if (op.Continue) { flagStr.Append("Non-blocking"); prepend = ", "; }
            if (op.UseTemp1) { flagStr.Append(prepend + "UseTemp1"); prepend = ", "; }
            if (op.UseTempXL) { flagStr.Append(prepend + "UseTempXL"); prepend = ", "; }

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
                new OpStaticTextProvider("Show a dialog to the sim in Stack Object (TSO) or the sole player (TS1). " +
                    "This dialog can either block thread execution for a response, or simply show the dialog and ignore the response.")));
            switch (PrimID)
            {
                case 36:
                    panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                    new OpStaticTextProvider("The following strings are sourced from the private STR resource #301.")));
                    break;
                case 38:
                    panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                    new OpStaticTextProvider("The following strings are sourced from the global STR resource #301.")));
                    break;
                case 39:
                    panel.Controls.Add(new OpLabelControl(master, escope, Operand,
                    new OpStaticTextProvider("The following strings are sourced from the semi-global STR resource #301.")));
                    break;
            }
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Title:", "TitleStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Message:", "MessageStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Yes Button:", "YesStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "No Button:", "NoStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Cancel Button:", "CancelStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Icon Name:", "IconNameStringID", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Type:", "Type", new OpStaticNamedPropertyProvider(typeof(VMDialogType))));
            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] { new OpFlag("Non-blocking", "Continue"), new OpFlag("Use Temp XL", "UseTempXL"), new OpFlag("Use Temp 1", "UseTemp1") }));
        }
    }
}
