using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public string GetAnimationName(EditorScope escope, VMAnimationScope scope, ushort id)
        {
            STR animTable = null;

            switch (scope)
            {
                case VMAnimationScope.Object:
                    var anitableID = escope.GetOBJD().AnimationTableID;
                    anitableID = 129;
                    animTable = escope.GetResource<STR>(anitableID, ScopeSource.Private);
                    break;
                case VMAnimationScope.Misc:
                    animTable = EditorScope.Globals.Resource.Get<STR>(156);
                    break;
                case VMAnimationScope.PersonStock:
                    animTable = EditorScope.Globals.Resource.Get<STR>(130);
                    break;
                case VMAnimationScope.Global:
                    animTable = EditorScope.Globals.Resource.Get<STR>(128);
                    break;
            }

            if (animTable == null) return "Unknown animation (bad source)";

            var animationName = animTable.GetString(id);
            return (animationName == null)?"Unknown animation #" + id:animationName;
        }
    }
}
