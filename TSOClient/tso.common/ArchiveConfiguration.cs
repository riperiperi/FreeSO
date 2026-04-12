using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

        DedicatedServer = 1 << 16,

        Default = UPnP | AllOpenable | AllowLotCreation | AllowSimCreation,

        QuickStartDesirable = Offline | AllowLotCreation | AllowSimCreation | AllOpenable,
        QuickStartUndesirable = UPnP | ReducedTickRate | Verification,
    }

    public class ArchiveConfiguration
    {
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

    public class ClientArchiveConfiguration : IniConfig
    {
        public override string HeadingComment => "Archive client + self-hosting configuration. Don't send this to other users, as it contains authentication keys!";

        private static ClientArchiveConfiguration defaultInstance;

        public static ClientArchiveConfiguration Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new ClientArchiveConfiguration(Path.Combine(FSOEnvironment.UserDir, "archiveConfig.ini"));

                    defaultInstance.VerifyKeys();
                }
                return defaultInstance;
            }
        }

        private static string GenerateGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public ClientArchiveConfiguration(string path) : base(path)
        {
        }

        private Dictionary<string, string> _DefaultValues = new Dictionary<string, string>()
        {
            { "PlayerName", "" },
            { "LastJoinedHost", "127.0.0.1" },
            { "SelectedArchiveName", "FreeSO Archive" },

            { "ServerPrivateKey", "" },
            { "ServerPublicKey", "" },
            { "ClientPrivateKey", "" },
            { "ClientPublicKey", "" },

            { "Flags", ((int)ArchiveConfigFlags.Default).ToString() },
            { "ArchiveDataDirectory", "" },
            { "CityPort", "33101" },
            { "LotPort", "34101" },
            { "GameScale", "1" },
        };

        public override Dictionary<string, string> DefaultValues
        {
            get { return _DefaultValues; }
            set { _DefaultValues = value; }
        }


        // Client configuration
        public string PlayerName { get; set; }
        public string LastJoinedHost { get; set; }
        public string SelectedArchiveName { get; set; }

        // Keys
        public string ServerPrivateKey { get; set; }
        public string ServerPublicKey { get; set; }
        public string ClientPrivateKey { get; set; }
        public string ClientPublicKey { get; set; }

        // Server configuration
        public int Flags { get; set; }
        public ushort CityPort { get; set; }
        public ushort LotPort { get; set; }
        public float GameScale { get; set; } = 1;

        public EventConfig? Events;

        public ArchiveConfiguration ToHostConfig()
        {
            return new ArchiveConfiguration()
            {
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
                var rsaParams = rsa.ExportParameters(false);

                rsa.ImportFromPem(ServerPublicKey.Replace('^', '\n'));

                // If the parameters were updated, it was valid.

                var publicRsaParams = rsa.ExportParameters(false);

                if (rsaParams.Equals(publicRsaParams))
                {
                    // No public key replacement...
                    return false;
                }

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
    }
}
