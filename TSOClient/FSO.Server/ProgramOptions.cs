using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class ProgramOptions
    {
        [VerbOption("run", HelpText = "Run the servers configured in config.json")]
        public RunServerOptions RunServerVerb { get; set; }

        [VerbOption("db-init", HelpText = "Initialize the database.")]
        public DatabaseInitOptions DatabaseMaintenanceVerb { get; set; }

        [VerbOption("import-nhood",
            HelpText = "Import the neighborhood stored in the given JSON file to the specified shard.")]
        public ImportNhoodOptions ImportNhoodVerb { get; set; }

        [VerbOption("restore-lots",
            HelpText = "Create lots in the database from FSOV saves in the specified folder. (with specified shard)")]
        public RestoreLotsOptions RestoreLotsVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    public class DatabaseInitOptions
    {
    }
    

    public class RunServerOptions
    {
        [Option('d', "debug", DefaultValue = false, HelpText = "Launches a network debug interface")]
        public bool Debug { get; set; }
    }

    public class ImportNhoodOptions
    {
        [ValueOption(0)]
        public int ShardId { get; set; }
        [ValueOption(1)]
        public string JSON { get; set; }
    }

    public class RestoreLotsOptions
    {
        [ValueOption(0)]
        public int ShardId { get; set; }
        [ValueOption(1)]
        public string RestoreFolder { get; set; }

        [Option('r', "report", DefaultValue = false, HelpText = "Report changes that would be made restoring the lot, " +
            "eg. add/remove/reown of objects, lot positon (and if we can restore it) ")]
        public bool Report { get; set; }

        [Option('o', "objects", DefaultValue = false, HelpText = "Create new database entries for objects when they are still owned. " +
            "If 'safe' is enabled, then database entries will be created for objects on other lots, otherwise they will be created for all.")]
        public bool Objects { get; set; }

        [Option('s', "safe", DefaultValue = false, HelpText = "Do not return objects that have been placed, only ones in inventories.")]
        public bool Safe { get; set; }
    }
}
