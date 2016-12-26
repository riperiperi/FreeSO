using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Tasks
{
    public class DbTask
    {
        public int task_id { get; set; }
        public DbTaskType task_type { get; set; }
        public DbTaskStatus task_status { get; set; }
        public DateTime time_created { get; set; }
        public DateTime? time_completed { get; set; }
        public int? shard_id { get; set; }
    }

    public enum DbTaskType
    {
        prune_database,
        top100
    }

    public enum DbTaskStatus
    {
        in_progress,
        completed,
        failed
    }
}
