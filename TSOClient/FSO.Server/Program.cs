using FSO.Server.Database;
using Ninject;
using Ninject.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Type toolType = null;
            object toolOptions = null;

            var options = new ProgramOptions();
            var switchIsValid = new CommandLine.Parser().ParseArguments(args, options,
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
                new Nancy.Bootstrappers.Ninject.FactoryModule()
            );

            var tool = (ITool)kernel.Get(toolType, new ConstructorArgument("options", toolOptions));
            tool.Run();
        }
    }
}
