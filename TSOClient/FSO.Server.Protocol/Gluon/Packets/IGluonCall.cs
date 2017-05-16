using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public interface IGluonCall
    {
        Guid CallId { get; set; }
    }
}
