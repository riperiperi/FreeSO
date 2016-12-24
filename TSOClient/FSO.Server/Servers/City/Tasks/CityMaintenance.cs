using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.LotTop100;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Tasks
{
    public class CityMaintenance : ITask
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IDAFactory DAFactory;
        private CityServerContext Context;

        private CityServerMaintenanceConfiguration Config;
        private bool Running;

        public CityMaintenance(IDAFactory DAFactory, CityServerContext context)
        {
            this.DAFactory = DAFactory;
            this.Context = context;
        }

        public void Run(TaskContext context)
        {
            Running = true;
            Config = (CityServerMaintenanceConfiguration)context.Data;

            using (var db = DAFactory.Get())
            {
                PurgeData(db);
                ProcessVisitorHours(db);
            }
        }

        private void ProcessVisitorHours(IDA db)
        {
            //Scan every lot that has had visitors over the visitor period
            //While doing this, calculate top 100 for the shard and visitor bonus
            var day4End = Midnight();
            var day4Start = day4End.Subtract(TimeSpan.FromDays(1));
            var day3End = day4Start;
            var day3Start = day3End.Subtract(TimeSpan.FromDays(1));
            var day2End = day3Start;
            var day2Start = day2End.Subtract(TimeSpan.FromDays(1));
            var day1End = day2Start;
            var day1Start = day1End.Subtract(TimeSpan.FromDays(1));
            
            var stream = db.LotVisits.StreamBetween(day1Start, day4End);
            var enumerator = stream.GetEnumerator();

            //The calculation is quite messy for SQL to do so implemented as code
            //We buffer the visits from SQL efficiently but the running totals are held in RAM
            //~32 bytes per lot that had visitors in the past 4 days ~3.5MB for 100,000 lots visited. 
            //Good enough for now.
            var hours = new Dictionary<int, double[]>();

            while (Running && enumerator.MoveNext())
            {
                var visit = enumerator.Current;

                var day1 = CalculateDateOverlap(day1Start, day1End, visit.time_created, visit.time_closed.Value);
                var day2 = CalculateDateOverlap(day2Start, day2End, visit.time_created, visit.time_closed.Value);
                var day3 = CalculateDateOverlap(day3Start, day3End, visit.time_created, visit.time_closed.Value);
                var day4 = CalculateDateOverlap(day4Start, day4End, visit.time_created, visit.time_closed.Value);

                if (!hours.ContainsKey(visit.lot_id))
                {
                    hours.Add(visit.lot_id, new double[4]);
                }

                var lotHours = hours[visit.lot_id];
                lotHours[0] += day1.TotalMinutes;
                lotHours[1] += day2.TotalMinutes;
                lotHours[2] += day3.TotalMinutes;
                lotHours[3] += day4.TotalMinutes;
            }

            var results = hours.Keys.Batch(50).SelectMany(x =>
            {
                var sideData = db.Lots.Get(x);

                return sideData.Select(lot =>
                {
                    var lotHours = hours[lot.lot_id];
                    var total = (lotHours[0] + lotHours[1] + lotHours[2] + lotHours[3]) / 4.0d;

                    return new
                    {
                        lot_id = lot.lot_id,
                        category = lot.category,
                        total = total
                    };
                });
            }).ToList();

            hours.Clear();

            foreach(var category in results.GroupBy(x => x.category))
            {
                var ordered = category.OrderByDescending(x => x.total).Take(100).ToList();
                var top100List = new List<DbLotTop100>();

                for(byte i=0; i < ordered.Count; i++){
                    var item = ordered[i];

                    top100List.Add(new DbLotTop100 {
                        shard_id = Context.ShardId,
                        category = category.Key,
                        rank = i,
                        lot_id = item.lot_id,
                        minutes = (int)Math.Floor(item.total)
                    });
                }

                //Fill in any gaps
                for(byte i = (byte)top100List.Count; i < 100; i++)
                {
                    top100List.Add(new DbLotTop100
                    {
                        shard_id = Context.ShardId,
                        category = category.Key,
                        rank = i
                    });
                }

                db.LotTop100.Replace(top100List);
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

            if (startsInRange && endsInRange){
                //Within the range / equal
                return r2_end.Subtract(r2_start);
            }else if(startsInRange){
                //Starts within range but does not end in range
                return r1_end.Subtract(r2_start);
            }else if (endsInRange) {
                //Ends in range but does not start in range
                return r2_end.Subtract(r1_start);
            }
            else
            {
                return new TimeSpan(0);
            }
        }

        private void PurgeData(IDA db)
        {
            //Purge visit data
            if (Config.Visits_Retention_Period < 4){
                throw new Exception("Visits retention period cannot be less than the top 100 average period");
            }
            var midnight = Midnight();

            db.LotVisits.PurgeByDate(midnight.Subtract(TimeSpan.FromDays(Config.Visits_Retention_Period)));

            //Purge old shard & auth tickets
            var expireTime = Epoch.Now - 600;
            db.AuthTickets.Purge(expireTime);
            db.Shards.PurgeTickets(expireTime);
        }

        public void Abort()
        {
            Running = false;
        }

        private DateTime Midnight()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        }
    }
}
