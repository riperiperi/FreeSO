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
            if (NewPrimitive.Type != PrimBoxType.Primitive)
            {
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
                NewPrimitive.InstPtr = (byte)(newInst.Length - 1);

                bhav.Instructions = newInst;
                editor.BHAVView.AddPrimitive(NewPrimitive);
                NewPrimitive.UpdateDisplay();
                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            if (NewPrimitive.Type != PrimBoxType.Primitive)
            {
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
                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
        }
    }
}
