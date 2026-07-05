using FSO.Common.Enum;
using System.Text;
using static FSO.UI.Model.DiscordRpc;

namespace FSO.UI.Model
{
    public struct RpcSecret
    {
        public bool ArchiveMode;
        public string ServerID;
        public string ServerHostname;
        public uint LotID;

        private static void XorBytes(byte[] data, byte[] pattern)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= pattern[i % pattern.Length];
            }
        }

        public static string EncodeHostname(string id, string hostname)
        {
            var hostnameBytes = Encoding.UTF8.GetBytes(hostname);
            var idBytes = Encoding.UTF8.GetBytes(id);

            XorBytes(hostnameBytes, idBytes);

            return Convert.ToBase64String(hostnameBytes);
        }

        public static string DecodeHostname(string id, string encoded)
        {
            var encodedBytes = Convert.FromBase64String(encoded);
            var idBytes = Encoding.UTF8.GetBytes(id);

            XorBytes(encodedBytes, idBytes);

            return Encoding.UTF8.GetString(encodedBytes);
        }

        public RpcSecret(string secret)
        {
            if (secret.StartsWith('#'))
            {
                ArchiveMode = true;
                var split = secret[1..].Split('#');

                if (split.Length != 3)
                {
                    throw new FormatException("Invalid number of join secret fields");
                }

                ServerID = split[0];
                ServerHostname = DecodeHostname(split[0], split[1]);
                if (!uint.TryParse(split[2], out LotID))
                {
                    LotID = 0;
                }
            }
            else
            {
                var split = secret.Split('#');

                if (!uint.TryParse(split[0], out LotID))
                {
                    LotID = 0;
                }
            }
        }

        public override string ToString()
        {
            if (ArchiveMode)
            {
                return $"#{ServerID ?? ""}#{EncodeHostname(ServerID, ServerHostname ?? "")}#{LotID}";
            }
            else
            {
                return $"{LotID}#";
            }
        }
    }

    public static class DiscordRpcEngine
    {
        public static bool Active;
        public static bool Disable;
        public static RpcSecret? Secret;
        public static EventHandlers Events;

        private static RpcSecret BroadcastSecret;

        public static bool PublicArchive => BroadcastSecret.ArchiveMode && !string.IsNullOrEmpty(BroadcastSecret.ServerHostname);
        public static string ArchiveID => BroadcastSecret.ServerID ?? "";

        public static void Init()
        {
            try
            {
                var handlers = new EventHandlers();
                handlers.readyCallback += Ready;
                handlers.errorCallback += Error;
                handlers.joinCallback += Join;
                handlers.spectateCallback += Spectate;
                handlers.disconnectedCallback += Disconnected;
                handlers.requestCallback += Request;
                Events = handlers;

                DiscordRpc.Initialize("378352963468525569", ref handlers, true, null);
            } catch (Exception)
            {
                Active = false;
            }
        }

        public static void Update()
        {
            if (Disable) return;
            try
            {
                DiscordRpc.RunCallbacks();
            }
            catch (Exception)
            {
                Active = false;
                Disable = true;
            }
        }

        public static void SetArchiveAddress(string address)
        {
            BroadcastSecret.ArchiveMode = true;
            BroadcastSecret.ServerHostname = address;

            SendFSOPresenceIngame();
        }

        public static void SetArchiveID(string id)
        {
            BroadcastSecret.ArchiveMode = true;
            BroadcastSecret.ServerID = id;
        }

        public static void SetArchivePlayers(int count)
        {
            ArchivePlayers = count;

            SendFSOPresenceIngame();
        }

        public static void Reset()
        {
            BroadcastSecret = default;
        }

        // Method for other game screens
        public static void SendFSOPresence(string state, string details = null)
        {

            if (!Active) return; // RPC not active
            var presence = new DiscordRpc.RichPresence();
            
            presence.largeImageKey = "sunrise_crater";
            presence.largeImageText = "Sunrise Crater";

            presence.state = state;
            presence.details = details == null ? "" : details;

            DiscordRpc.UpdatePresence(ref presence);
        }

        private static string ActiveSim;
        private static string LotName;
        private static int LotID;
        private static int Players;
        private static int MaxSize;
        private static int CatID;
        private static string CDNUrl;
        private static bool IsPrivate;
        private static int ArchivePlayers = 1;

        public static void SendFSOPresence(string activeSim, string lotName, int lotID, int players, int maxSize, int catID, string cdnUrl, bool isPrivate = false)
        {
            ActiveSim = activeSim;
            LotName = lotName;
            LotID = lotID;
            Players = players;
            MaxSize = maxSize;
            CatID = catID;
            CDNUrl = cdnUrl;
            IsPrivate = isPrivate;

            SendFSOPresenceIngame();
        }

        // Standard DiscordRpc presence method
        private static void SendFSOPresenceIngame()
        {
            if (!Active) return;
            var presence = new DiscordRpc.RichPresence();

            bool isJob = false;

            if (!IsPrivate)
            {
                if (LotName?.StartsWith("{job:") == true)
                {
                    isJob = true;

                    var jobStr = "";
                    var split = LotName.Split(':');
                    if (split.Length > 2)
                    {
                        switch (split[1])
                        {
                            case "0": // Robot Factory
                                jobStr = "Robot Factory";
                                break;
                            case "1": // Restaurant
                                jobStr = "Restaurant";
                                break;
                            case "2": // Nightclub
                                jobStr = "Nightclub";
                                break;
                            default: // Other
                                jobStr = "Job Lot";
                                break;
                        }
                        jobStr += " | Level " + split[2].Trim('}');
                    }
                    else
                        jobStr = "Job Lot";
                    if (ActiveSim != null) presence.details = "Playing as " + ActiveSim;
                    presence.state = jobStr;
                }
                else
                {
                    if (ActiveSim == null)
                    {
                        presence.state = LotName ?? "Idle in City";
                        presence.details = "";
                    }                       
                    else
                    {
                        presence.details = "Playing as " + ActiveSim;
                        presence.state = LotName ?? "Idle in City";
                    }
                }                
                
            }
            else
            {
                presence.state = "Online";
                presence.details = "Privacy Enabled";
            }

            presence.largeImageKey = "sunrise_crater";
            presence.largeImageText = "Sunrise Crater";

            BroadcastSecret.LotID = (uint)LotID;

            if (BroadcastSecret.ArchiveMode)
            {
                presence.state += " (archive)";
                if (PublicArchive)
                {
                    presence.details += " (server joinable)";
                    presence.joinSecret = BroadcastSecret.ToString();

                    presence.smallImageKey = "sunrise_crater";
                    presence.smallImageText = "Joinable Server";

                    presence.partyMax = 128;
                    presence.partySize = ArchivePlayers;
                    presence.partyId = "shared";
                }
            }

            if (LotName != null && !IsPrivate)
            {
                presence.joinSecret = BroadcastSecret.ToString();
                //presence.matchSecret = lotID + "#" + lotName+".";
                //presence.spectateSecret = lotID + "#" + lotName + "..";
                presence.partyMax = MaxSize;
                presence.partySize = Players;
                presence.partyId = LotID.ToString();

                if (CDNUrl != null && !isJob)
                {
                    presence.smallImageKey = "cat_" + CatID;
                    presence.smallImageText = CapFirstWord(((LotCategory)CatID).ToString());

                    presence.largeImageKey = $"{CDNUrl}/userapi/city/1/{LotID}.png";
                    presence.largeImageText = presence.state;
                }
                else
                {
                    presence.largeImageKey = "cat_" + CatID;
                    presence.largeImageText = CapFirstWord(((LotCategory)CatID).ToString());
                }
            }

            DiscordRpc.UpdatePresence(ref presence);
        }

        private static string CapFirstWord(string cat)
        {
            return char.ToUpperInvariant(cat[0]) + cat.Substring(1);
        }

        public static void Ready()
        {
            Active = true;
        }

        public static void Error(int errorCode, string message)
        {

        }

        public static void Join(string secret)
        {
            Secret = new RpcSecret(secret);
        }

        public static void Spectate(string secret)
        {
            Secret = new RpcSecret(secret);
        }

        public static void Disconnected(int errorCode, string message)
        {

        }

        public static void Request(DiscordRpc.JoinRequest request)
        {

        }
    }
}
