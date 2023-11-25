using FSO.Files.Formats.IFF.Chunks;
using FSO.Content;

namespace FSO.SimAntics.Model
{
    public class VMBHAVOwnerPair
    {
        public BHAV bhav;
        public VMRoutine routine;
        public GameObject owner;

        public VMBHAVOwnerPair(BHAV bhav, GameObject owner)
        {
            this.bhav = bhav;
            this.owner = owner;
        }

        public VMBHAVOwnerPair(VMRoutine routine, GameObject owner)
        {
            this.bhav = routine.Chunk;
            this.routine = routine;
            this.owner = owner;
        }
    }
}
