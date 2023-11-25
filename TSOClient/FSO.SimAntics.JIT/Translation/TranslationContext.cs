using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Runtime;
using System.Collections.Generic;

namespace FSO.SimAntics.JIT.Translation
{
    public class TranslationContext : IBHAVInfo
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

        public bool BHAVYields(ushort id)
        {
            //in previously compiled context
            StructuredBHAV newBHAV;
            if (BHAVInfo.TryGetValue(id, out newBHAV))
            {
                return newBHAV.Yields;
            }
            else
            {
                return true; //if we don't know if it yields, err on the side of caution.
            }
        }
    }
}
