namespace FSO.UpdateWorker
{
    internal class InstallerFile : IEquatable<InstallerFile>
    {
        public string url { get; set; } = "";
        public int size { get; set; }

        public bool Equals(InstallerFile? other)
        {
            return other != null &&
                url == other.url &&
                size == other.size;
        }

        public override bool Equals(object? obj)
        {
            return obj is InstallerFile file && Equals(file);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(url, size);
        }
    }

    internal class InstallerPlatform : IEquatable<InstallerPlatform>
    {
        public string name { get; set; } = "Unknown";

        public InstallerFile? installer { get; set; }
        public InstallerFile? zip { get; set; }
        public InstallerFile? server { get; set; }

        public bool Equals(InstallerPlatform? other)
        {
            return other != null &&
                name == other.name &&
                (installer == other.installer || (installer?.Equals(other.installer) ?? false)) &&
                (zip == other.zip || (zip?.Equals(other.zip) ?? false)) &&
                (server == other.server || (server?.Equals(other.server) ?? false));
        }

        public override bool Equals(object? obj)
        {
            return obj is InstallerPlatform platform && Equals(platform);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name, installer, server);
        }
    }

    internal class InstallerManifestChannel : IEquatable<InstallerManifestChannel>
    {
        public string channel { get; set; } = "";
        public string version { get; set; } = "";
        public string releaseUrl { get; set; } = "";
        public InstallerPlatform? windows { get; set; }
        public InstallerPlatform? linux { get; set; }
        public InstallerPlatform? mac { get; set; }

        public bool Equals(InstallerManifestChannel? other)
        {
            return other != null &&
                channel == other.channel &&
                version == other.version &&
                releaseUrl == other.releaseUrl &&
                (windows == other.windows || (windows?.Equals(other.windows) ?? false)) &&
                (mac == other.mac || (mac?.Equals(other.mac) ?? false)) &&
                (linux == other.linux || (linux?.Equals(other.linux) ?? false));
        }

        public override bool Equals(object? obj)
        {
            return obj is InstallerManifestChannel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(channel, version, releaseUrl, windows, linux, mac);
        }
    }

    internal class InstallerManifestResponse
    {
        public InstallerManifestChannel[] channels { get; set; } = [];
    }
}
