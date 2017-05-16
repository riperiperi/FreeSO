using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Bonus;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotTop100;
using FSO.Server.Database.DA.LotVisitTotals;
using FSO.Server.Database.DA.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Tasks.Domain
{
    /// <summary>
    /// Calculate top 100
    /// </summary>
    public class BonusTask : ITask
    {
        private IDAFactory DAFactory;
        private bool Running;
        private TaskTuning Tuning;

        public BonusTask(IDAFactory DAFactory, TaskTuning tuning)
        {
            this.DAFactory = DAFactory;
            this.Tuning = tuning;
        }

        public void Run(TaskContext context)
        {
            Running = true;
            var tuning = Tuning.Bonus;
            if(tuning == null)
            {
                tuning = new BonusTaskTuning();
            }

            if (context.ShardId == null || !context.ShardId.HasValue)
            {
                throw new Exception("Top 100 must be given a shard_id to process");
            }

            using (var db = DAFactory.Get())
            {
                var endTime = Midnight();
                var endDay = endTime.Subtract(TimeSpan.FromMilliseconds(1));
                endDay = new DateTime(endDay.Year, endDay.Month, endDay.Day);
                
                var startTime = endTime.Subtract(TimeSpan.FromDays(4));
                var shardId = context.ShardId;


                while (startTime < endTime)
                {
                    var dayStart = startTime;
                    var dayEnd = dayStart.Add(TimeSpan.FromDays(1));

                    var stream = db.LotVisits.StreamBetween(shardId.Value, dayStart, dayEnd);
                    var enumerator = stream.GetEnumerator();

                    var hours = new Dictionary<int, double>();

                    while (Running && enumerator.MoveNext())
                    {
                        var visit = enumerator.Current;
                        var span = CalculateDateOverlap(dayStart, dayEnd, visit.time_created, visit.time_closed.Value);
                        if (hours.ContainsKey(visit.lot_id))
                        {
                            hours[visit.lot_id] += span.TotalMinutes;
                        }
                        else
                        {
                            hours.Add(visit.lot_id, span.TotalMinutes);
                        }
                    }

                    db.LotVisitTotals.Insert(hours.Where(x => x.Value > 0).Select(x =>
                    {
                        return new DbLotVisitTotal() { lot_id = x.Key, date = dayStart, minutes = (int)x.Value };
                    }));

                    startTime = startTime.Add(TimeSpan.FromDays(1));
                }

                var top100Calculated = db.LotTop100.Calculate(endTime, context.ShardId.Value);
                if (!top100Calculated){
                    throw new Exception("Unknown error while calculating top 100 lots");
                }

                var bonusMetrics = db.Bonus.GetMetrics(endDay, context.ShardId.Value).ToList(); //force this as a list. if we lazy evaluate it, an exception will be thrown.

                db.Bonus.Insert(bonusMetrics.Select(x =>
                {
                    int? bonus_property = null;
                    int? bonus_visitor = null;
                    int? bonus_sim = null;

                    if(x.visitor_minutes != null && x.visitor_minutes >= 60){
                        bonus_visitor = (int)Math.Floor((double)x.visitor_minutes / (double)60) * tuning.visitor_bonus.per_unit;
                    }

                    if(x.property_rank != null){
                        bonus_property = (100 - x.property_rank.Value + 1) * tuning.property_bonus.per_unit;

                        if(tuning.property_bonus.overrides != null &&
                            tuning.property_bonus.overrides.ContainsKey(x.property_rank.Value)){
                            bonus_property = tuning.property_bonus.overrides[x.property_rank.Value];
                        }
                    }

                    return new DbBonus()
                    {
                        avatar_id = x.avatar_id,
                        period = endDay,
                        bonus_property = bonus_property,
                        bonus_visitor = bonus_visitor,
                        bonus_sim = bonus_sim
                    };
                }));
            }
        }

        /// <summary>
        /// Calculates the overlap between two date ranges
        /// </summary>
        /// <param name="day"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private TimeSpan CalculateDateOverlap(DateTime r1_start, DateTime r1_end, DateTime r2_start, DateTime r2_end)
        {
            var startsInRange = r2_start >= r1_start && r2_start <= r1_end;
            var endsInRange = r2_end <= r1_end && r2_end >= r1_start;

            if (startsInRange && endsInRange)
            {
                //Within the range / equal
                return r2_end.Subtract(r2_start);
            }
            else if (startsInRange)
            {
                //Starts within range but does not end in range
                return r1_end.Subtract(r2_start);
            }
            else if (endsInRange)
            {
                //Ends in range but does not start in range
                return r2_end.Subtract(r1_start);
            }
            else
            {
                return new TimeSpan(0);
            }
        }

        private DateTime Midnight()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        }

        public void Abort()
        {
            Running = false;
        }

        public DbTaskType GetTaskType()
        {
            return DbTaskType.bonus;
        }
    }

    public class Top100Lot
    {
        public int lot_id;
        public LotCategory category;
        public int total;
        public int rank = -1;
    }

    public class BonusTaskParameter
    {
    }

    public class BonusTaskTuning
    {
        public PropertyBonusTuning property_bonus { get; set; } = new PropertyBonusTuning();
        public VisitorBonusTuning visitor_bonus { get; set; } = new VisitorBonusTuning();
    }

    public class PropertyBonusTuning
    {
        public int per_unit { get; set; } = 10;
        public Dictionary<byte, int> overrides { get; set; }
    }

    public class VisitorBonusTuning
    {
        public int per_unit { get; set; } = 5;
    }
}
