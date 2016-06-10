using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Sound
{
    public class VMSoundTransfer
    {
        public short SourceID; //only copy on ID+GUID match. In future, match on persist too?
        public uint SourceGUID;
        public VMSoundEntry SFX;

        public VMSoundTransfer(short id, uint guid, VMSoundEntry sfx)
        {
            SourceID = id;
            SourceGUID = guid;
            SFX = sfx;
        }
    }
}
