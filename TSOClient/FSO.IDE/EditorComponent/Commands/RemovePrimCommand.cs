using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.IDE.EditorComponent.Commands
{
    public class RemovePrimCommand : BHAVCommand
    {
        public PrimitiveBox Primitive;
        public List<PrimitiveBox> FromTrue;
        public List<PrimitiveBox> FromFalse;

        public RemovePrimCommand(List<PrimitiveBox> realPrims, PrimitiveBox prim)
        {
            Primitive = prim;
            FromFalse = new List<PrimitiveBox>();
            FromTrue = new List<PrimitiveBox>();

            foreach(var from in realPrims)
            {
                if (from.TrueUI == prim) FromTrue.Add(from);
                if (from.FalseUI == prim) FromFalse.Add(from);
            }
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
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

            foreach (var prim in FromTrue)
            {
                prim.TrueUI = null;
                prim.Instruction.TruePointer = 253;
            }

            foreach (var prim in FromFalse)
            {
                prim.FalseUI = null;
                prim.Instruction.FalsePointer = 253;
            }

            bhav.Instructions = newInst;
            editor.BHAVView.RemovePrimitive(Primitive);
            FSO.SimAntics.VM.BHAVChanged(bhav);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
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

            foreach (var prim in FromTrue)
            {
                prim.TrueUI = Primitive;
                prim.Instruction.TruePointer = Primitive.InstPtr;
            }

            foreach (var prim in FromFalse)
            {
                prim.FalseUI = Primitive;
                prim.Instruction.FalsePointer = Primitive.InstPtr;
            }

            bhav.Instructions = newInst;
            editor.BHAVView.Primitives.Add(Primitive);
            editor.BHAVView.RealPrim.Insert(Primitive.InstPtr, Primitive);
            editor.BHAVView.Add(Primitive);

            FSO.SimAntics.VM.BHAVChanged(bhav);

        }
    }
}
