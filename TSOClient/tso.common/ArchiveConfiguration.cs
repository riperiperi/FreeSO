using Newtonsoft.Json;
using System.Security.Cryptography;

namespace FSO.Common
{
    [Flags]
    public enum ArchiveConfigFlags
    {
        None = 0,
        Offline = 1 << 0,
        UPnP = 1 << 1,
        HideNames = 1 << 2,
        Verification = 1 << 3,
        AllOpenable = 1 << 4,
        DebugFeatures = 1 << 5,
        AllowLotCreation = 1 << 6,
        AllowSimCreation = 1 << 7,
        LockArchivedSims = 1 << 8,
        ReducedTickRate = 1 << 9,
        CityEditor = 1 << 10,
        CityEditorMods = 1 << 11,
        CityEditorAllUsers = 1 << 12,
        DebugFeaturesMods = 1 << 13,
        DebugFeaturesAllUsers = 1 << 14,

        DedicatedServer = 1 << 16,

        Default = UPnP | AllOpenable | AllowLotCreation | AllowSimCreation,

        QuickStartDesirable = Offline | AllowLotCreation | AllowSimCreation | AllOpenable,
        QuickStartUndesirable = UPnP | ReducedTickRate | Verification,
    }

    public class ArchiveConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("flags")]
        public ArchiveConfigFlags Flags { get; set; }
        [JsonProperty("archiveDataDirectory")]
        public string ArchiveDataDirectory { get; set; } // Effectively equal to the nfs
        [JsonProperty("cityPort")]
        public ushort CityPort { get; set; }
        [JsonProperty("lotPort")]
        public ushort LotPort { get; set; }
        [JsonProperty("serverKey")]
        public string ServerKey { get; set; }
        [JsonProperty("serverPublicKey")]
        public string ServerPublicKey { get; set; }
        [JsonProperty("gameScale")]
        public float GameScale { get; set; } = 1;
        [JsonProperty("allowUserApi")]
        public bool AllowUserApi { get; set; }

        [JsonProperty("initialFunds")]
        public int InitialFunds { get; set; } = 20000;

        // Runtime
        public IDisposable[] Disposables;
        public EventConfig? Events;

        public void LoadEvents()
        {
            // Try and load associated event config
            var eventPath = Path.Combine(ArchiveDataDirectory, "events.json");

            try
            {
                var eventJson = File.ReadAllText(eventPath);

                Events = EventConfig.FromJson(eventJson);
            }
            catch { }
        }

        public void SaveEvents()
        {
            if (Events == null)
            {
                return;
            }

            // Try and save associated event config
            var eventPath = Path.Combine(ArchiveDataDirectory, "events.json");

            try
            {
                File.WriteAllText(eventPath, Events.Value.ToJson());
            }
            catch { }
        }
    }

    public enum ClientArchiveHistoryType
    {
        /// <summary>
        /// MMO type server - user registration/login + mmo gameplay.
        /// </summary>
        FreeSO = 0,

        /// <summary>
        /// Archive server - anonymous authentication + archive gameplay.
        /// </summary>
        Archive = 1,

        /// <summary>
        /// Archive server, but triggered by a discord join.
        /// Can only store one of these in history for quick rejoins. Address uses basic obfuscation.
        /// </summary>
        DiscordArchive = 2
    }

    public class ClientArchiveHistoryItem(ClientArchiveHistoryType serverType, string name, string address, ArchiveConfigFlags flags)
    {
        /// <summary>
        /// The type of server.
        /// </summary>
        [JsonProperty("serverType")]
        public ClientArchiveHistoryType ServerType { get; set; } = serverType;

        /// <summary>
        /// Friendly name of the server. Updated when the server responds to the status query.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = name;

        /// <summary>
        /// Address of the server. If this is an archive server, will be hostname:port, otherwise it will be an http api base url.
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; } = address;

        /// <summary>
        /// If UPnP is set, the server list will try all possible UPnP ports if the last address:port failed to respond.
        /// </summary>
        [JsonProperty("flags")]
        public ArchiveConfigFlags Flags { get; set; } = flags;
    }

    public class ClientArchiveConfiguration : JsonConfig
    {
        [JsonProperty("_comment")]
        public string HeadingComment { get; set; } = "Archive client + self-hosting configuration. Don't send this to other users, as it contains authentication keys!";

        private static ClientArchiveConfiguration defaultInstance;

        public static ClientArchiveConfiguration Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = Load<ClientArchiveConfiguration>(Path.Combine(FSOEnvironment.UserDir, "archiveConfig.json"));

                    defaultInstance.VerifyKeys();
                }
                return defaultInstance;
            }
        }

        private static string GenerateGUID()
        {
            return Guid.NewGuid().ToString();
        }

        // Client configuration

        [JsonProperty("playerName")]
        public string PlayerName { get; set; } = "";
        [JsonProperty("lastJoinedHost")]
        public string LastJoinedHost { get; set; } = "127.0.0.1";
        [JsonProperty("selectedArchiveName")]
        public string SelectedArchiveName { get; set; } = "FreeSO Archive";

        // Keys
        [JsonProperty("serverPrivateKey")]
        public string ServerPrivateKey { get; set; } = "";
        [JsonProperty("serverPublicKey")]
        public string ServerPublicKey { get; set; } = "";
        [JsonProperty("clientPrivateKey")]
        public string ClientPrivateKey { get; set; } = "";
        [JsonProperty("clientPublicKey")]
        public string ClientPublicKey { get; set; } = "";

        // Server configuration
        [JsonProperty("serverName")]
        public string ServerName { get; set; } = "";
        [JsonProperty("flags")]
        public int Flags { get; set; } = (int)ArchiveConfigFlags.Default;
        [JsonProperty("cityPort")]
        public ushort CityPort { get; set; } = 33101;
        [JsonProperty("lotPort")]
        public ushort LotPort { get; set; } = 34101;
        [JsonProperty("gameScale")]
        public float GameScale { get; set; } = 1;

        [JsonProperty("joinHistory")]
        public List<ClientArchiveHistoryItem> JoinHistory = [];

        public EventConfig? Events;

        public ArchiveConfiguration ToHostConfig()
        {
            return new ArchiveConfiguration()
            {
                Name = ServerName,
                Flags = (ArchiveConfigFlags)Flags,
                ArchiveDataDirectory = "",
                CityPort = CityPort,
                LotPort = LotPort,
                GameScale = GameScale,

                ServerKey = ServerPrivateKey,
                ServerPublicKey = ServerPublicKey,
            };
        }

        public void ApplyHostConfig(ArchiveConfiguration config)
        {
            Flags = (int)config.Flags;
            CityPort = config.CityPort;
            LotPort = config.LotPort;
            GameScale = config.GameScale;
        }

        private void GenerateServerRsaKeys()
        {
            var rsa = RSA.Create();

            ServerPublicKey = rsa.ExportRSAPublicKeyPem().Replace('\n', '^');
            ServerPrivateKey = rsa.ExportRSAPrivateKeyPem().Replace('\n', '^');
        }

        private bool VerifyServerRsaKeys()
        {
            if (ServerPrivateKey == "" || ServerPublicKey == "")
            {
                return false;
            }

            var rsa = RSA.Create();

            try
            {
                rsa.ImportFromPem(ServerPublicKey.Replace('^', '\n'));

                var publicRsaParams = rsa.ExportParameters(false);

                rsa.ImportFromPem(ServerPrivateKey.Replace('^', '\n'));

                // If the parameters were updated, it was valid.

                // This will fail if a private key wasn't imported.
                var privateRsaParams = rsa.ExportParameters(true);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void VerifyKeys()
        {
            bool changed = false;
            if (!VerifyServerRsaKeys())
            {
                GenerateServerRsaKeys();
                changed = true;
            }

            if (ClientPrivateKey == "")
            {
                ClientPrivateKey = GenerateGUID();
                changed = true;
            }

            if (ClientPublicKey == "")
            {
                ClientPublicKey = GenerateGUID();
                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        public static bool ValidDisplayName(string name)
        {
            // Maybe there's a better location for this.
            return name != null && name.Length > 0 && name.Length <= 24;
        }

        public string GetDefaultServerName()
        {
            return $"{PlayerName}'s Server";
        }

        public string GetServerNameOrDefault()
        {
            return ServerName.Length > 0 ? ServerName : GetDefaultServerName();
        }

        public void RegisterJoin(ClientArchiveHistoryItem item)
        {
            var existing = JoinHistory.FindIndex(x => x.Address == item.Address && x.ServerType == item.ServerType);

            if (existing != -1)
            {
                // If the item already exists, we're moving it to the top (so remove the old entry)
                JoinHistory.RemoveAt(existing);
            }

            // Add it to the top.

            if (item.ServerType == ClientArchiveHistoryType.DiscordArchive)
            {
                // Only remember one discord server at a time.
                JoinHistory.RemoveAll(x => x.ServerType == ClientArchiveHistoryType.DiscordArchive);
            }

            JoinHistory.Insert(0, item);

            Save();
        }

        public void RemoveJoin(ClientArchiveHistoryItem item)
        {
            var existing = JoinHistory.FindIndex(x => x.Address == item.Address && x.ServerType == item.ServerType);

            if (existing != -1)
            {
                // If the item already exists, we're removing it.
                JoinHistory.RemoveAt(existing);
            }

            Save();
        }
    }
}
