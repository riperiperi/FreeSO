using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Tasks.Handlers
{
    public class TaskEngineHandler
    {
        private TaskEngine TaskEngine;

        public TaskEngineHandler(TaskEngine engine)
        {
            this.TaskEngine = engine;
        }

        public void Handle(IGluonSession session, RequestTask task)
        {
            var shardId = new Nullable<int>();
            if(task.ShardId > 0){
                shardId = task.ShardId;
            }

            var id = TaskEngine.Run(task.TaskType, shardId, task.ParameterJson);
            session.Write(new RequestTaskResponse() {
                CallId = task.CallId,
                TaskId = id
            });
        }
    }
}
