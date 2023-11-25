namespace FSO.Server.Protocol.Voltron.DataService
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class clsid : System.Attribute
    {
        public uint Value;

        public clsid(uint value) {
            this.Value = value;
        }
    }
}
