using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Diagnostics;
using System.Threading;
using GonzoNet;

namespace ProtocolAbstractionLibraryD
{
    /// <summary>
    /// CityInfo holds information about, and can be used to represent a city server.
    /// </summary>
    public class CityInfo
    {
        public NetworkClient Client;

        public string Name;
        public string Description;
        public ulong Thumbnail;
        public string UUID;
        public ulong Map;
        public bool Online = true;
        public CityInfoStatus Status;

        public string IP;
        public int Port;

        /// <summary>
        /// Initializes a new instance of CityInfo.
        /// </summary>
        /// <param name="IsServer">Is this a city server?</param>
        public CityInfo(bool IsServer)
        {
        }

        public List<CityInfoMessageOfTheDay> Messages = new List<CityInfoMessageOfTheDay>();
    }

    public class CityInfoMessageOfTheDay 
    {
        public string From;
        public string Subject;
        public string Body;
    }

    public enum CityInfoStatus
    {
        Ok = 1,
        Busy = 2,
        Full = 3,
        Reserved = 4
    }
}
