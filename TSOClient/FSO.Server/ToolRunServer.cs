using FSO.Common.DataService.Framework;
using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.DataService;
using FSO.Server.Domain;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Servers;
using FSO.Server.Servers.City;
using FSO.Server.Servers.Lot;
using FSO.Server.Servers.Tasks;
using FSO.Server.Servers.UserApi;
using FSO.Server.Utils;
using FSO.SimAntics;
using Ninject;
using Ninject.Extensions.ChildKernel;
using Ninject.Parameters;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class ToolRunServer : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ServerConfiguration Config;
        private IKernel Kernel;

        private bool Running;
        private List<AbstractServer> Servers;
        private List<CityServer> CityServers;
        private UserApi ActiveUApiServer;
        private TaskServer ActiveTaskServer;
        private RunServerOptions Options;
        private ShutdownType ShutdownMode;

        private IGluonHostPool HostPool;

        public ToolRunServer(RunServerOptions options, ServerConfiguration config, IKernel kernel, IGluonHostPool hostPool)
        {
            this.Options = options;
            this.Config = config;
            this.Kernel = kernel;
            this.HostPool = hostPool;
        }

        public int RunEmbedded(Action<Action> onStarted)
        {
            LOG.Info("Starting embedded server");

            if (Config.Services == null)
            {
                LOG.Warn("No services found in the configuration file, exiting");
                return 1;
            }

            Directory.CreateDirectory(Config.SimNFS);
            Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Lots/"));
            Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Objects/"));

            if (Config.Archive == null)
            {
                throw new Exception("Can only run archive server embedded. Check configuration.");
            }

            Content.Content.Get().Upgrades.LoadJSONTuning();

            CommonInit();

            LOG.Info("Starting services");
            foreach (AbstractServer server in Servers)
            {
                LOG.Info("Starting " + server.GetType().ToString() + "...");
                server.Start();
            }

            HostPool.Start();

            onStarted(() =>
            {
                RequestedShutdown(0, ShutdownType.SHUTDOWN);
            });

            //Hacky reference to maek sure the assembly is included
            FSO.Common.DatabaseService.Model.LoadAvatarByIDRequest x;

            {
                while (Running)
                {
                    Thread.Sleep(50);
                    lock (Servers)
                    {
                        if (Servers.Count == 0)
                        {
                            LOG.Info("All servers shut down, shutting down pool...");

                            Kernel.Get<IGluonHostPool>().Stop();

                            return 2;
                        }
                    }
                }
            }

            return 1;
        }

        private void CommonInit()
        {
            Kernel.Bind<Content.Content>().ToConstant(Content.Content.Get());
            Kernel.Bind<MemoryCache>().ToConstant(new MemoryCache("fso_server"));

            LOG.Info("Loading domain logic");
            Kernel.Load<ServerDomainModule>();

            Servers = new List<AbstractServer>();
            CityServers = new List<CityServer>();
            Kernel.Bind<IServerNFSProvider>().ToConstant(new ServerNFSProvider(Config.SimNFS));

            if (Config.Services.UserApi != null &&
                Config.Services.UserApi.Enabled)
            {
                if (Config.Archive == null)
                {
                    var childKernel = new ChildKernel(
                        Kernel
                    );
                    var api = new UserApi(Config, childKernel);
                    ActiveUApiServer = api;
                    Servers.Add(api);
                    api.OnRequestShutdown += RequestedShutdown;
                    api.OnBroadcastMessage += BroadcastMessage;
                    api.OnRequestUserDisconnect += RequestedUserDisconnect;
                    api.OnRequestMailNotify += RequestedMailNotify;
                }
                else
                {
                    LOG.Info("Skipping User API for Archive Server (shouldn't be in the config...)");
                }
            }

            foreach (var cityServer in Config.Services.Cities)
            {
                if (cityServer.Archive == null) cityServer.Archive = Config.Archive;

                /**
                 * Need to create a kernel for each city server as there is some data they do not share
                 */
                var childKernel = new ChildKernel(
                    Kernel,
                    new ShardDataServiceModule(Config.SimNFS),
                    new CityServerModule()
                );

                var city = childKernel.Get<CityServer>(new ConstructorArgument("config", cityServer));
                CityServers.Add(city);
                Servers.Add(city);
            }

            foreach (var lotServer in Config.Services.Lots)
            {
                if (lotServer.SimNFS == null) lotServer.SimNFS = Config.SimNFS;
                var childKernel = new ChildKernel(
                    Kernel,
                    new LotServerModule()
                );

                Servers.Add(
                    childKernel.Get<LotServer>(new ConstructorArgument("config", lotServer))
                );
            }

            if (Config.Services.Tasks != null
                && Config.Services.Tasks.Enabled)
            {
                var childKernel = new ChildKernel(
                    Kernel,
                    new TaskEngineModule()
                );

                childKernel.Bind<TaskServerConfiguration>().ToConstant(Config.Services.Tasks);
                childKernel.Bind<TaskTuning>().ToConstant(Config.Services.Tasks.Tuning);

                var tasks = childKernel.Get<TaskServer>(new ConstructorArgument("config", Config.Services.Tasks));
                Servers.Add(tasks);
                ActiveTaskServer = tasks;
                Server.Servers.Tasks.Domain.ShutdownTask.ShutdownHook = RequestedShutdown;
            }

            foreach (var server in Servers)
            {
                server.OnInternalShutdown += ServerInternalShutdown;
            }

            Running = true;
        }

        public int Run()
        {
            LOG.Info("Starting server");
            TimedReferenceController.SetMode(CacheType.PERMANENT);

            if (Config.Services == null)
            {
                LOG.Warn("No services found in the configuration file, exiting");
                return 1;
            }

            if (!Directory.Exists(Config.GameLocation))
            {
                LOG.Fatal("The directory specified as gameLocation in config.json does not exist");
                return 1;
            }

            Directory.CreateDirectory(Config.SimNFS);
            Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Lots/"));
            Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Objects/"));

            if (Content.Model.AbstractTextureRef.ImageFetchFunction == null)
                Content.Model.AbstractTextureRef.ImageFetchFunction = Utils.CoreImageLoader.SoftImageFetch;

            LOG.Info("Checking for scheduled updates...");
            if (AutoUpdateUtility.QueueUpdateIfRequired(Kernel, Config.UpdateBranch))
            {
                //update queued, restart
                LOG.Info("An update was scheduled, and has been queued for the watchdog to apply. Restarting...");
                return 4;
            }

            //get server update ID if present in a file (from auto updater)
            if (File.Exists("updateID.txt"))
            {
                var stringID = File.ReadAllText("updateID.txt");
                int id;
                if (int.TryParse(stringID, out id)) {
                    Config.UpdateID = id;
                }
            }

            if (Config.Archive != null)
            {
                LOG.Info("=== RUNNING IN ARCHIVE MODE! Only archive authentication will work! ===");
            }

            //TODO: Some content preloading
            LOG.Info("Scanning content");
            VMContext.InitVMConfig(false);
            Content.Content.Init(Config.GameLocation, Content.ContentMode.SERVER);
            CommonInit();

            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            /*
            NetworkDebugger debugInterface = null;

            if (Options.Debug)
            {
                debugInterface = new NetworkDebugger(Kernel);
                foreach (AbstractServer server in Servers)
                {
                    server.AttachDebugger(debugInterface);
                }
            }
            */

            LOG.Info("Starting services");
            foreach (AbstractServer server in Servers)
            {
                LOG.Info("Starting " + server.GetType().ToString() + "...");
                server.Start();
            }

            HostPool.Start();

            //Hacky reference to maek sure the assembly is included
            FSO.Common.DatabaseService.Model.LoadAvatarByIDRequest x;

            /*if (debugInterface != null)
            {
                Application.EnableVisualStyles();
                Application.Run(debugInterface);
            }
            else*/
            {
                while (Running)
                {
                    Thread.Sleep(1000);
                    lock (Servers)
                    {
                        if (Servers.Count == 0)
                        {
                            LOG.Info("All servers shut down, shutting down program...");

                            Kernel.Get<IGluonHostPool>().Stop();

                            LOG.Info("(pre-close) checking for scheduled updates...");
                            if (AutoUpdateUtility.QueueUpdateIfRequired(Kernel, Config.UpdateBranch))
                            {
                                LOG.Info("An update was scheduled, and has been queued for the watchdog to apply on restart.");
                                return 4;
                            }

                            /*var domain = AppDomain.CreateDomain("RebootApp");

                            var assembly = "FSO.Server.Updater, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                            var type = "FSO.Server.Updater.Program";

                            var updater = typeof(FSO.Server.Updater.Program);

                            domain.CreateInstance(assembly, type);
                            AppDomain.Unload(AppDomain.CurrentDomain);*/
                            return 2 + (int)ShutdownMode;
                        }
                    }
                }
            }
            return 1;
        }

        private int[] ShutdownAlertTimings = new int[]
        {
            30*60, //30 mins
            15*60, //15 mins
            10*60,
            5*60,
            4*60,
            3*60,
            2*60,
            60,
            30
        };

        private void BroadcastMessage(string sender, string title, string message)
        {
            //TODO: select which shards to operate on
            try
            {
                foreach (var city in CityServers)
                {
                    city.Sessions.Broadcast(new Protocol.Voltron.Packets.AnnouncementMsgPDU()
                    {
                        SenderID = "??" + sender,
                        Message = "\r\n" + message,
                        Subject = title
                    });
                }
            }
            catch (Exception)
            {
                //don't fail if this somehow screws up
            }
        }

        /// <summary>
        /// Disconnects a user ingame.
        /// </summary>
        /// <param name="user_id">ID of user to be disconnected.</param>
        private void RequestedUserDisconnect(uint user_id)
        {
            //TODO: select shard to send disconnection request
            foreach (var city in CityServers)
            {
                var session = city.Sessions.GetByAvatarId(user_id);
                session?.Close();
            }
        }

        /// <summary>
        /// Tries to notify an ingame player of a new email.
        /// </summary>
        /// <param name="message_id">The email message id from db insertion.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body.</param>
        /// <param name="target_id">The recipient's user_id.</param>
        private void RequestedMailNotify(int message_id, string subject, string body, uint target_id)
        {
            var messageItem = new Files.Formats.tsodata.MessageItem()
            {
                ID = message_id,
                SenderID = 2147483648,
                TargetID = target_id,
                Subject = subject,
                Body = body,
                SenderName = "FreeSO Staff",
                Time = DateTime.UtcNow.Ticks,
                Type = 4,
                Subtype = 0,
                ReadState = 0,
                ReplyID = 0
            };

            //TODO: select shard to send mail
            foreach (var city in CityServers)
            {
                var session = city.Sessions.GetByAvatarId(target_id);

                if (session != null)
                {
                    try
                    {
                        session.Write(new MailResponse
                        {
                            Type = MailResponseType.NEW_MAIL,
                            Messages = new Files.Formats.tsodata.MessageItem[] { messageItem }
                        });
                    }
                    catch { }
                }
            }
        }

        private async void RequestedShutdown(uint time, ShutdownType type)
        {
            //TODO: select which shards to operate on
            ShutdownMode = type;
            LOG.Info("Shutdown requested in " + time + " seconds.");

            var remaining = (int)time;
            foreach (var alertTime in ShutdownAlertTimings)
            {
                if (remaining < alertTime) continue;
                //wait until this alert time and display an announcement
                var waitTime = remaining - alertTime;
                await Task.Delay((int)waitTime * 1000);
                remaining -= waitTime;

                string timeString = (remaining % 60 == 0 && remaining > 60) ? ((remaining / 60) + " minutes") : (remaining + " seconds");
                LOG.Info("Shutdown in " + timeString);
                BroadcastMessage("FreeSO Server", "Shutting down", "The game server will go down for maintenance in " + timeString + ".");
            }

            await Task.Delay((int)remaining * 1000);

            LOG.Info("Shutdown commencing.");
            List<Task<bool>> ShutdownTasks = new List<Task<bool>>();
            foreach (var city in CityServers)
            {
                ShutdownTasks.Add(city.Shutdown(type));
            }
            await Task.WhenAll(ShutdownTasks.ToArray());
            LOG.Info("Successfully shut down all city servers!");
            lock (Servers)
            {
                if (ActiveUApiServer != null)
                {
                    ActiveUApiServer.Shutdown();
                    Servers.Remove(ActiveUApiServer);
                }
                if (ActiveTaskServer != null)
                {
                    ActiveTaskServer.Shutdown();
                    Servers.Remove(ActiveTaskServer);
                }
            }
        }

        private void ServerInternalShutdown(AbstractServer server, ShutdownType data)
        {
            lock (Servers)
            {
                Servers.Remove(server);
            }
            ShutdownMode = data;
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            CurrentDomain_ProcessExit(sender, e);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            HostPool.Stop();

            lock (Servers)
            {
                foreach (AbstractServer server in Servers)
                {
                    server.Shutdown();
                }
            }

            Running = false;
        }
    }
}
