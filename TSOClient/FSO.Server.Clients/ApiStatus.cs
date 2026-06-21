namespace FSO.Server.Clients
{
    public class ApiStatus
    {
        public string name { get; set; }
        public int[] shards { get; set; }
        public int onlineCount { get; set; }
        public string versionInfo { get; set; }
    }

}
