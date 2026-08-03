namespace FSO.Files.FSO
{
    /// <summary>
    /// Update file.
    /// The SHA256 hash should be verified using the signature with the update channel's public key.
    /// If the hash and size don't match, assume the update is invalid or tampered with.
    /// The signature can be blank if the update channel doesn't have a key pair.
    /// </summary>
    public class FSOUpdateFile
    {
        public string zip { get; set; }
        public string hash { get; set; }
        public string signature { get; set; }
        public int size { get; set; }
    }

    /// <summary>
    /// Update files by supported platform.
    /// Unsupported platforms will have a null file.
    /// </summary>
    public class FSOUpdateCrossPlatformFile
    {
        public FSOUpdateFile windows { get; set; }
        public FSOUpdateFile linux { get; set; }
        public FSOUpdateFile mac { get; set; }

        public void SetPlatform(string target, FSOUpdateFile file)
        {
            switch (target)
            {
                case "windows": windows = file; break;
                case "linux": linux = file; break;
                case "mac": mac = file; break;
            }
        }

        public FSOUpdateFile CurrentPlatform()
        {
            if (OperatingSystem.IsMacOS())
            {
                return mac;
            }
            else if (OperatingSystem.IsWindows())
            {
                return windows;
            }
            else
            {
                return linux;
            }
        }
    }

    /// <summary>
    /// Metadata for an update within a channel.
    /// </summary>
    public class FSOUpdateMetadata
    {
        public string id { get; set; }
        public string lastid { get; set; } // (nullable)
        public uint date { get; set; }
        public FSOUpdateCrossPlatformFile server { get; set; }
        public FSOUpdateCrossPlatformFile full { get; set; }
        public FSOUpdateCrossPlatformFile delta { get; set; }
        public string changelog { get; set; }

        public FSOUpdateMetadata Clone()
        {
            return new FSOUpdateMetadata() {
                id = id,
                lastid = lastid,
                date = date,
                server = server,
                full = full,
                delta = delta,
                changelog = changelog
            };
        }
    }

    /// <summary>
    /// Metadata for an update that's by itself instead of part of a listing.
    /// The manifests directly in github have this format. (it's removed when building the full version listing)
    /// </summary>
    public class FSOUpdateMetadataStandalone : FSOUpdateMetadata
    {
        public string channel { get; set; }
        public string publicKey { get; set; }
    }

    /// <summary>
    /// Update channel, containing a list of updates for the channel from newest first.
    /// </summary>
    public class FSOUpdateChannel
    {
        public string channel { get; set; }
        public string publicKey { get; set; }
        public FSOUpdateMetadata[] updates { get; set; } = [];
    }

    /// <summary>
    /// Remesh file. 
    /// </summary>
    public class FSORemeshFile
    {
        public string url { get; set; }
        public string hash { get; set; }
        public string signature { get; set; }
        public int size { get; set; }
    }

    /// <summary>
    /// Remesh channel, containing the latest remesh package for the channel.
    /// The client will automatically download remesh updates from the active channel if the public key matches the client.
    /// </summary>
    public class FSORemeshChannel
    {
        public string channel { get; set; }
        public string publicKey { get; set; }
        public int version { get; set; }

        // Credits metadata
        public string name { get; set; }
        public string description { get; set; }
        public string url { get; set; }

        public FSORemeshFile dxt;
        public FSORemeshFile png;
    }

    /// <summary>
    /// Update API response containing multiple update channels.
    /// </summary>
    public class FSOUpdateResponse
    {
        public FSOUpdateChannel[] channels { get; set; } = [];
        public FSORemeshChannel[] remeshes { get; set; } = [];
        public string autoRemeshChannel { get; set; } = null;
    }
}
