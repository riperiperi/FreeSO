using System;

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
        public string shard_name { get; set; }
    }

    public enum DbTaskType
    {
        prune_database,
        bonus,
        shutdown,
        job_balance,
        multi_check,
        prune_abandoned_lots,
        neighborhood_tick,
        birthday_gift
    }

    public enum DbTaskStatus
    {
        in_progress,
        completed,
        failed
    }
}
