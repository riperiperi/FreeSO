using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class AnimateSimDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Sim; } }

        public override Type OperandType { get { return typeof(VMAnimateSimOperand); } }

        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMAnimateSimOperand)Operand;
            var result = new StringBuilder();

            if (op.AnimationID != 0)
            {
                var animName = (GetAnimationName(scope, op.Source, op.AnimationID));
                if (op.IDFromParam) animName = "Animation indexed by param[" + op.AnimationID + "]";
                switch (op.Mode)
                {
                    case 0:
                        result.Append("Play \"");
                        result.Append(animName);
                        result.Append("\"");
                        break;
                    case 1:
                        result.Append("Set \"");
                        result.Append(animName);
                        result.Append("\" as Background Animation");
                        break;
                    case 2:
                        result.Append("Set \"");
                        result.Append(animName);
                        result.Append("\" as Carry Animation");
                        break;
                    case 3:
                        result.Append("Stop Carry, then Play \"");
                        result.Append(animName);
                        result.Append("\"");
                        break;
                }

                var flagStr = new StringBuilder();
                string prepend = "";
                if (op.PlayBackwards) { flagStr.Append("Play Backwards"); prepend = ", "; }
                if (op.StoreFrameInLocal) { flagStr.Append(prepend + "Place events in Local " + op.LocalEventNumber); prepend = ", "; }
                if (op.Hurryable) { flagStr.Append(prepend + "Hurryable"); prepend = ", "; }

                if (flagStr.Length != 0)
                {
                    result.Append("\r\n(");
                    result.Append(flagStr);
                    result.Append(")");
                }
            } else
            {
                result.Append("Reset");
            }
            return result.ToString();
        }

        public static STR GetAnimTable(EditorScope escope, VMAnimationScope scope) {
            switch (scope)
            {
                case VMAnimationScope.Object:
                    var anitableID = escope.GetOBJD().AnimationTableID;
                    var table = escope.GetResource<STR>(anitableID, ScopeSource.Private); 
                    if (table == null) table = escope.GetResource<STR>(129, ScopeSource.Private);
                    return table;
                case VMAnimationScope.Misc:
                    return EditorScope.Globals.Resource.Get<STR>(156);
                case VMAnimationScope.PersonStock:
                    return EditorScope.Globals.Resource.Get<STR>(130);
                case VMAnimationScope.Global:
                    return EditorScope.Globals.Resource.Get<STR>(128);
            }
            return null;
        }

        public static string GetAnimationName(EditorScope escope, VMAnimationScope scope, ushort id)
        {
            STR animTable = GetAnimTable(escope, scope);
            if (animTable == null) return "Unknown animation (bad source)";

            var animationName = animTable.GetString(id);
            return (animationName == null)?"Unknown animation #" + id:animationName;
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Animates the caller sim. Returns False when an animation event occurs, and True when the animation is compete.")));
            panel.Controls.Add(new OpAnimControl(master, escope, Operand, "Animation:"));
            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Animation Source:", "Source", new OpStaticNamedPropertyProvider(
                new string[] { "This Tree's Object", "Global", "Person", "Misc" }, 0)));

            panel.Controls.Add(new OpComboControl(master, escope, Operand, "Animation Mode:", "Mode", new OpStaticNamedPropertyProvider(
                new string[] { "Play", "Set as Background", "Set as Carry", "Clear Carry & Play" }, 0)));

            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Expected Events:", "ExpectedEventCount", new OpStaticValueBoundsProvider(0, 255)));
            panel.Controls.Add(new OpFlagsControl(master, escope, Operand, "Flags:", new OpFlag[] {
                new OpFlag("Play Backwards", "PlayBackwards"),
                new OpFlag("Place Events in Local", "StoreFrameInLocal"),
                new OpFlag("Hurryable", "Hurryable"),
                new OpFlag("ID in Parameter", "IDFromParam")
                }));

            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Target Local:", "LocalEventNumber", new OpStaticValueBoundsProvider(0, 255)));
        }
    }

    public class OpAnimationNameProvider : OpNamedPropertyProvider
    {
        public override Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand operand)
        {
            var op = (VMAnimateSimOperand)operand;
            var result = new Dictionary<int, string>();
            result.Add(0, "Stop Animation");
            var anims = AnimateSimDescriptor.GetAnimTable(scope, op.Source);

            if (anims != null) {

                for (int i = 1; i < anims.Length; i++)
                {
                    result.Add(i, anims.GetString(i));
                }

            }
            return result; 
        }
    }
}
