namespace FSO.Files.FSO
{
    /// <summary>
    /// Version info for a FreeSO client or server.
    /// Should be saved as `version.json` next to the binary.
    /// When acting as a server, sent to connecting clients so they can ensure the same version.
    /// </summary>
    public class FSOVersionInfo
    {
        public string id { get; set; }
        public string channel { get; set; }
        public string channelUrl { get; set; }
        public string publicKey { get; set; }
    }
}
