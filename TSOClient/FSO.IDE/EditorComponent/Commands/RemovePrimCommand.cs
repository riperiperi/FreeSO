using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.Commands
{
    public class RemovePrimCommand : BHAVCommand
    {
        public PrimitiveBox Primitive;
        public List<PrimitiveBox> FromTrue;
        public List<PrimitiveBox> FromFalse;

        public RemovePrimCommand(List<PrimitiveBox> prims, PrimitiveBox prim)
        {
            Primitive = prim;
            FromFalse = new List<PrimitiveBox>();
            FromTrue = new List<PrimitiveBox>();

            foreach(var from in prims)
            {
                if (from.TrueUI == prim) FromTrue.Add(from);
                if (from.FalseUI == prim) FromFalse.Add(from);
            }
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (Primitive.Type != TREEBoxType.Primitive)
            {
                if (Primitive.TreeBox.InternalID != -1)
                {
                    tree.DeleteBox(Primitive.TreeBox);
                }
                editor.BHAVView.Primitives.Remove(Primitive);
                editor.BHAVView.Remove(Primitive);
            }
            else
            {
                var newInst = new BHAVInstruction[bhav.Instructions.Length - 1];
                byte index = 0;
                for (int i = 0; i < bhav.Instructions.Length; i++)
                {
                    if (i != Primitive.InstPtr)
                    {
                        var inst = bhav.Instructions[i];
                        newInst[index++] = inst;
                        if (inst.TruePointer < 253 && inst.TruePointer > Primitive.InstPtr) inst.TruePointer--;
                        if (inst.FalsePointer < 253 && inst.FalsePointer > Primitive.InstPtr) inst.FalsePointer--;
                    }
                }

                bhav.Instructions = newInst;
                editor.BHAVView.RemovePrimitive(Primitive);
            }

            foreach (var prim in FromTrue)
            {
                prim.TrueUI = null;
                prim.TreeBox.TruePointer = -1;
                if (prim.Instruction != null) prim.Instruction.TruePointer = 253;
                if (prim.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(prim.TreeBox.InternalID);
            }

            foreach (var prim in FromFalse)
            {
                prim.FalseUI = null;
                prim.TreeBox.FalsePointer = -1;
                if (prim.Instruction != null) prim.Instruction.FalsePointer = 253;
                if (prim.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(prim.TreeBox.InternalID);
            }
            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
            Content.Content.Get().Changes.ChunkChanged(tree);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            if (Primitive.Type != TREEBoxType.Primitive)
            {
                //put the tree item back at the end
                if (Primitive.TreeBox.InternalID != -1) tree.InsertRemovedBox(Primitive.TreeBox);
                editor.BHAVView.Primitives.Add(Primitive);
                editor.BHAVView.Add(Primitive);
            }
            else
            {
                var newInst = new BHAVInstruction[bhav.Instructions.Length + 1];
                byte index = 0;
                for (int i = 0; i < newInst.Length; i++)
                {
                    if (i == Primitive.InstPtr) newInst[i] = Primitive.Instruction;
                    else
                    {
                        var inst = bhav.Instructions[index++];
                        newInst[i] = inst;
                        if (inst.TruePointer < 252 && inst.TruePointer >= Primitive.InstPtr) inst.TruePointer++;
                        if (inst.FalsePointer < 252 && inst.FalsePointer >= Primitive.InstPtr) inst.FalsePointer++;
                    }
                }

                bhav.Instructions = newInst;
                editor.BHAVView.AddPrimitive(Primitive);
                //insert the tree item
                if (Primitive.TreeBox.InternalID != -1) tree.InsertRemovedBox(Primitive.TreeBox);
            }

            foreach (var prim in FromTrue)
            {
                prim.TrueUI = Primitive;
                prim.TreeBox.TruePointer = Primitive.TreeBox.InternalID;
                if (prim.Instruction != null) prim.Instruction.TruePointer = Primitive.InstPtr;
                if (prim.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(prim.TreeBox.InternalID);
            }

            foreach (var prim in FromFalse)
            {
                prim.FalseUI = Primitive;
                prim.TreeBox.FalsePointer = Primitive.TreeBox.InternalID;
                if (prim.Instruction != null) prim.Instruction.FalsePointer = Primitive.InstPtr;
                if (prim.Type == TREEBoxType.Label) editor.BHAVView.UpdateLabelPointers(prim.TreeBox.InternalID);
            }

            Content.Content.Get().Changes.ChunkChanged(bhav);
            FSO.SimAntics.VM.BHAVChanged(bhav);
            Content.Content.Get().Changes.ChunkChanged(tree);
        }
    }
}
