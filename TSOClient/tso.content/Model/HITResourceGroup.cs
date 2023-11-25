using FSO.Files.HIT;

namespace FSO.Content.Model
{
    /// <summary>
    /// Groups related HIT resources, like the tsov2 series or newmain.
    /// </summary>
    public class HITResourceGroup
    {
        public EVT evt;
        public HITFile hit;
        public HSM hsm;
        public Hot hot; //used by ts1
    }
}
