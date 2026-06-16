using FSO.Common;

namespace FSO.Files.FSO
{
    public enum FSOServerHistoryType
    {
        /// <summary>
        /// Archive server - anonymous authentication + archive gameplay.
        /// </summary>
        Archive = 0,

        /// <summary>
        /// MMO type server - user registration/login + mmo gameplay.
        /// </summary>
        FreeSO = 1,

        /// <summary>
        /// Archive server, but triggered by a discord join.
        /// Can only store one of these in history for quick rejoins. Address uses basic obfuscation.
        /// </summary>
        DiscordArchive = 2
    }

    public class FSOServerHistoryItem
    {
        /// <summary>
        /// The type of server.
        /// </summary>
        public FSOServerHistoryType type { get; set; }

        /// <summary>
        /// Friendly name of the server. Updated when the server responds to the status query.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Address of the server. If this is an archive server, will be hostname:port, otherwise it will be an http api base url.
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// If UPnP is set, the server list will try all possible UPnP ports if the last address:port failed to respond.
        /// </summary>
        public ArchiveConfigFlags archiveFlags { get; set; }
    }

    /// <summary>
    /// Saved FSO server history.
    /// </summary>
    public class FSOServerHistory
    {
        public FSOServerHistoryItem[] servers;
    }
}
