using Newtonsoft.Json;

namespace FSO.Common
{
    /// <summary>
    /// Version info for a FreeSO client or server.
    /// Should be saved as `version.json` next to the binary.
    /// When acting as a server, sent to connecting clients so they can ensure the same version.
    /// </summary>
    public class FSOVersionInfo : IEquatable<FSOVersionInfo>
    {
        /// <summary>
        /// Public key used for official FreeSO client updates.
        /// This changes the update warnings a little when transitioning from FreeSO update to another source.
        /// </summary>
        public static string FreeSOPublicKey = "-----BEGIN RSA PUBLIC KEY-----^MIIBCgKCAQEAukMS/klrVM7N7hjcfrWbgK7UjIU352RWkcAYRv5Uh7pt6Gd4U6Ng^9J8OoNCuU1aZfFEauCQvkX4i53KGWEKpBoBA6e3zFIGhyZQeq\u002BytxegDx/iMgRCi^U\u002BaKH1\u002BdxbmL/FU10eX2JNErhcJvQ/tcWttc\u002BJdWQlKM\u002BPBBR5PmgUrdcPBvhled^CMg9W\u002BRSGoAgqozeaspYPFJG3FoZDCqp16WZ7oFWAGsSKq2ovy2wPAMFqMrcGlas^pcWWXbVv\u002BgUefhWPjWZFAZObX77FCVdraHJlKnu5o2UX9XjNXkM6SDTeuDCHjap/^N/E2uWL5soHCtyUB9cqUt3penJ4mov\u002BeQQIDAQAB^-----END RSA PUBLIC KEY-----";

        private static FSOVersionInfo _current;
        public static FSOVersionInfo Current
        {
            get
            {
                if (_current == null)
                {
                    _current = GetCurrent();
                }

                return _current;
            }
        }

        private static FSOVersionInfo GetCurrent()
        {
            try
            {
                if (File.Exists("version.json"))
                {
                    using StreamReader reader = new StreamReader(File.Open("version.json", FileMode.Open, FileAccess.Read, FileShare.Read));

                    var result = JsonConvert.DeserializeObject<FSOVersionInfo>(reader.ReadToEnd());

                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (Exception)
            {

            }

            return new FSOVersionInfo()
            {
                id = "dev",
                channel = "FreeSO Development Build",
                channelUrl = "",
                publicKey = ""
            };
        }

        public string id { get; set; } = "unknown";
        public string channel { get; set; } = "invalid";
        public string channelUrl { get; set; } = "";
        public string publicKey { get; set; } = "";

        public static FSOVersionInfo FromJson(string json)
        {
            var result = JsonConvert.DeserializeObject<FSOVersionInfo>(json);

            return result ?? new FSOVersionInfo();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public FSOVersionInfo Clone()
        {
            return new FSOVersionInfo()
            {
                id = id,
                channel = channel,
                channelUrl = channelUrl,
                publicKey = publicKey,
            };
        }

        public bool Equals(FSOVersionInfo other)
        {
            return id == other.id && channel == other.channel && channelUrl == other.channelUrl && publicKey == other.publicKey;
        }

        public override bool Equals(object obj)
        {
            return obj is FSOVersionInfo info && Equals(info);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, channel, channelUrl, publicKey);
        }
    }
}
