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
}
