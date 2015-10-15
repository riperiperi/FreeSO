using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    }
}
