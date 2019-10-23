using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;

namespace FSO.IDE.EditorComponent.Commands
{
    public class AddPrimCommand : BHAVCommand
    {
        PrimitiveBox NewPrimitive;

        public AddPrimCommand(PrimitiveBox prim)
        {
            NewPrimitive = prim;
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (NewPrimitive.Type != TREEBoxType.Primitive)
            {
                var ptr = NewPrimitive.TreeBox.TruePointer; //if this is a goto, this will contain the label id
                var comment = NewPrimitive.TreeBox.Comment;
                NewPrimitive.SetTreeBox(tree.MakeNewSpecialBox(NewPrimitive.Type));
                NewPrimitive.TreeBox.TruePointer = ptr;
                NewPrimitive.TreeBox.Comment = comment;
                NewPrimitive.CopyPosToTree();
                editor.BHAVView.Primitives.Add(NewPrimitive);
                editor.BHAVView.Add(NewPrimitive);
            }
            else
            {
                var newInst = new BHAVInstruction[bhav.Instructions.Length + 1];
                for (int i = 0; i < bhav.Instructions.Length; i++)
                {
                    newInst[i] = bhav.Instructions[i];
                }
                newInst[newInst.Length - 1] = NewPrimitive.Instruction;
                NewPrimitive.SetTreeBox(tree.MakeNewPrimitiveBox(TREEBoxType.Primitive));
                NewPrimitive.CopyPosToTree();

                bhav.Instructions = newInst;
                editor.BHAVView.AddPrimitive(NewPrimitive);
                NewPrimitive.UpdateDisplay();

                Content.Content.Get().Changes.ChunkChanged(bhav);

                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
            Content.Content.Get().Changes.ChunkChanged(tree);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (NewPrimitive.Type != TREEBoxType.Primitive)
            {
                if (NewPrimitive.TreeBox.InternalID != -1)
                {
                    tree.DeleteBox(NewPrimitive.TreeBox);
                }
                editor.BHAVView.Primitives.Remove(NewPrimitive);
                editor.BHAVView.Remove(NewPrimitive);
            }
            else
            {
                //primitive we added should be at the end
                var newInst = new BHAVInstruction[bhav.Instructions.Length - 1];
                for (int i = 0; i < newInst.Length; i++)
                {
                    newInst[i] = bhav.Instructions[i];
                }

                bhav.Instructions = newInst;
                editor.BHAVView.RemovePrimitive(NewPrimitive);
                Content.Content.Get().Changes.ChunkChanged(bhav);
                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
            Content.Content.Get().Changes.ChunkChanged(tree);
        }
    }
}
