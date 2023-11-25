using System;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public interface IGluonCall
    {
        Guid CallId { get; set; }
    }
}
