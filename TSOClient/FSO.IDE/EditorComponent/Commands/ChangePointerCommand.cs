using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;

namespace FSO.IDE.EditorComponent.Commands
{
    public class ChangePointerCommand : BHAVCommand
    {
        public PrimitiveBox InstUI;
        public PrimitiveBox DestUI;
        public PrimitiveBox OldDestUI;
        public bool TrueBranch;

        public ChangePointerCommand(PrimitiveBox instUI, PrimitiveBox destUI, bool trueBranch)
        {
            InstUI = instUI;
            DestUI = destUI;
            OldDestUI = trueBranch ? instUI.TrueUI : instUI.FalseUI;
            TrueBranch = trueBranch;
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (TrueBranch)
            {
                if (InstUI.Instruction != null) InstUI.Instruction.TruePointer = (DestUI == null)?(byte)253:DestUI.InstPtr;
                InstUI.TrueUI = DestUI;
                InstUI.TreeBox.TruePointer = (DestUI == null) ? (short)-1 : DestUI.TreeBox.InternalID;
            }
            else
            {
                if (InstUI.Instruction != null) InstUI.Instruction.FalsePointer = (DestUI == null) ? (byte)253 : DestUI.InstPtr;
                InstUI.FalseUI = DestUI;
                InstUI.TreeBox.FalsePointer = (DestUI == null) ? (short)-1 : DestUI.TreeBox.InternalID;
            }

            if (InstUI.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(InstUI.TreeBox.InternalID);

            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
            Content.Content.Get().Changes.ChunkChanged(tree);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (TrueBranch)
            {
                if (InstUI.Instruction != null) InstUI.Instruction.TruePointer = (OldDestUI == null) ? (byte)253 : OldDestUI.InstPtr;
                InstUI.TrueUI = OldDestUI;
                InstUI.TreeBox.TruePointer = (OldDestUI == null) ? (short)-1 : OldDestUI.TreeBox.InternalID;
            }
            else
            {
                if (InstUI.Instruction != null) InstUI.Instruction.FalsePointer = (OldDestUI == null) ? (byte)253 : OldDestUI.InstPtr;
                InstUI.FalseUI = OldDestUI;
                InstUI.TreeBox.FalsePointer = (OldDestUI == null) ? (short)-1 : OldDestUI.TreeBox.InternalID;
            }

            if (InstUI.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(InstUI.TreeBox.InternalID);

            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
            Content.Content.Get().Changes.ChunkChanged(tree);
        }
    }
}
