using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff.chunks;
using TSO.Content;

namespace TSO.Simantics.model
{
    public class VMBHAVOwnerPair
    {
        public BHAV bhav;
        public GameIffResource owner;

        public VMBHAVOwnerPair(BHAV bhav, GameIffResource owner)
        {
            this.bhav = bhav;
            this.owner = owner;
        }
    }
}
