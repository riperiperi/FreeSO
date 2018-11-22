using FSO.Server.Database;
using FSO.Server.DataService;
using FSO.Server.Utils;
using Ninject;
using Ninject.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Type toolType = null;
            object toolOptions = null;

            string[] a2 = args;
            if (args.Length == 0) a2 = new string[] { "run" };

            var options = new ProgramOptions();
            var switchIsValid = new CommandLine.Parser().ParseArguments(a2, options,
                (verb, subOptions) =>
                {
                    switch (verb)
                    {
                        case "run":
                            toolType = typeof(ToolRunServer);
                            toolOptions = subOptions;
                            break;
                        case "db-init":
                            toolType = typeof(ToolInitDatabase);
                            toolOptions = subOptions;
                            break;
                        case "import-nhood":
                            toolType = typeof(ToolImportNhood);
                            toolOptions = subOptions;
                            break;
                        default:
                            Console.Write(options.GetUsage(verb));
                            break;
                    }
                }
            );

            if (!switchIsValid || toolType == null)
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var kernel = new StandardKernel(
                new ServerConfigurationModule(),
                new DatabaseModule(),
                new GlobalDataServiceModule(),
                new GluonHostPoolModule()
            );

            //If db init, allow @ variables in the query itself. We could always enable this but for added security
            //we are conditionally adding it only for db migrations
            if (toolType == typeof(ToolInitDatabase))
            {
                var config = kernel.Get<ServerConfiguration>();
                if (!config.Database.ConnectionString.EndsWith(";")){
                    config.Database.ConnectionString += ";";
                }
                config.Database.ConnectionString += "Allow User Variables=True";
            }

            var tool = (ITool)kernel.Get(toolType, new ConstructorArgument("options", toolOptions));
            return tool.Run();

        }
    }
}
