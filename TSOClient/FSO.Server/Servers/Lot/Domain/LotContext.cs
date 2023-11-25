using FSO.Server.Protocol.Gluon.Model;

namespace FSO.Server.Servers.Lot.Domain
{
    public class LotContext
    {
        public uint Id;
        public int DbId;
        public int ShardId;
        public uint ClaimId;
        public ClaimAction Action;
        public bool HighMax; 

        public bool JobLot
        {
            get
            {
                return (Id & 0x40000000) > 0;
            }
        }
    }
}
