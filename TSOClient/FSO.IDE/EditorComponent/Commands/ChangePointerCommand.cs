using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (TrueBranch)
            {
                InstUI.Instruction.TruePointer = (DestUI == null)?(byte)253:DestUI.InstPtr;
                InstUI.TrueUI = DestUI;
            }
            else
            {
                InstUI.Instruction.FalsePointer = (DestUI == null) ? (byte)253 : DestUI.InstPtr;
                InstUI.FalseUI = DestUI;
            }

            FSO.SimAntics.VM.BHAVChanged(bhav);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            if (TrueBranch)
            {
                InstUI.Instruction.TruePointer = (OldDestUI == null) ? (byte)253 : OldDestUI.InstPtr;
                InstUI.TrueUI = OldDestUI;
            }
            else
            {
                InstUI.Instruction.FalsePointer = (OldDestUI == null) ? (byte)253 : OldDestUI.InstPtr;
                InstUI.FalseUI = OldDestUI;
            }

            FSO.SimAntics.VM.BHAVChanged(bhav);
        }
    }
}
