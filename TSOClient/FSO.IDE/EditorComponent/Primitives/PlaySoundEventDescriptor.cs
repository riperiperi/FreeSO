using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class PlaySoundEventDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Looks; } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.Done; } }

        public override Type OperandType { get { return typeof(VMPlaySoundOperand); } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMPlaySoundOperand)Operand;
            var result = new StringBuilder();

            result.Append("Play \"");
            var fwav = scope.GetResource<FWAV>(op.EventID, ScopeSource.Private);
            result.Append((fwav == null)?"*Event Missing!*":fwav.Name);
            result.Append("\"");

            var flagStr = new StringBuilder();
            string prepend = "";
            if (op.Loop) { flagStr.Append("Loop"); prepend = ", "; }
            if (op.NoPan) { flagStr.Append(prepend + "No Pan"); prepend = ", "; }
            if (op.NoZoom) { flagStr.Append(prepend + "No Zoom"); prepend = ", "; }
            if (op.StackObjAsSource) { flagStr.Append(prepend + "Stack Object as Source"); prepend = ", "; }

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
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Attempts to play the specified sound event.")));
            panel.Controls.Add(new OpSoundControl(master, escope, Operand, "Sound:"));
            //panel.Controls.Add(new OpComboControl(master, escope, Operand, "Sound: ", "EventID", new OpSoundNameProvider()));

            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Volume (unused): ", "Volume", new OpStaticValueBoundsProvider(0, 100)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Loop", "Loop"),
                new OpFlag("No 3D Positioning", "NoPan"),
                new OpFlag("No Zoom quieting", "NoZoom"),
                new OpFlag("Use Stack Object as Source", "StackObjAsSource"),
                }));
        }

    }

    public class OpSoundNameProvider : OpNamedPropertyProvider
    {
        public override Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand operand)
        {
            var map = new Dictionary<int, string>();
            var evts = scope.GetAllResource<FWAV>(ScopeSource.Private);
            foreach (var evt in evts)
            {
                map.Add(evt.ChunkID, evt.Name);
            }
            return map;
        }
    }
}
