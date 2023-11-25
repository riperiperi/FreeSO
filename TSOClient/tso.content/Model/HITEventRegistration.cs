using FSO.Files.HIT;

namespace FSO.Content.Model
{
    public class HITEventRegistration
    {
        public string Name;
        public HITEvents EventType;
        public uint TrackID;
        public HITResourceGroup ResGroup; //used to access this event's hit code
    }
}
