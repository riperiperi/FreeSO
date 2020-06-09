using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.IDE.EditorComponent.Commands
{
    public class SetFirstCommand : BHAVCommand
    {
        public PrimitiveBox Primitive;
        public PrimitiveBox Old0;
        public byte OldPtr;
        public List<PrimitiveBox> FromTrue;
        public List<PrimitiveBox> FromFalse;
        public List<PrimitiveBox> FromTrue0;
        public List<PrimitiveBox> FromFalse0;

        public SetFirstCommand(List<PrimitiveBox> realPrims, PrimitiveBox prim)
        {
            Primitive = prim;
            var prim0 = realPrims.FirstOrDefault(x => x.InstPtr == 0);
            Old0 = prim0;
            OldPtr = Primitive.InstPtr;
            FromFalse = new List<PrimitiveBox>();
            FromTrue = new List<PrimitiveBox>();
            FromFalse0 = new List<PrimitiveBox>();
            FromTrue0 = new List<PrimitiveBox>();

            foreach (var from in realPrims)
            {
                if (from.TrueUI == prim) FromTrue.Add(from);
                if (from.FalseUI == prim) FromFalse.Add(from);
                if (from.TrueUI == prim0) FromTrue0.Add(from);
                if (from.FalseUI == prim0) FromFalse0.Add(from);
            }
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            if (Primitive.Type != TREEBoxType.Primitive)
            {
                //do nothing.
            }
            else
            {
                bhav.Instructions[0] = Primitive.Instruction;
                bhav.Instructions[OldPtr] = Old0.Instruction;
                // TODO
                //Primitive.InstPtr = 0;
                //Old0.InstPtr = OldPtr;
                foreach (var prim in FromTrue) prim.Instruction.TruePointer = 0;
                foreach (var prim in FromFalse) prim.Instruction.FalsePointer = 0;
                foreach (var prim in FromTrue0) prim.Instruction.TruePointer = OldPtr;
                foreach (var prim in FromFalse0) prim.Instruction.FalsePointer = OldPtr;
                Content.Content.Get().Changes.ChunkChanged(bhav);
                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            if (Primitive.Type != TREEBoxType.Primitive)
            {
                //do nothing.
            }
            else
            {
                bhav.Instructions[0] = Old0.Instruction;
                bhav.Instructions[OldPtr] = Primitive.Instruction;
                // TODO
                //Primitive.InstPtr = OldPtr;
                //Old0.InstPtr = 0;
                foreach (var prim in FromTrue) prim.Instruction.TruePointer = OldPtr;
                foreach (var prim in FromFalse) prim.Instruction.FalsePointer = OldPtr;
                foreach (var prim in FromTrue0) prim.Instruction.TruePointer = 0;
                foreach (var prim in FromFalse0) prim.Instruction.FalsePointer = 0;
                Content.Content.Get().Changes.ChunkChanged(bhav);
                FSO.SimAntics.VM.BHAVChanged(bhav);
            }
        }
    }
}
