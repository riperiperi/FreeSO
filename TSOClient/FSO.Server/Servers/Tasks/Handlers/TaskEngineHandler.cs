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
            var id = TaskEngine.Run(task.TaskType);
            session.Write(new RequestTaskResponse() {
                CallId = task.CallId,
                TaskId = id
            });
        }
    }
}
