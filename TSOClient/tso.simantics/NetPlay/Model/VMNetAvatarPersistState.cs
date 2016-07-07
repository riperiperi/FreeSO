using FSO.SimAntics.Model.TSOPlatform;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FSO.SimAntics.Model;
using FSO.SimAntics.Marshals;

namespace FSO.SimAntics.NetPlay.Model
{
    public class VMNetAvatarPersistState : VMSerializable
    {
        // to be loaded and sent to the VM, which uses it to initialize a user's sim.
        // some of these attributes must be saved in the database, so they must be re-initialized by the server.
        // a lot of them are not in the database, so this state storage is the only way you're getting them.

        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;

        public string Name;
        public uint PersistID;
        public VMAvatarDefaultSuits DefaultSuits;
        public VMTSOAvatarPermissions Permissions;
        public uint Budget; //partial db... only here for check tree purposes. real transactions always go through db.

        public ulong BodyOutfit;
        public ulong HeadOutfit;
        public byte SkinTone; //ALSO person data 60

        public short[] MotiveData = new short[16]; //lots of this is garbage data. Copy relevant motives from DB.
        private short[] PersonData = new short[27]; //special selection of things which should persist.

        //relationships
        public VMEntityPersistRelationshipMarshal[] Relationships;

        //===== PERSON DATA =====

        //todo: personality? tso doesn't really use it (force max)

        public short CookingSkill { get { return PersonData[0]; } set { PersonData[0] = value; } } //10 - copy from ~~>DB<~~
        public short CharismaSkill { get { return PersonData[1]; } set { PersonData[1] = value; } } //11 - copy from ~~>DB<~~
        public short MechanicalSkill { get { return PersonData[2]; } set { PersonData[2] = value; } } //12 - copy from ~~>DB<~~
        public short CreativitySkill { get { return PersonData[3]; } set { PersonData[3] = value; } } //15 - copy from ~~>DB<~~
        public short BodySkill { get { return PersonData[4]; } set { PersonData[4] = value; } } //17 - copy from ~~>DB<~~
        public short LogicSkill { get { return PersonData[5]; } set { PersonData[5] = value; } } //18 - copy from ~~>DB<~~

        //note: interests. tso does not use but could be interesting to reintroduce
        public short Gender { get { return PersonData[6]; } set { PersonData[6] = value; } } //65 - can get complex for pets, but only their carrier will be serialized (using object serialization) ~~>DB<~~
        public short IsGhost { get { return PersonData[7]; } set { PersonData[7] = value; } } //68 - 0/1 ~~>DB<~~

        public short NumOutgoingFriends { get { return PersonData[8]; } set { PersonData[8] = value; } } //99 - should probably derive from relationship matrix?
        public short IncomingFriends { get { return PersonData[9]; } set { PersonData[9] = value; } } //61

        public short SkillLock { get { return PersonData[10]; } set { PersonData[10] = value; } } //70 - bitfield specifying which skills have been locked on this sim. ~~>DB<~~
        public short SkillLockBody { get { return PersonData[11]; } set { PersonData[11] = value; } } //81 ~~>DB<~~
        public short SkillLockCharisma { get { return PersonData[12]; } set { PersonData[12] = value; } } //82 ~~>DB<~~
        public short SkillLockCooking { get { return PersonData[13]; } set { PersonData[13] = value; } } //83 ~~>DB<~~
        public short SkillLockCreativity { get { return PersonData[14]; } set { PersonData[14] = value; } } //84 ~~>DB<~~
        public short SkillLockLogic { get { return PersonData[15]; } set { PersonData[15] = value; } } //85 ~~>DB<~~
        public short SkillLockMechanical { get { return PersonData[16]; } set { PersonData[16] = value; } } //86 ~~>DB<~~

        public short DeathTicker { get { return PersonData[17]; } set { PersonData[17] = value; } } //87 - how long the sim has been dead for. (does not tick off lot?) 
        public short GardenerRehireTicker { get { return PersonData[18]; } set { PersonData[18] = value; } } //88
        public short MaidRehireTicker { get { return PersonData[19]; } set { PersonData[19] = value; } } //89
        public short RepairmanRehireTicker { get { return PersonData[20]; } set { PersonData[20] = value; } } //90

        //ONLINE JOBS!
        public short OnlineJobID { get { return PersonData[21]; } set { PersonData[21] = value; } } //91 ~~>DB<~~

        public Dictionary<short, VMTSOJobInfo> OnlineJobInfo = new Dictionary<short, VMTSOJobInfo>();
        /*public short OnlineJobGrade { get { return PersonData[22]; } set { PersonData[22] = value; } } //92 ~~>DB<~~
        public short OnlineJobXP { get { return PersonData[23]; } set { PersonData[23] = value; } } //93 ~~>DB<~~
        public short OnlineJobSickDays { get { return PersonData[24]; } set { PersonData[24] = value; } } //94 ~~>DB<~~
        public short OnlineJobStatusFlags { get { return PersonData[25]; } set { PersonData[25] = value; } } //98 ~~>DB<~~*/

        public short BadgeLevel { get { return PersonData[26]; } set { PersonData[26] = value; } } //100 (not sure, maybe number of days active?)

        public static int[] PersonDataMap =
        {
            //skills
            10,
            11,
            12,
            15,
            17,
            18,

            65,
            68,

            99,
            61,

            //skill locks
            70,
            81,
            82,
            83,
            84,
            85,
            86,

            //tickers
            87,
            88,
            89,
            90,

            //onlinejobs
            91,

            100
        };

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Name);
            writer.Write(PersistID);
            DefaultSuits.SerializeInto(writer);
            writer.Write((byte)Permissions);
            writer.Write(Budget); //partial db... only here for check tree purposes. real transactions always go through db.

            writer.Write(BodyOutfit);
            writer.Write(HeadOutfit);
            writer.Write(SkinTone); //ALSO person data 60

            writer.Write(VMSerializableUtils.ToByteArray(MotiveData));
            writer.Write(VMSerializableUtils.ToByteArray(PersonData));

            writer.Write(Relationships.Length);
            foreach (var rel in Relationships)
            {
                rel.SerializeInto(writer);
            }

            writer.Write(OnlineJobInfo.Count);
            foreach (var item in OnlineJobInfo)
            {
                writer.Write(item.Key);
                item.Value.SerializeInto(writer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            Version = reader.ReadInt32();
            Name = reader.ReadString();
            PersistID = reader.ReadUInt32();
            DefaultSuits = new VMAvatarDefaultSuits(reader);
            Permissions = (VMTSOAvatarPermissions)reader.ReadByte();
            Budget = reader.ReadUInt32();

            BodyOutfit = reader.ReadUInt64();
            HeadOutfit = reader.ReadUInt64();
            SkinTone = reader.ReadByte();

            for (int i = 0; i < MotiveData.Length; i++) MotiveData[i] = reader.ReadInt16();
            for (int i = 0; i < PersonData.Length; i++) PersonData[i] = reader.ReadInt16();

            var count = reader.ReadInt32();
            Relationships = new VMEntityPersistRelationshipMarshal[count];
            for (int i = 0; i< Relationships.Length; i++)
            {
                Relationships[i] = new VMEntityPersistRelationshipMarshal();
                Relationships[i].Deserialize(reader);
            }

            var jobs = reader.ReadInt32();
            for (int i = 0; i < jobs; i++)
            {
                var id = reader.ReadInt16();
                var job = new VMTSOJobInfo();
                job.Deserialize(reader);
                OnlineJobInfo[id] = job;
            }
        }

        public void Apply(VMAvatar avatar)
        {
            avatar.SkinTone = (AppearanceType)SkinTone;
            for (int i = 0; i < PersonDataMap.Length; i++)
                avatar.SetPersonData((VMPersonDataVariable)PersonDataMap[i], PersonData[i]);
            avatar.SetPersonData(VMPersonDataVariable.SkinColor, SkinTone);
            avatar.DefaultSuits = DefaultSuits;
            avatar.BodyOutfit = BodyOutfit;
            avatar.HeadOutfit = HeadOutfit;
            avatar.Name = Name;
            ((VMTSOAvatarState)avatar.TSOState).Permissions = Permissions;
            avatar.TSOState.Budget.Value = Budget;

            avatar.PersistID = PersistID;

            for (int i = 0; i < MotiveData.Length; i++) avatar.SetMotiveData((VMMotive)i, MotiveData[i]);
            avatar.MeToPersist = new Dictionary<uint, List<short>>();
            foreach (var obj in Relationships) avatar.MeToPersist[obj.Target] = new List<short>(obj.Values);

            ((VMTSOAvatarState)avatar.TSOState).JobInfo = OnlineJobInfo;
        }

        public void Save(VMAvatar avatar)
        {
            SkinTone = (byte)avatar.SkinTone;
            DefaultSuits = avatar.DefaultSuits; //todo: clone?
            BodyOutfit = avatar.BodyOutfit;
            HeadOutfit = avatar.HeadOutfit;
            Name = avatar.Name;
            Permissions = ((VMTSOAvatarState)avatar.TSOState).Permissions;
            Budget = avatar.TSOState.Budget.Value;

            for (int i = 0; i < MotiveData.Length; i++) MotiveData[i] = avatar.GetMotiveData((VMMotive)i);
            for (int i = 0; i < PersonDataMap.Length; i++)
            {
                PersonData[i] = avatar.GetPersonData((VMPersonDataVariable)PersonDataMap[i]);
            }
            OnlineJobInfo = ((VMTSOAvatarState)avatar.TSOState).JobInfo;
        }
    }
}
