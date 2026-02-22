using CommandLine;
using CommandLine.Text;

namespace FSO.Server
{
    public class ProgramOptions
    {
        public RunServerOptions RunServerVerb { get; set; }

        public DatabaseInitOptions DatabaseMaintenanceVerb { get; set; }

        public ImportNhoodOptions ImportNhoodVerb { get; set; }

        public RestoreLotsOptions RestoreLotsVerb { get; set; }

        public SqliteImportOptions SqliteImportVerb { get; set; }

        public DataTrimOptions DataTrimVerb { get; set; }

        public DataTrimOptions ArchiveConvertVerb { get; set; }

        public ImportArchiveFeaturedOptions ImportArchiveFeaturedVerb { get; set; }
    }

    [Verb("db-init", HelpText = "Initialize the database.")]
    public class DatabaseInitOptions
    {
    }

    [Verb("sqlite-import", HelpText = "Imports a MariaDB export from a given directory into an sqlite database.")]
    public class SqliteImportOptions
    {
        [Value(0)]
        public string ImportDir { get; set; }
    }

    [Verb("run", HelpText = "Run the servers configured in config.json")]
    public class RunServerOptions
    {
        [Option('d', "debug", Default = false, HelpText = "Launches a network debug interface")]
        public bool Debug { get; set; }
    }

    [Verb("data-trim", HelpText = "Remove unimportant data, and optionally sensitive information from the database and NFS.")]
    public class DataTrimOptions
    {
        [Option('a', "anon", Default = false, HelpText = "Strips any private information from the database and NFS. Does leave users intact - convert to archive to remove them.")]
        public bool Anon { get; set; }
    }

    [Verb("archive-convert", HelpText = "Convert the database for use as an archive server.")]
    public class ArchiveConvertOptions
    {
    }

    [Verb("import-archive-featured", HelpText = "Import the featured lots in the given JSON file to the specified shard.")]
    public class ImportArchiveFeaturedOptions
    {
        [Value(0)]
        public int ShardId { get; set; }
        [Value(1)]
        public string JSON { get; set; }
    }

    [Verb("import-nhood", HelpText = "Import the neighborhood stored in the given JSON file to the specified shard.")]
    public class ImportNhoodOptions
    {
        [Value(0)]
        public int ShardId { get; set; }
        [Value(1)]
        public string JSON { get; set; }
    }

    [Verb("restore-lots", HelpText = "Create lots in the database from FSOV saves in the specified folder. (with specified shard)")]
    public class RestoreLotsOptions
    {
        [Value(0)]
        public int ShardId { get; set; }
        [Value(1)]
        public string RestoreFolder { get; set; }

        [Option('l', "location", Default = 0u, HelpText = "Override location to place the property.")]
        public uint Location { get; set; }

        [Option('t', "owner", Default = 0u, HelpText = "Override avatar id to own the property.")]
        public uint Owner { get; set; }

        [Option('c', "category", Default = -1, HelpText = "Override property category.")]
        public int Category { get; set; }

        [Option('r', "report", Default = false, HelpText = "Report changes that would be made restoring the lot, " +
            "eg. add/remove/reown of objects, lot positon (and if we can restore it) ")]
        public bool Report { get; set; }

        [Option('o', "objects", Default = false, HelpText = "Create new database entries for objects when they are still owned. " +
            "If 'safe' is enabled, then database entries will be created for objects on other lots, otherwise they will be created for all.")]
        public bool Objects { get; set; }

        [Option('s', "safe", Default = false, HelpText = "Do not return objects that have been placed, only ones in inventories.")]
        public bool Safe { get; set; }

        [Option('d', "donate", Default = false, HelpText = "Convert all objects to donated so they don't have to belong to roommates.")]
        public bool Donate { get; set; }
    }

    [Verb("plugin-anonymize", HelpText = "Tool for reviewing and stripping potentially sensitive plugin data")]
    public class PluginAnonymizeOptions
    {
        [Value(0, Required = false)]
        public string InputFile { get; set; }
    }

    [Verb("backup-selection", HelpText = "Tool for selecting backups with the least number of evicted roommates for more complete historical lot data")]
    public class BackupSelectionOptions
    {
        [Option('v', "validate", Default = false, HelpText = "Print information about backup selection without actually doing it.")]
        public bool DryRun { get; set; }
    }
}
