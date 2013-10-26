using System;
using System.Collections.Generic;
using System.Text;

namespace TSO_CityServer.Network
{
    /// <summary>
    /// Size of packet headers.
    /// </summary>
    public enum PacketHeaders
    {
        UNENCRYPTED = 3,
        ENCRYPTED = 5
    }
}
