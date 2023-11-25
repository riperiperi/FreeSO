using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;

namespace FSO.IDE.EditorComponent.Commands
{
    public class ToggleBreakpointCommand : BHAVCommand
    {
        public PrimitiveBox InstUI;

        public ToggleBreakpointCommand(PrimitiveBox instUI)
        {
            InstUI = instUI;
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            InstUI.Instruction.Breakpoint = !InstUI.Instruction.Breakpoint;

            FSO.SimAntics.VM.BHAVChanged(bhav);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            InstUI.Instruction.Breakpoint = !InstUI.Instruction.Breakpoint;

            FSO.SimAntics.VM.BHAVChanged(bhav);
        }
    }
}
