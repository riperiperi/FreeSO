using FSO.Client.Rendering.City.Model;
using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Enum;
using FSO.Common.Security;
using FSO.Common.Utils;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Elections;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Database.DA.LotVisits;
using FSO.Server.Database.DA.Neighborhoods;
using FSO.Server.Framework.Voltron;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ninject;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Providers
{
    public class ServerNeighborhoodProvider : EagerDataServiceProvider<uint, Neighborhood>
    {
        private IRealestateDomain GlobalRealestate;
        private IShardRealestateDomain Realestate;
        private int ShardId;
        private IDAFactory DAFactory;
        private IServerNFSProvider NFS;

        private ServerLotProvider Lots;

        public ServerNeighborhoodProvider([Named("ShardId")] int shardId, IRealestateDomain realestate, IDAFactory daFactory, IServerNFSProvider nfs)
        {
            OnMissingLazyLoad = true;
            OnLazyLoadCacheValue = false;

            ShardId = shardId;
            GlobalRealestate = realestate;
            Realestate = realestate.GetByShard(shardId);
            DAFactory = daFactory;
            NFS = nfs;
        }

        public void BindCityRep(ServerLotProvider lots)
        {
            //we provide the city representation with a JSON rep of the nhood structure
            Lots = lots;
        }
        
        private int WorstRating;

        protected override void PreLoad(Callback<uint, Neighborhood> appender)
        {
            using (var db = DAFactory.Get())
            {
                var nhoods = db.Neighborhoods.All(ShardId);

                var midnight = LotVisitUtils.Midnight(); //gets this morning's midnight (basically current date, with day reset)
                var activityBeginning = midnight - new TimeSpan(7, 0, 0, 0);

                var visits = db.LotVisits.StreamBetweenPlusNhood(ShardId, activityBeginning, midnight).ToList();
                var enumerator = visits.GetEnumerator();
                var nhoodHours = new Dictionary<uint, double>();
                while (enumerator.MoveNext())
                {
                    var visit = enumerator.Current;
                    var span = LotVisitUtils.CalculateDateOverlap(activityBeginning, midnight, visit.time_created, visit.time_closed.Value);
                    if (nhoodHours.ContainsKey(visit.neighborhood_id))
                    {
                        nhoodHours[visit.neighborhood_id] += span.TotalMinutes;
                    }
                    else
                    {
                        nhoodHours.Add(visit.neighborhood_id, span.TotalMinutes);
                    }
                }

                var order = nhoodHours.OrderByDescending(x => x.Value).ToList();
                WorstRating = nhoods.Count;
                foreach (var item in nhoods)
                {
                    var lots = db.Lots.GetLocationsInNhood((uint)item.neighborhood_id);
                    var avatars = db.Avatars.GetLivingInNhood((uint)item.neighborhood_id);
                    var townHall = db.Lots.Get(item.town_hall_id ?? 0)?.location ?? 0;
                    var cycle = (item.election_cycle_id == null) ? null : db.Elections.GetCycle(item.election_cycle_id.Value);
                    var converted = HydrateOne(item, avatars, lots, townHall, cycle, visits, order);
                    var intId = (uint)item.neighborhood_id;
                    appender(intId, converted);
                }

                //
                var neighObj = nhoods.Select(x =>
                {
                    var loc = MapCoordinates.Unpack(x.location);
                    var result = new CityNeighbourhood()
                    {
                        Name = x.name,
                        Color = new Color(x.color),
                        Description = x.description,
                        GUID = x.guid,
                        Location = new Point(loc.X, loc.Y),
                        ID = x.neighborhood_id
                    };
                    return result;
                }).ToList();

                Lots.CityRepresentation.City_NeighJSON = JsonConvert.SerializeObject(neighObj);
            }
        }

        protected override Neighborhood LoadOne(uint key)
        {
            using (var db = DAFactory.Get())
            {
                var nhood = db.Neighborhoods.Get(key);
                if (nhood == null || nhood.shard_id != ShardId) return null;
                else
                {
                    var lots = db.Lots.GetLocationsInNhood((uint)nhood.neighborhood_id);
                    var avatars = db.Avatars.GetLivingInNhood((uint)nhood.neighborhood_id);
                    var townHall = db.Lots.Get(nhood.town_hall_id ?? 0)?.location ?? 0;
                    var cycle = (nhood.election_cycle_id == null) ? null : db.Elections.GetCycle(nhood.election_cycle_id.Value);
                    return HydrateOne(nhood, avatars, lots, townHall, cycle, null, null);
                }
            }
        }

        private List<uint> SelectRandom(List<uint> input, int count, Random rand)
        {
            var result = new List<uint>();
            var shuff = new List<uint>(input);
            for (int i=0; i<count; i++)
            {
                var ind = rand.Next(shuff.Count);
                result.Add(shuff[ind]);
                shuff.RemoveAt(ind);
            }
            return result;
        }

        public static void SetElectionCycle(Neighborhood nhood, DbElectionCycle cycle)
        {
            if (cycle == null) return;
            var result = new ElectionCycle()
            {
                ElectionCycle_CurrentState = (byte)cycle.current_state,
                ElectionCycle_ElectionType = (byte)cycle.election_type,
                ElectionCycle_EndDate = cycle.end_date,
                ElectionCycle_StartDate = cycle.start_date
            };
            nhood.Neighborhood_ElectionCycle = result;
        }

        public void SetTop10s(Neighborhood nhood, List<uint> avatars, List<DbLotVisitNhood> visits, List<KeyValuePair<uint, double>> order)
        {
            var midnight = LotVisitUtils.Midnight(); //gets this morning's midnight (basically current date, with day reset)
            var activityBeginning = midnight - new TimeSpan(7, 0, 0, 0);

            var index = order.FindIndex(x => x.Key == nhood.Id);
            nhood.Neighborhood_ActivityRating = (uint)((index == -1) ? WorstRating : (index+1));
            var subset = visits.Where(x => x.neighborhood_id == nhood.Id && x.type == DbLotVisitorType.visitor);

            //get the total hours for all lots in the nhood
            var enumerator = subset.GetEnumerator();
            var lotHours = new Dictionary<uint, double>();
            var cats = new Dictionary<uint, LotCategory>();
            while (enumerator.MoveNext())
            {
                var visit = enumerator.Current;
                var span = LotVisitUtils.CalculateDateOverlap(activityBeginning, midnight, visit.time_created, visit.time_closed.Value);
                if (lotHours.ContainsKey((uint)visit.location))
                {
                    lotHours[(uint)visit.location] += span.TotalMinutes;
                }
                else
                {
                    lotHours.Add((uint)visit.location, span.TotalMinutes);
                    cats.Add((uint)visit.location, visit.category);
                }
            }

            var ordered = lotHours.OrderByDescending(x => x.Value);

            //TOP 10 OVERALL
            nhood.Neighborhood_TopLotOverall = ImmutableList.Create(ordered.Take(10).Select(x => x.Key).ToArray());

            //TOP 10 CATEGORIES
            var categories = Enum.GetValues(typeof(LotCategory)).Cast<LotCategory>().Skip(1).Take(10);
            nhood.Neighborhood_TopLotCategory = ImmutableList.Create(categories.Select(x => ordered.FirstOrDefault(y => cats[y.Key] == x).Key).ToArray());

            //avatars activity outwith their neighbourhood still counts - so we need to take another subset.
            var avas = new HashSet<uint>(avatars); //faster if this is a hashset
            var subset2 = visits.Where(x => avas.Contains(x.avatar_id));

            //TOP 10 ACTIVITY
            enumerator = subset2.GetEnumerator();
            var avaHours = new Dictionary<uint, double>();
            while (enumerator.MoveNext())
            {
                var visit = enumerator.Current;
                var span = LotVisitUtils.CalculateDateOverlap(activityBeginning, midnight, visit.time_created, visit.time_closed.Value);
                if (avaHours.ContainsKey((uint)visit.avatar_id))
                {
                    avaHours[(uint)visit.avatar_id] += span.TotalMinutes;
                }
                else
                {
                    avaHours.Add((uint)visit.avatar_id, span.TotalMinutes);
                }
            }
            ordered = avaHours.OrderByDescending(x => x.Value);

            nhood.Neighborhood_TopAvatarActivity = ImmutableList.Create(ordered.Take(10).Select(x => x.Key).ToArray());
        }

        protected Neighborhood HydrateOne(DbNeighborhood nhood, List<uint> avatars, List<uint> lots, uint townHallLoc, DbElectionCycle cycle,
             List<DbLotVisitNhood> visits, List<KeyValuePair<uint, double>> order)
        {
            var rand = new Random();

            var result = new Neighborhood
            {
                Id = (uint)nhood.neighborhood_id,

                Neighborhood_ActivityRating = 1,
                Neighborhood_AvatarCount = (uint)avatars.Count,
                Neighborhood_CenterGridXY = nhood.location,
                Neighborhood_Color = nhood.color,
                Neighborhood_Description = nhood.description,
                Neighborhood_ElectedDate = nhood.mayor_elected_date,
                Neighborhood_ElectionCycle = new ElectionCycle(),
                Neighborhood_Flag = nhood.flag,
                Neighborhood_IconURL = nhood.icon_url ?? "",
                Neighborhood_LotCount = (uint)lots.Count,
                Neighborhood_MayorID = nhood.mayor_id ?? 0,
                Neighborhood_Name = nhood.name,
                Neighborhood_TownHallXY = townHallLoc,
            };

            SetElectionCycle(result, cycle);
            SetTop10s(result, avatars, visits, order);
            
            return result;
        }

        public override void PersistMutation(object entity, MutationType type, string path, object value)
        {
            var neigh = entity as Neighborhood;

            switch (path)
            {
                case "Neighborhood_Description":
                    using (var db = DAFactory.Get())
                    {
                        db.Neighborhoods.UpdateDescription(neigh.Id, neigh.Neighborhood_Description);
                    }
                    break;
                case "Neighborhood_Name":
                    using (var db = DAFactory.Get())
                    {
                        db.Neighborhoods.UpdateName(neigh.Id, neigh.Neighborhood_Name);
                    }
                    break;
            }
        }

        public override void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
            var neigh = entity as Neighborhood;

            //currently, only admins can mutate neighborhoods
            var volt = (context as VoltronSession);
            if (volt == null) throw new SecurityException("Neighborhoods cannot be mutated by non-voltron connections.");
            using (var da = DAFactory.Get())
            {
                var mod = da.Avatars.GetModerationLevel(volt.AvatarId);
                if (mod < 2) throw new SecurityException("Neighborhoods can only be mutated by administrators.");
            }

            switch (path)
            {
                case "Neighborhood_Description":
                    var desc = value as string;
                    if (desc != null && desc.Length > 1000)
                        throw new Exception("Description too long!");
                    break;
                case "Neighborhood_Name":
                    var name = value as string;
                    if (name != null && name.Length > 100)
                        throw new Exception("Name too long!");
                    break;
                default:
                    throw new SecurityException("Field: " + path + " may not be mutated by users");
            }
        }
    }
}
