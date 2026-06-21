using FSO.Common;

namespace FSO.Server.Embedded
{
    public static class ArchiveConfigExporter
    {
        private static string MakeRelativePath(string path)
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;

            return Path.GetRelativePath(root, path);
        }

        public static string BuildAndExport(ArchiveConfiguration config, bool archiveAbsolute, bool tsoAbsolute)
        {
            if (!archiveAbsolute)
            {
                config.ArchiveDataDirectory = MakeRelativePath(config.ArchiveDataDirectory);
            }
            else
            {
                config.ArchiveDataDirectory = Path.GetFullPath(config.ArchiveDataDirectory);
            }

            var serverConfig = ArchiveConfigBuilder.Build(config);

            if (!tsoAbsolute)
            {
                serverConfig.GameLocation = MakeRelativePath(serverConfig.GameLocation);
            }
            else
            {
                serverConfig.GameLocation = Path.GetFullPath(serverConfig.GameLocation);
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                serverConfig,
                Newtonsoft.Json.Formatting.Indented,
                new Newtonsoft.Json.JsonSerializerSettings() { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });

            return json;
        }
    }
}
