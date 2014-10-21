using System;
using System.Collections.Generic;
using System.Text;
using TSO.Files.HIT;

namespace TSO.HIT.model
{
    public class HITEventRegistration
    {
        public string Name;
        public HITEvents EventType;
        public uint TrackID;
        public HITResourceGroup ResGroup; //used to access this event's hit code
    }
}
