using System;
using System.Collections.Generic;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Framework.Aries;
using Ninject;
using FSO.Server.Servers.Tasks.Domain;
using NLog;
using FSO.Server.Servers.Shared.Handlers;
using FSO.Server.Servers.Tasks.Handlers;
using FSO.Server.Database.DA.Tasks;
using Newtonsoft.Json;

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
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
        [JsonProperty("schedule")]
        public List<ScheduledTaskRunOptions> Schedule;
        [JsonProperty("tuning")]
        public TaskTuning Tuning { get; set; }
    }

    // Note: the tuning config types use the json casing so don't need the property attributes.
    public class TaskTuning
    {
        [JsonProperty("bonus")]
        public BonusTaskTuning Bonus { get; set; }
        [JsonProperty("shutdown")]
        public ShutdownTaskTuning Shutdown { get; set; }
        [JsonProperty("jobBalance")]
        public JobBalanceTuning JobBalance { get; set; }
        [JsonProperty("birthdayGift")]
        public BirthdayGiftTaskTuning BirthdayGift { get; set; }
    }
}
