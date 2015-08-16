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
}
