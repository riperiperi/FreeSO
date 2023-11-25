using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class SetBalloonHeadlineDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Looks; } }

        public override Type OperandType { get { return typeof(VMSetBalloonHeadlineOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        private static int[] GroupOffsets =
        {
            0,
            100,
            400,
            200,
            300,
            500,
            600,
            0x000, //algorithmic
            700,
            800,
            900 //magic
        };

        private static IffFile Sprites;

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSetBalloonHeadlineOperand)Operand;
            var result = new StringBuilder();

            var content = Content.Content.Get();
            if (Sprites == null)
                Sprites = new IffFile(content.TS1 ?
                        Path.Combine(content.TS1BasePath, "GameData/Sprites.iff") :
                        content.GetPath("objectdata/globals/sprites.iff"));

            result.AppendLine((op.OfStackOBJ) ? "Set Headline of Stack Object" : "Set my Headline");
            if (op.Duration == 0)
            {
                result.Append("Clear Headline");
            }
            else
            {
                var balloon = Sprites.Get<SPR>((ushort)(GroupOffsets[(int)VMSetBalloonHeadlineOperandGroup.Balloon] + op.Type));

                string iconName;
                if (op.Group == VMSetBalloonHeadlineOperandGroup.Algorithmic)
                {
                    if (op.Algorithmic < 2) iconName = "Stack Object Icon";
                    else iconName = "Object in Local[" + op.Algorithmic + "] Icon";
                } else {
                    var icon = Sprites.Get<SPR>((ushort)(GroupOffsets[(int)op.Group] + op.Index));
                    iconName = icon?.ChunkLabel ?? op.Index.ToString();
                }

                if (op.Indexed) iconName += " + temp[0]";
                result.AppendLine("Category " + op.Group.ToString() + ", Type " + iconName);
                result.AppendLine("Balloon Type " + balloon?.ChunkLabel ?? "none");
                result.Append("Duration " + op.Duration + ((op.DurationInLoops) ? " loops" : " ticks"));

                var flagStr = new StringBuilder();
                string prepend = "";
                if (op.Crossed) { flagStr.Append("Crossed"); prepend = ", "; }
                if (op.Backwards) { flagStr.Append(prepend + "Backwards"); prepend = ", "; }
                if (op.Inactive) { flagStr.Append(prepend + "Inactive"); prepend = ", "; }

                if (flagStr.Length != 0)
                {
                    result.Append("\r\n(");
                    result.Append(flagStr);
                    result.Append(")");
                }
            }

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            var content = Content.Content.Get();
            if (Sprites == null)
                Sprites = new IffFile(content.TS1 ?
                        Path.Combine(content.TS1BasePath, "GameData/Sprites.iff") :
                        content.GetPath("objectdata/globals/sprites.iff"));

            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Shows a 'Headline' above the specified object; ")));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Category", "Group", new OpStaticNamedPropertyProvider(typeof(VMSetBalloonHeadlineOperandGroup))));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Index", "Index", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Balloon", "Type", new OpStaticNamedPropertyProvider(
                Enumerable.Range(0, 32)
                    .ToDictionary(x => x, x => Sprites.Get<SPR>((ushort)(GroupOffsets[(int)VMSetBalloonHeadlineOperandGroup.Balloon] + x))?.ChunkLabel)
                    .Where(x => x.Value != null)
                    .ToDictionary(x => x.Key, x => x.Value)
                )));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Duration", "Duration", new OpStaticValueBoundsProvider(-32768, 32767)));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Algorithmic", "Algorithmic", new OpStaticValueBoundsProvider(0, 0x7FFF)));

            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Of Stack Object", "OfStackOBJ"),
                new OpFlag("Crossed", "Crossed"),
                new OpFlag("Animate Backwards", "Backwards"),
                new OpFlag("Indexed by Temp[0]", "Indexed"),
                new OpFlag("Duration In Loops", "DurationInLoops"),
                }));
        }
    }
}
