using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Common;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Framework.Aries;
using Ninject;
using FSO.Server.Servers.Tasks.Domain;
using NLog;
using FSO.Server.Servers.City.Handlers;
using FSO.Server.Servers.Shared.Handlers;
using FSO.Server.Servers.Tasks.Handlers;
using FSO.Server.Database.DA.Tasks;

namespace FSO.Server.Servers.Tasks
{
    public class TaskServer : AbstractAriesServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private TaskEngine Engine;
        private TaskServerConfiguration Config;

        public TaskServer(TaskServerConfiguration config, IKernel kernel, TaskEngine engine) : base(config, kernel)
        {
            Engine = engine;
            Config = config;

            Engine.AddTask(DbTaskType.prune_database.ToString(), typeof(PruneDatabaseTask));
            Engine.AddTask(DbTaskType.bonus.ToString(), typeof(BonusTask));
            Engine.AddTask(DbTaskType.shutdown.ToString(), typeof(ShutdownTask));
            Engine.AddTask(DbTaskType.job_balance.ToString(), typeof(JobBalanceTask));
            Engine.AddTask(DbTaskType.neighborhood_tick.ToString(), typeof(NeighborhoodsTask));
            Engine.AddTask(DbTaskType.birthday_gift.ToString(), typeof(BirthdayGiftTask));
        }

        public override void Start()
        {
            LOG.Info("starting task server");

            foreach(var task in Config.Schedule){
                Engine.Schedule(task);
            }

            Engine.Start();
            base.Start();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Engine.Stop();
        }

        public override Type[] GetHandlers(){
            return new Type[] {
                typeof(GluonAuthenticationHandler),
                typeof(TaskEngineHandler)
            };
        }

        protected override DbHost CreateHost(){
            var host = base.CreateHost();
            host.role = DbHostRole.task;
            return host;
        }

        protected override void HandleVoltronSessionResponse(IAriesSession session, object message){
        }
    }

    public class TaskServerConfiguration : AbstractAriesServerConfig
    {
        public bool Enabled { get; set; } = true;
        public List<ScheduledTaskRunOptions> Schedule;
        public TaskTuning Tuning { get; set; }
    }

    public class TaskTuning
    {
        public BonusTaskTuning Bonus { get; set; }
        public ShutdownTaskTuning Shutdown { get; set; }
        public JobBalanceTuning JobBalance { get; set; }
        public BirthdayGiftTaskTuning BirthdayGift { get; set; }
    }
}
