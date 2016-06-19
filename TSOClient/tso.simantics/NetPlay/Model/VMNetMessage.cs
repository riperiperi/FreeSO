using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model
{
    public class VMNetMessage
    {
        public VMNetMessageType Type;
        public byte[] Data;

        public VMNetMessage(VMNetMessageType type, byte[] data)
        {
            Type = type;
            Data = data;
        }
    }
}
