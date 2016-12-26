﻿using System;
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

        public TaskServer(TaskServerConfiguration config, IKernel kernel, TaskEngine engine) : base(config, kernel)
        {
            Engine = engine;

            Engine.AddTask(DbTaskType.prune_database.ToString(), typeof(PruneDatabaseTask), new TaskOptions {
                AllowTaskOverlap = false,
                Timeout = (int)TimeSpan.FromHours(1).TotalSeconds
            });
        }

        public override void Start()
        {
            LOG.Info("starting task server");
            base.Start();
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
    }
}