using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.Commands
{
    public abstract class BHAVCommand
    {
        public abstract void Execute(BHAV bhav, UIBHAVEditor editor);
        public abstract void Undo(BHAV bhav, UIBHAVEditor editor);
    }
}
