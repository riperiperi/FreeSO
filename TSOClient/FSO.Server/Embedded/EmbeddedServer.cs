using FSO.Common;
using FSO.Server.Database;
using FSO.Server.DataService;
using FSO.Server.Utils;
using Ninject;
using Ninject.Parameters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Embedded
{
    public class EmbeddedServer
    {
        public bool Ready { get; private set; }
        public float ReadyPercent { get; private set; }
        public Exception Error { get; private set; }

        private Thread ServerThread;
        private Action ShutdownAction;

        public ArchiveConfiguration Config { get; }

        public EmbeddedServer(ArchiveConfiguration config)
        {
            Config = config;
        }

        public void Start()
        {

            ServerThread = new Thread(() =>
            {
                var config = ArchiveConfigBuilder.Build(Config);

                var kernel = new StandardKernel(
                    new ServerConfigurationModule(config),
                    new DatabaseModule(),
                    new GlobalDataServiceModule(),
                    new GluonHostPoolModule()
                );

                var tool = kernel.Get<ToolRunServer>(new ConstructorArgument("options", new RunServerOptions()));

                tool.RunEmbedded(
                    (Action shutdown) =>
                    {
                        ShutdownAction = shutdown;
                        Ready = true;
                    },
                    (float progress) =>
                    {
                        ReadyPercent = progress;
                    },
                    (Exception error) =>
                    {
                        Error = error;
                    }
                );
            });

            ServerThread.Start();
        }

        private void DisposeResources()
        {
            if (Config.Disposables != null)
            {
                foreach (var item in Config.Disposables)
                {
                    item.Dispose();
                }
            }
        }

        public Task<bool> Shutdown()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    if (ShutdownAction == null)
                    {
                        if (ServerThread.Join(10))
                        {
                            DisposeResources();
                            return true;
                        }
                    }
                    else
                    {
                        ShutdownAction();
                        ShutdownAction = null;
                        ServerThread.Join();
                        DisposeResources();
                        return true;
                    }
                }
            });
        }
    }
}
