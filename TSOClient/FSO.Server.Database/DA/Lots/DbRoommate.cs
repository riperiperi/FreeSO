using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public class DbRoommate
    {
        public uint avatar_id;
        public int lot_id;
        public byte permissions_level;
        public byte is_pending;
    }
}
