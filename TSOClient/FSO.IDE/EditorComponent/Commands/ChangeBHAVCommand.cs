using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.Commands
{
    public class ChangeBHAVCommand : BHAVCommand
    {
        BHAV Target;
        EditorScope TargetScope;
        VMStackFrame Frame;

        BHAV Old;
        EditorScope OldScope;
        VMStackFrame OldFrame;

        BHAVPrimSelect SelectCallback;

        public ChangeBHAVCommand(BHAV target, EditorScope scope, VMStackFrame frame, BHAVPrimSelect callback)
        {
            Target = target;
            TargetScope = scope;
            Frame = frame;
            SelectCallback = callback;
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            Old = editor.BHAVView.EditTarget;
            OldScope = editor.BHAVView.Scope;
            OldFrame = editor.DebugFrame;
            editor.BHAVView.OnSelectedChanged -= SelectCallback;
            editor.SwitchBHAV(Target, TargetScope, Frame);
            editor.BHAVView.OnSelectedChanged += SelectCallback;
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            editor.BHAVView.OnSelectedChanged -= SelectCallback;
            editor.SwitchBHAV(Old, OldScope, OldFrame);
            editor.BHAVView.OnSelectedChanged += SelectCallback;
        }
    }
}
