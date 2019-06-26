using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tasks;
using Newtonsoft.Json;
using Ninject;
using Ninject.Modules;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FSO.Server.Servers.Tasks
{
    public class TaskEngine
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IKernel Kernel;
        private IDAFactory DAFactory;

        private System.Timers.Timer Timer = new System.Timers.Timer(30000);
        private List<TaskEngineEntry> Entries;
        private List<ITask> Running;
        private DateTime Last;
        private List<ScheduledTaskRunOptions> _Schedule;

        public TaskEngine(IKernel kernel, IDAFactory daFactory)
        {
            Last = DateTime.Now;
            Kernel = kernel;
            DAFactory = daFactory;
            Entries = new List<TaskEngineEntry>();
            _Schedule = new List<ScheduledTaskRunOptions>();
            Timer.AutoReset = true;
            Timer.Elapsed += Timer_Elapsed;
            Running = new List<ITask>();
        }

        public void Start()
        {
            LOG.Info("starting task engine");
            Timer.Start();

            Task.Delay(30000).ContinueWith(x =>
            {
                RunMissedTasks();
            });
        }

        public void RunMissedTasks()
        {
            using (var db = DAFactory.Get())
            {
                foreach (var task in _Schedule)
                {
                    if (!task.Run_If_Missed) continue;

                    DbTaskType type;
                    if (!Enum.TryParse(task.Task, out type)) continue;

                    var rangeEnd = task.CronSchedule.GetLastValidTime();
                    var rangeStart = task.CronSchedule.GetStartOfRange(rangeEnd);

                    var lastRun = db.Tasks.CompletedAfter(type, rangeStart);

                    if (lastRun == null) Run(task);
                }
            }
        }

        public void Stop()
        {
            LOG.Info("stopping task engine");
            Timer.Stop();
            foreach(var task in Running){
                task.Abort();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var time = DateTime.Now;
            if(time.Minute == Last.Minute){
                return;
            }
            Last = time;

            foreach(var task in _Schedule)
            {
                if (task.CronSchedule.isTime(time)){
                    Run(task);
                }
            }
        }

        public void Schedule(ScheduledTaskRunOptions options)
        {
            options.CronSchedule = new CronSchedule(options.Cron);
            if (!options.CronSchedule.isValid()){
                throw new Exception("Invalid cron expression: " + options.Cron);
            }
            
           _Schedule.Add(options);
        }

        public int Run(TaskRunOptions options)
        {
            var name = options.Task;

            try
            {
                var entry = Entries.FirstOrDefault(x => x.Name == name);
                if(entry == null)
                {
                    LOG.Info("unknown task: " + name);
                    return -1;
                }

                LOG.Info("ready to start task: " + entry.Name);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.Timeout));

                ITask instance = (ITask)Kernel.Get(entry.Type);

                int taskId = 0;
                using (var db = DAFactory.Get())
                {
                    taskId = db.Tasks.Create(new DbTask {
                        task_type = instance.GetTaskType(),
                        task_status = DbTaskStatus.in_progress,
                        shard_id = options.Shard_Id
                    });
                }

                if (options.AllowTaskOverlap == false)
                {
                    //Can only have one task of this type running at once
                    foreach (var task in Running)
                    {
                        if (entry.Type.IsAssignableFrom(task.GetType()))
                        {
                            LOG.Warn("could not start task, previous task is still running");
                            using (var db = DAFactory.Get())
                            {
                                db.Tasks.CompleteTask(taskId, DbTaskStatus.failed);
                            }
                            return -2;
                        }
                    }
                }


                /*LoggingConfiguration config = new LoggingConfiguration();
                FileTarget fileTarget = new FileTarget();
                config.AddTarget("logfile", fileTarget);
                fileTarget.FileName = @"C:\Logfile\Log.txt";

                LoggingRule rule = new LoggingRule("*", LogLevel.Debug, fileTarget);
                config.LoggingRules.Add(rule);*/
                //NLog.LogManager.Configuration = config;

                //LogManager.GetLogger()

                var context = new TaskContext(this);
                context.ShardId = options.Shard_Id;
                context.ParameterJson = JsonConvert.SerializeObject(options.Parameter);
                Running.Add(instance);

                Task.Run(() =>
                {
                    LOG.Info(entry.Name + " task running");
                    instance.Run(context);
                }, cts.Token).ContinueWith(x =>
                {
                    Running.Remove(instance);
                    var endStatus = DbTaskStatus.failed;
                    if (x.IsFaulted)
                    {
                        LOG.Error(x.Exception, entry.Name + " task failed: "+x.Exception.ToString());
                    }
                    else
                    {
                        LOG.Info(entry.Name + " task complete");
                        endStatus = DbTaskStatus.completed;
                    }

                    using (var db = DAFactory.Get())
                    {
                        db.Tasks.CompleteTask(taskId, endStatus);
                    }
                });

                return taskId;
            }catch(Exception ex){
                LOG.Error(ex, "unknown error starting task " + name);
                return -1;
            }
        }

        public void AddTask(string name, Type type)
        {
            Entries.Add(new TaskEngineEntry {
                Name = name,
                Type = type
            });
        }
    }

    public class TaskEngineModule : NinjectModule
    {
        public override void Load()
        {
            Bind<TaskEngine>().ToSelf().InSingletonScope();
        }
    }

    public class TaskEngineEntry
    {
        public string Name;
        public Type Type;
    }

    public class TaskRunOptions
    {
        public string Task;
        public bool AllowTaskOverlap = false;
        public bool Run_If_Missed = false;
        public int Timeout = 3600; //1hr
        public int? Shard_Id;
        public dynamic Parameter;
    }

    public class ScheduledTaskRunOptions : TaskRunOptions
    {
        public string Cron;
        public CronSchedule CronSchedule;
    }

    public class TaskContext
    {
        private TaskEngine Engine;
        public int? ShardId;
        public string ParameterJson;

        public TaskContext(TaskEngine engine)
        {
        }

        public T GetParameter<T>()
        {
            return JsonConvert.DeserializeObject<T>(ParameterJson);
        }
    }

    public interface ITask
    {
        void Run(TaskContext context);
        void Abort();

        DbTaskType GetTaskType();
    }




    public interface ICronSchedule
    {
        bool isValid(string expression);
        bool isTime(DateTime date_time);
        DateTime GetLastValidTime();
        DateTime GetStartOfRange(DateTime result);
    }

    public class CronSchedule : ICronSchedule
    {
        #region Readonly Class Members

        readonly static Regex divided_regex = new Regex(@"(\*/\d +)");
        readonly static Regex range_regex = new Regex(@"(\d+\-\d+)\/?(\d+)?");
        readonly static Regex wild_regex = new Regex(@"(\*)");
        readonly static Regex list_regex = new Regex(@"(((\d+,)*\d+)+)");
        readonly static Regex validation_regex = new Regex(divided_regex + "|" + range_regex + "|" + wild_regex + "|" + list_regex);

        #endregion

        #region Private Instance Members

        private readonly string _expression;
        public List<int> minutes;
        public List<int> hours;
        public List<int> days_of_month;
        public List<int> months;
        public List<int> days_of_week;

        #endregion

        #region Public Constructors

        public CronSchedule()
        {
        }

        public CronSchedule(string expressions)
        {
            this._expression = expressions;
            generate();
        }

        #endregion

        #region Public Methods

        public bool isValid()
        {
            return isValid(this._expression);
        }

        public bool isValid(string expression)
        {
            MatchCollection matches = validation_regex.Matches(expression);
            return matches.Count > 0;//== 5;
        }

        public bool isTime(DateTime date_time)
        {
            return minutes.Contains(date_time.Minute) &&
                   hours.Contains(date_time.Hour) &&
                   days_of_month.Contains(date_time.Day) &&
                   months.Contains(date_time.Month) &&
                   days_of_week.Contains((int)date_time.DayOfWeek);
        }

        public DateTime GetLastValidTime()
        {
            //gets the last time this cron schedule item was valid.
            //this is at the *end* of the last valid range
            //used to determine if the last valid time fired an event.

            var result = DateTime.UtcNow;
            int lastChange = 0;
            //note that all of these changes can cascade to the last.

            while (!minutes.Contains(result.Minute))
            {
                result = result.AddMinutes(-1);
                lastChange = 1;
            }

            while (!hours.Contains(result.Hour))
            {
                result = result.AddHours(-1);
                lastChange = 2;
            }

            while (true)
            {
                while (!months.Contains(result.Month))
                {
                    result = result.AddMonths(-1);
                    lastChange = 4;
                }

                bool dirty = false;
                while (!(days_of_week.Contains((int)result.DayOfWeek) && days_of_month.Contains(result.Day)))
                {
                    result = result.AddDays(-1);
                    lastChange = 3;
                    dirty = true;
                }
                if (dirty) continue;

                break;
            }

            result = result.AddSeconds(-result.Second).AddMinutes(1);
            /*
            switch (lastChange)
            {
                case 1:
                    result = result.AddSeconds(-result.Second).AddMinutes(1);
                    break;
                case 2:
                    result = result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(1);
                    break;
                case 3:
                    result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(-result.Hour).AddDays(1);
                    break;
                case 4:
                    result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(-result.Hour).AddDays(-result.Day).AddMonths(1);
                    break;
            }*/

            return result;
        }

        public DateTime GetStartOfRange(DateTime result)
        {
            result = result.AddSeconds(-1);
            //find first time that is outwith the bounds of this cron schedule, from the given time.
            //rather inefficient, but this is not used often.

            if (minutes.Count != 60)
            {
                while (minutes.Contains(result.Minute))
                    result = result.AddMinutes(-1);

                result = result.AddSeconds(-result.Second).AddMinutes(1);
            }
            else if (hours.Count != 24)
            {
                while (hours.Contains(result.Hour))
                    result = result.AddHours(-1);

                result = result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(1);
            }
            else if (days_of_week.Count != 7 || days_of_month.Count != 32)
            {
                while (days_of_week.Contains((int)result.DayOfWeek) && days_of_month.Contains(result.Day))
                    result = result.AddDays(-1);

                result = result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(-result.Hour).AddDays(1);
            }
            else if (months.Count != 12 )
            {
                while (months.Contains(result.Month))
                    result = result.AddMonths(-1);

                result = result.AddSeconds(-result.Second).AddMinutes(-result.Minute).AddHours(-result.Hour).AddDays(-result.Day).AddMonths(1);
            }
            return result;
        }

        private void generate()
        {
            if (!isValid()) return;

            MatchCollection matches = validation_regex.Matches(this._expression);

            generate_minutes(matches[0].ToString());

            if (matches.Count > 1)
                generate_hours(matches[1].ToString());
            else
                generate_hours("*");

            if (matches.Count > 2)
                generate_days_of_month(matches[2].ToString());
            else
                generate_days_of_month("*");

            if (matches.Count > 3)
                generate_months(matches[3].ToString());
            else
                generate_months("*");

            if (matches.Count > 4)
                generate_days_of_weeks(matches[4].ToString());
            else
                generate_days_of_weeks("*");
        }

        private void generate_minutes(string match)
        {
            this.minutes = generate_values(match, 0, 60);
        }

        private void generate_hours(string match)
        {
            this.hours = generate_values(match, 0, 24);
        }

        private void generate_days_of_month(string match)
        {
            this.days_of_month = generate_values(match, 1, 32);
        }

        private void generate_months(string match)
        {
            this.months = generate_values(match, 1, 13);
        }

        private void generate_days_of_weeks(string match)
        {
            this.days_of_week = generate_values(match, 0, 7);
        }

        private List<int> generate_values(string configuration, int start, int max)
        {
            if (divided_regex.IsMatch(configuration)) return divided_array(configuration, start, max);
            if (range_regex.IsMatch(configuration)) return range_array(configuration);
            if (wild_regex.IsMatch(configuration)) return wild_array(configuration, start, max);
            if (list_regex.IsMatch(configuration)) return list_array(configuration);

            return new List<int>();
        }

        private List<int> divided_array(string configuration, int start, int max)
        {
            if (!divided_regex.IsMatch(configuration))
                return new List<int>();

            List<int> ret = new List<int>();
            string[] split = configuration.Split("/".ToCharArray());
            int divisor = int.Parse(split[1]);

            for (int i = start; i < max; ++i)
                if (i % divisor == 0)
                    ret.Add(i);

            return ret;
        }

        private List<int> range_array(string configuration)
        {
            if (!range_regex.IsMatch(configuration))
                return new List<int>();

            List<int> ret = new List<int>();
            string[] split = configuration.Split("-".ToCharArray());
            int start = int.Parse(split[0]);
            int end = 0;
            if (split[1].Contains("/"))
            {
                split = split[1].Split("/".ToCharArray());
                end = int.Parse(split[0]);
                int divisor = int.Parse(split[1]);

                for (int i = start; i < end; ++i)
                    if (i % divisor == 0)
                        ret.Add(i);
                return ret;
            }
            else
                end = int.Parse(split[1]);

            for (int i = start; i <= end; ++i)
                ret.Add(i);

            return ret;
        }

        private List<int> wild_array(string configuration, int start, int max)
        {
            if (!wild_regex.IsMatch(configuration))
                return new List<int>();

            List<int> ret = new List<int>();

            for (int i = start; i < max; ++i)
                ret.Add(i);

            return ret;
        }

        private List<int> list_array(string configuration)
        {
            if (!list_regex.IsMatch(configuration))
                return new List<int>();

            List<int> ret = new List<int>();

            string[] split = configuration.Split(",".ToCharArray());

            foreach (string s in split)
                ret.Add(int.Parse(s));

            return ret;
        }

        #endregion
    }
}
