using FSO.Server.Protocol.Gluon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
