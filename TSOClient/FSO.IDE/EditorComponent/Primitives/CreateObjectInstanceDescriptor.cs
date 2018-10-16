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
    public class CreateObjectInstanceDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Object ; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override Type OperandType { get { return typeof(VMCreateObjectInstanceOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMCreateObjectInstanceOperand)Operand;
            var result = new StringBuilder();

            var obj = Content.Content.Get().WorldObjects.Get(op.GUID);
            
            result.Append((obj == null)?("0x" + Convert.ToString(op.GUID.ToString("x8"))):obj.OBJ.ChunkLabel);
            result.Append("\r\n");
            
            result.Append(EditorScope.Behaviour.Get<STR>(167).GetString((int)op.Position));
            if ((int)op.Position > 7) result.Append(op.LocalToUse);

            //TODO: INTERACTION CALLBACK

            var flagStr = new StringBuilder();
            string prepend = "";
            if (op.NoDuplicate) { flagStr.Append(prepend + "Do not Duplicate"); prepend = ", "; }
            if (op.PassObjectIds) { flagStr.Append(prepend + "Pass Object IDs to main"); prepend = ", "; }
            if (op.PassTemp0) { flagStr.Append(prepend + "Pass Temp 0 to main"); prepend = ", "; }
            if (op.PersistInDB) { flagStr.Append(prepend + "Persist in Database");  prepend = ", "; }

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
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Creates a new instance of the specified object.")));

            panel.Controls.Add(new OpObjectControl(master, escope, Operand, "Object Type: ", "GUID"));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Position:", "Position", new OpStaticNamedPropertyProvider(EditorScope.Behaviour.Get<STR>(167))));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Position Local: ", "LocalToUse", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Interaction Callback: ", "InteractionCallback", new OpStaticValueBoundsProvider(0, 255)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Pass Object IDs", "PassObjectIds"),
                new OpFlag("Pass Temp 0 to Main", "PassTemp0"),
                new OpFlag("Persist in DB", "PersistInDB"),

                new OpFlag("Return Immediately", "ReturnImmediately"),
                new OpFlag("Fail if Non-Empty", "FailIfNonEmpty"),
                new OpFlag("Face StackOBJ Dir", "FaceStackObjDir"),
                new OpFlag("Use Neighbor (TS1)", "UseNeighbor"),
                }));

            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider(
                "Pass Object IDs provides the option to pass both the Stack Object and the Caller Object to the newly "+
                "created object's main routine, as its Stack Object and Temp 0 (ID) respectively.\r\n\r\n"+
                "Return Immediately returns from this interaction and immediately runs it again with the created "+
                "object's ID in parameter 0.")));
        }
    }
}
