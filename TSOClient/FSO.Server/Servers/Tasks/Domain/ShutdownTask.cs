using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Database.DA.Tasks;

namespace FSO.Server.Servers.Tasks.Domain
{
    public class ShutdownTask : ITask
    {
        public static Action<uint, Protocol.Gluon.Model.ShutdownType> ShutdownHook;
        public static TaskTuning Tuning;

        public ShutdownTask(TaskTuning tuning)
        {
            Tuning = tuning;
        }

        public void Abort()
        {
        }

        public DbTaskType GetTaskType()
        {
            return DbTaskType.shutdown;
        }

        public void Run(TaskContext context)
        {
            var sdTune = Tuning.Shutdown;
            if (sdTune == null) sdTune = new ShutdownTaskTuning();
            if (ShutdownHook != null) ShutdownHook(sdTune.warning_period, Protocol.Gluon.Model.ShutdownType.RESTART);
        }
    }

    public class ShutdownTaskTuning
    {
        public uint warning_period = 60*15;
    }
}
