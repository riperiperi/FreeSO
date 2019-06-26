using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Translation
{
    public class TranslationContext
    {
        public bool TS1 = true;
        public AbstractTranslationPrimitives Primitives;

        public SimAnticsModule GlobalModule;
        public SimAnticsModule SemiGlobalModule;

        public TranslationContext GlobalContext;
        public TranslationContext SemiGlobalContext;

        public GameIffResource GlobalRes;
        public GameIffResource SemiGlobalRes;
        public GameIffResource ObjectRes;

        public Dictionary<ushort, StructuredBHAV> BHAVInfo = new Dictionary<ushort, StructuredBHAV>();

        public IffFile CurrentFile;
        public BHAV CurrentBHAV;
    }
}
