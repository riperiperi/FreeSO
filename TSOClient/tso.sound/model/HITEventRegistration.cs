using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.HIT.model
{
    public class HITEventRegistration
    {
        public string Name;
        public uint EventType;
        public uint TrackID;
        public HITResourceGroup ResGroup; //used to access this event's hit code
    }
}
