using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.DynPayouts;
using FSO.Server.Database.DA.Tuning;

namespace FSO.Server.Servers.Tasks.Domain
{
    public class JobBalanceTask : ITask
    {
        private IDAFactory DAFactory;
        private bool Running;
        private TaskTuning Tuning;

        private static Dictionary<int, int> TransactionToType = new Dictionary<int, int>()
        {
            { 41, 0 }, //typewriter
            {42, 1 }, //easel
            {44, 2 }, //boards
            {45, 3 }, //jams
            {46, 4 }, //potions
            {47, 5 }, //gnome
            {48, 6 }, //pinata
            {50, 7 } //telemarketing
        };
        private static Dictionary<int, int> TypeToTransaction = TransactionToType.ToDictionary(x => x.Value, x => x.Key);

        private float BaseCompletionTime = 270f;
        private static float[] TypeCompletionTimes = new float[]
        {
            292.21f, //typewriter
            289.23f, //easel
            207.73f, //boards
            299.1f, //jam
            324.41f, //potion
            290.47f, //gnome
            290.6f, //pinata
            288.13f //telemarketing
        };

        private static int[] ToTuningIndex = new int[] //see skillobjects.otf
        {
            6, //typewriter
            3, //easel
            1, //boards
            4, //jam
            2, //potion
            0, //gnome
            7, //pinata
            5 //telemarketing
        };

        public JobBalanceTask(IDAFactory DAFactory, TaskTuning tuning)
        {
            this.DAFactory = DAFactory;
            this.Tuning = tuning;
        }

        public void Abort()
        {

        }

        public DbTaskType GetTaskType()
        {
            return DbTaskType.job_balance;
        }

        public float PercentToBaseMultiplier(float pct)
        {
            float divider = 1f / TypeCompletionTimes.Length;
            if (pct < divider)
            {
                pct = (float)Math.Pow(pct / divider, 2.0) * 0.5f;
            }
            else
            {
                pct = 1.0f - (float)Math.Pow((1.0 - pct) / (1.0 - divider), 1.0 / divider) * 0.5f;
            }
            return pct;
        }

        public void Run(TaskContext context)
        {
            var tuning = Tuning.JobBalance;
            if (tuning == null)
            {
                tuning = new JobBalanceTuning();
            }

            //obtain aggregate for all skills
            using (var db = DAFactory.Get())
            {
                var days = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalDays;
                var limitdays = days - tuning.days_to_aggregate;
                var aggregate = db.DynPayouts.GetSummary(limitdays);

                var missing = TransactionToType.Keys.Where(x => !aggregate.Any(y => y.transaction_type == x));
                var baseSum = (aggregate.Count == 0) ? 1 : 0;
                foreach (var miss in missing)
                    aggregate.Add(new DbTransSummary() { transaction_type = miss, sum = baseSum });
                aggregate = aggregate.OrderBy(x => x.transaction_type).ToList();

                var totalPayouts = aggregate.Sum(x => x.sum);
                //get the percent of all single money payouts that each object gave out
                //since some jobs take longer to complete, their payout counts need to be rescaled, 
                //so that one payout counts as like, 1.1.
                int i = 0;
                var percentIncome = aggregate.Select(x => TypeCompletionTimes[(i++)]/BaseCompletionTime * ((float)x.sum)/totalPayouts);

                var rand = new Random();
                //convert this into our base multipler, with some variation
                var multipliers = percentIncome.Select(x => PercentToBaseMultiplier(x) + tuning.mul_variation * ((float)rand.NextDouble() - 0.5f)).ToList();

                //pick a job that recieves a notable boost. there is a chance that no job gets this boost.
                var bonusJob = rand.Next(TypeCompletionTimes.Length + 1);
                if (bonusJob < TypeCompletionTimes.Length)
                    multipliers[bonusJob] -= tuning.mul_bonus_job + tuning.mul_variation * (float)rand.NextDouble();

                //finally,  scale into the final range.
                var finalMultipliers = multipliers.Select(x => Math.Min(1f, Math.Max(0f, 1-x)) * (tuning.max_multiplier - tuning.min_multiplier) + tuning.min_multiplier);

                //ace af. lets put these into the database. log the raw multipliers first...
                var dbEntries = new List<DbDynPayout>();
                i = 0;
                foreach (var mul in finalMultipliers)
                {
                    dbEntries.Add(new DbDynPayout()
                    {
                        day = days,
                        multiplier = mul,
                        skilltype = i,
                        flags = (i == bonusJob) ? 1 : 0
                    });
                    i++;
                }
                db.DynPayouts.InsertDynRecord(dbEntries);

                //...then we calculate and replace the object tuning.
                var dbTuning = new List<DbTuning>();
                i = 0;
                foreach (var mul in finalMultipliers)
                {
                    //we need to scale the multiplier by the time it takes to get one payout.
                    //the longer it takes than the base, the more we pay out in one instance.
                    var timeScaledMulti = mul * (TypeCompletionTimes[i]/BaseCompletionTime);
                    dbTuning.Add(new DbTuning()
                    {
                        tuning_type = "skillobjects.iff",
                        tuning_table = 8207-8192,
                        tuning_index = ToTuningIndex[i],
                        value = (int)Math.Round((100 * timeScaledMulti) - 100),
                        owner_type = DbTuningType.DYNAMIC,
                        owner_id = 1
                    });
                    i++;
                }

                db.DynPayouts.ReplaceDynTuning(dbTuning);
            }
        }

    }

    public class JobBalanceTuning
    {
        public int days_to_aggregate { get; set; } = 3;
        public int days_to_prune { get; set; } = 7;

        public float min_multiplier { get; set; } = 0.5f;
        public float max_multiplier { get; set; } = 1.5f;

        public float mul_variation { get; set; } = 0.1f; //this is a 10% variation. 5% in positive and negative direction!
        public float mul_bonus_job { get; set; } = 0.15f; //15% bonus, plus potential variation above (unsigned, so potentially 25%).
    }
}
