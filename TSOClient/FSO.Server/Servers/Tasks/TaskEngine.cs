using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tasks;
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

        public TaskEngine(IKernel kernel, IDAFactory daFactory)
        {
            Last = DateTime.Now;
            Kernel = kernel;
            DAFactory = daFactory;
            Entries = new List<TaskEngineEntry>();
            Timer.AutoReset = true;
            Timer.Elapsed += Timer_Elapsed;
            Running = new List<ITask>();
        }

        public void Start()
        {
            LOG.Info("starting task engine");
            Timer.Start();
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

            foreach(var task in Entries)
            {
                /*if (task.Schedule.isTime(time)){
                    Run(task);
                }*/
            }
        }
        
        public int Run(string name)
        {
            try
            {
                var entry = Entries.FirstOrDefault(x => x.Name == name);
                if(entry == null)
                {
                    LOG.Info("unknown task: " + name);
                    return -1;
                }

                LOG.Info("ready to start task: " + entry.Name);

                if(entry.Options.AllowTaskOverlap == false)
                {
                    //Can only have one task of this type running at once
                    foreach(var task in Running){
                        if (entry.Type.IsAssignableFrom(task.GetType())){
                            LOG.Warn("could not start task, previous task is still running");
                            return -2;
                        }
                    }
                }

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(entry.Options.Timeout));

                ITask instance = null;
                if(entry.Options.CustomKernel != null){
                    instance = (ITask)entry.Options.CustomKernel.Get(entry.Type);
                }
                else{
                    instance = (ITask)Kernel.Get(entry.Type);
                }


                int taskId = 0;
                using (var db = DAFactory.Get())
                {
                    taskId = db.Tasks.Create(new DbTask {
                        task_type = instance.GetTaskType(),
                        task_status = DbTaskStatus.in_progress
                    });
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
                context.Data = entry.Options.Data;
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
                        LOG.Error(x.Exception, entry.Name + " task failed");
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

        public void AddTask(string name, Type type, TaskOptions options)
        {
            /*var schedule = new CronSchedule(cron);
            if (!schedule.isValid()){
                throw new Exception("Invalid cron expression: " + cron);
            }*/

            /** public CronSchedule Schedule; **/

            Entries.Add(new TaskEngineEntry {
                Name = name,
                Type = type,
                Options = options
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
        public TaskOptions Options;
    }

    public class TaskOptions
    {
        public bool AllowTaskOverlap = false;
        public int Timeout = 3600; //1hr
        public object Data;
        public IKernel CustomKernel;
    }

    public class TaskContext
    {
        private TaskEngine Engine;
        public object Data;

        public TaskContext(TaskEngine engine)
        {
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
