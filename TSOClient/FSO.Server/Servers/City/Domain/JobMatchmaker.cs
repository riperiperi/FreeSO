using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class JobMatchmaker
    {
        public static string[] JobXMLName =
        {
            "robotfactory",
            "restaurant",
            "restaurant",
            "nightclub",
            "nightclub",
        };
        public static int[][] JobGradeToLotGroup =
        {
            new int[]
            { //robot factory. basically 1-1 map
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            },
            new int[]
            { //waiter, cook
                0, 1, 2, 3, 3, 4, 4, 7, 7, 8, 8
            },
            new int[]
            { //dj, dancer
                0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2
            }
        };

        public static int[] JobPlayerLimit =
        {
            4,4,4,4,4
        };

        public Dictionary<uint, List<JobEntry>> JobTypeToInstance = new Dictionary<uint, List<JobEntry>>();
        public Dictionary<uint, JobEntry> InstanceIDToInstance = new Dictionary<uint, JobEntry>();
        public Dictionary<uint, JobEntry> InstanceForAvatar = new Dictionary<uint, JobEntry>();

        private IDataService DataService;

        public JobMatchmaker(IDataService dataService)
        {
            DataService = dataService;
        }

        public uint? TryGetJobLot(uint requestID, uint avatarID)
        {
            //todo: fail when job unavailable.
            lock (this)
            {
                JobEntry instance = null;
                if (InstanceForAvatar.TryGetValue(avatarID, out instance))
                {
                    //we've already been assigned an active instance.
                    //todo: maybe remove this once we leave
                    return instance.RealID;
                }

                var code = requestID - 0x200;
                var jobtype = (code-1) / 0x10;
                var jobgrade = (code - 1) % 0x10;
                List<JobEntry> instances = null;
                if (!JobTypeToInstance.TryGetValue(requestID, out instances))
                {
                    instances = new List<JobEntry>();
                    JobTypeToInstance[requestID] = instances;
                }
                //check if there are existing instances still to be filled.
                foreach (var possible in instances)
                {
                    if (possible.Avatars.Count < JobPlayerLimit[jobtype])
                    {
                        instance = possible; //we found it
                        break;
                    }
                }
                if (instance == null)
                {
                    //have to make a new instance.
                    instance = new JobEntry()
                    {
                        RealID = GetRealID(),
                        GradeType = requestID,
                        Avatars = new List<uint>()
                    };
                    var jobString = "{job:" + jobtype + ":" + jobgrade + "}";
                    DataService.Invalidate(instance.RealID, new FSO.Common.DataService.Model.Lot
                    {
                        DbId = 0,
                        Id = instance.RealID,
                        Lot_Name = jobString,
                        Lot_Location = new Location(),
                        Lot_Description = jobString,
                        Lot_IsOnline = true,
                        Lot_LeaderID = uint.MaxValue,
                        Lot_OwnerVec = new List<uint>() { uint.MaxValue },
                        Lot_RoommateVec = new List<uint>()
                    });
                    instances.Add(instance);
                    InstanceIDToInstance.Add(instance.RealID, instance);
                }
                InstanceForAvatar.Add(avatarID, instance);
                instance.Avatars.Add(avatarID);
                return instance.RealID;
            }
        }

        private uint GetRealID()
        {
            lock (this)
            {
                uint result = 0x300; //start search
                var keys = InstanceIDToInstance.Keys.OrderBy(x => x);
                foreach (var instance in keys)
                {
                    if (instance == result) result++; //not here. keep going.
                    else return result; //found a space
                }
                return result; //no spaces, place at end.
            }
        }

        public void RemoveJobLot(uint instanceID)
        {
            lock (this)
            {
                JobEntry instance = null;
                if (InstanceIDToInstance.TryGetValue(instanceID, out instance))
                {
                    InstanceIDToInstance.Remove(instanceID);
                    JobTypeToInstance[instance.GradeType].Remove(instance);
                    foreach (var avatar in instance.Avatars)
                    {
                        InstanceForAvatar.Remove(avatar);
                    }
                }
            }
        }
    }

    public class JobEntry
    {
        public uint RealID = 0x300;
        public uint GradeType;
        public List<uint> Avatars;
    }
}
