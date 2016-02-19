using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;

namespace FSO.IDE.EditorComponent.Commands
{
    public class OpModifyCommand : BHAVCommand
    {
        public PrimitiveBox Prim;
        public byte[] NewOp;
        public byte[] OldOp;

        public OpModifyCommand(PrimitiveBox prim, byte[] newOp)
        {
            Prim = prim;
            NewOp = newOp;
            OldOp = prim.Instruction.Operand;
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            Prim.Instruction.Operand = NewOp;
            Prim.UpdateDisplay();
            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            Prim.Instruction.Operand = OldOp;
            Prim.RefreshOperand();
            Prim.UpdateDisplay();
            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
        }
    }
}
