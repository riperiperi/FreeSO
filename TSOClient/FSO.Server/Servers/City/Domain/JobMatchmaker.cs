using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private IDAFactory DA;

        public JobMatchmaker(IDataService dataService, IDAFactory da)
        {
            DataService = dataService;
            DA = da;
        }

        public Tuple<uint?, uint> TryGetJobLot(uint requestID, uint avatarID)
        {
            //todo: fail when job unavailable.
            lock (this)
            {
                //first translate the id into job lot space

                var code = requestID - 0x200;
                var jobtype = (code - 1) / 0x10;
                var jobgrade = (code - 1) % 0x10;

                uint typeToJoin = jobtype;
                uint gradeToJoin = jobgrade;
                if (jobtype > 2)
                {
                    //nightclub
                    typeToJoin = 2;
                }
                //lowest grade that shares the same lot group
                gradeToJoin = (uint)Array.IndexOf(JobGradeToLotGroup[typeToJoin], JobGradeToLotGroup[typeToJoin][(int)jobgrade]);

                uint lotTypeID = typeToJoin * 0x10 + gradeToJoin + 0x201;

                JobEntry instance = null;
                if (InstanceForAvatar.TryGetValue(avatarID, out instance))
                {
                    //we've already been assigned an active instance.
                    //todo: maybe remove this once we leave
                    return new Tuple<uint?, uint>(instance.RealID, lotTypeID );
                }


                List<JobEntry> instances = null;
                if (!JobTypeToInstance.TryGetValue(lotTypeID, out instances))
                {
                    instances = new List<JobEntry>();
                    JobTypeToInstance[lotTypeID] = instances;
                }

                //get all our ignored avatars
                using (var da = DA.Get())
                {
                    var myIgnore = da.Bookmarks.GetAvatarIgnore(avatarID);

                    //check if there are existing instances still to be filled.
                    foreach (var possible in instances)
                    {
                        if (possible.Avatars[jobtype].Count < JobPlayerLimit[jobtype])
                        {
                            //if we're ignoring an avatar, we can't go to the property with them on it
                            //if they're ignoring us, same thing.
                            if (possible.Avatars.Any(
                                y => y.Any(x => myIgnore.Contains(x) || da.Bookmarks.GetAvatarIgnore(x).Contains(avatarID)))) continue;
                            instance = possible; //we found it
                            break;
                        }
                    }
                }
                
                if (instance == null)
                {
                    //have to make a new instance.
                    instance = new JobEntry()
                    {
                        RealID = GetRealID(),
                        GradeType = lotTypeID,
                    };
                    var jobString = "{job:" + typeToJoin + ":" + gradeToJoin + "}";
                    DataService.Invalidate(instance.RealID, new FSO.Common.DataService.Model.Lot
                    {
                        DbId = 0,
                        Id = instance.RealID,
                        Lot_Name = jobString,
                        Lot_Location = new Location(),
                        Lot_Description = jobString,
                        Lot_IsOnline = true,
                        Lot_LeaderID = uint.MaxValue,
                        Lot_OwnerVec = ImmutableList.Create(uint.MaxValue),
                        Lot_RoommateVec = ImmutableList.Create<uint>()
                    });
                    instances.Add(instance);
                    InstanceIDToInstance.Add(instance.RealID, instance);
                }
                InstanceForAvatar.Add(avatarID, instance);
                instance.Avatars[jobtype].Add(avatarID);
                return new Tuple<uint?, uint>(instance.RealID, lotTypeID);
            }
        }

        public bool TryJoinExisting(uint instanceID, uint avatarID)
        {
            lock (this)
            {
                using (var da = DA.Get())
                {
                    //check if there are existing instances still to be filled.
                    JobEntry instance = null;
                    if (InstanceIDToInstance.TryGetValue(instanceID, out instance))
                    {
                        //does this job lot have room for us?
                        var myJob = da.Avatars.GetCurrentJobLevel(avatarID);
                        //if we have a job, check allowed avatars of each type
                        //(don't care about avatars this job lot is not for right now)
                        var jobValid = myJob != null && myJob.job_type <= JobPlayerLimit.Length;
                        if (jobValid && instance.Avatars[myJob.job_type-1].Count >= JobPlayerLimit[myJob.job_type-1])
                            return false;

                        var myIgnore = da.Bookmarks.GetAvatarIgnore(avatarID);
                        //if we're ignoring an avatar, we can't go to the property with them on it
                        //if they're ignoring us, same thing.
                        if (instance.Avatars.Any(
                                y => y.Any(x => myIgnore.Contains(x) || da.Bookmarks.GetAvatarIgnore(x).Contains(avatarID))))
                            return false;

                        if (jobValid)
                            instance.Avatars[myJob.job_type - 1].Add(avatarID);
                    }
                }
                return true; //if there is no instance, we'll fail at the allocation connection step.
            }
        }

        public void RemoveAvatar(uint lotID, uint avatarID)
        {
            lock (this)
            {
                JobEntry instance = null;
                InstanceForAvatar.Remove(avatarID);
                if (InstanceIDToInstance.TryGetValue(lotID, out instance))
                {
                    foreach (var type in instance.Avatars)
                    {
                        type.Remove(avatarID);
                    }
                }
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
                    foreach (var type in instance.Avatars)
                    {
                        foreach (var avatar in type)
                        {
                            InstanceForAvatar.Remove(avatar);
                        }
                    }
                }
            }
        }
    }

    public class JobEntry
    {
        public uint RealID = 0x300;
        public uint GradeType;
        public List<uint>[] Avatars = new List<uint>[5];

        public JobEntry()
        {
            for (int i=0; i<Avatars.Length; i++)
            {
                Avatars[i] = new List<uint>();
            }
        }
    }
}
