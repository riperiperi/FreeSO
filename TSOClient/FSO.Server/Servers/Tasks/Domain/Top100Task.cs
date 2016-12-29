using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
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
    public class Top100Task : ITask
    {
        private IDAFactory DAFactory;
        private bool Running;

        public Top100Task(IDAFactory DAFactory)
        {
            this.DAFactory = DAFactory;
        }

        public void Run(TaskContext context)
        {
            Running = true;

            if(context.ShardId == null || !context.ShardId.HasValue)
            {
                throw new Exception("Top 100 must be given a shard_id to process");
            }

            using (var db = DAFactory.Get())
            {
                var endDate = Midnight();
                var startDate = endDate.Subtract(TimeSpan.FromDays(4));
                var shardId = context.ShardId;


                while (startDate < endDate)
                {
                    var dayStart = startDate;
                    var dayEnd = dayStart.Add(TimeSpan.FromDays(1));

                    var stream = db.LotVisits.StreamBetween(shardId.Value, dayStart, dayEnd);
                    var enumerator = stream.GetEnumerator();

                    //The calculation is quite messy for SQL to do so implemented as code
                    //We buffer the visits from SQL efficiently but the running totals are held in RAM
                    //~32 bytes per lot that had visitors in the past 4 days ~3.5MB for 100,000 lots visited. 
                    //Good enough for now.
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

                    startDate = startDate.Add(TimeSpan.FromDays(1));
                }

                var top100Calculated = db.LotTop100.Calculate(endDate, context.ShardId.Value);
                if (!top100Calculated){
                    throw new Exception("Unknown error while calculating top 100 lots");
                }
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
            return DbTaskType.top100;
        }
    }

    public class Top100Lot
    {
        public int lot_id;
        public LotCategory category;
        public int total;
        public int rank = -1;
    }

    public class Top100TaskParameter
    {
    }

    public class Top100TaskTuning
    {
        public int BonusPerVisitorHour { get; set; } = 5;
        public int BonusPerPropertyTop100Rank { get; set; } = 10;
    }
}
