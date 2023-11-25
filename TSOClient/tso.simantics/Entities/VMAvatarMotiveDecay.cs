using FSO.Files;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Entities
{
    public interface VMIMotiveDecay : VMSerializable
    {
        void Tick(VMAvatar avatar, VMContext context);
    }

    public class VMAvatarMotiveDecay : VMIMotiveDecay
    {
        public static string[] LotMotiveNames = new string[]
        {
            "Hunger",
            "Comfort",
            "Hygiene",
            "Bladder",

            "Energy",
            "Fun",
            "Social"
        };

        public static string[] CategoryNames = new string[]
        {
            "None",
            "Money",
            "Offbeat",
            "Romance",
            "Services",
            "Shopping",
            "Skills",
            "Welcome",
            "Games",
            "Entertain",
            "Residence"
        };

        public static VMMotive[] DecrementMotives = new VMMotive[]
        {
            VMMotive.Hunger,
            VMMotive.Comfort,
            VMMotive.Hygiene,
            VMMotive.Bladder,
            VMMotive.Energy,
            VMMotive.Fun,
            VMMotive.Social
        };

        public short[] MotiveFractions = new short[7]; //in 1/1000ths
        public int LastMinute;

        TuningEntry LotMotives = Content.Content.Get()?.GlobalTuning.EntriesByName["lotmotives"] ?? TuningEntry.DEFAULT;
        TuningEntry SimMotives = Content.Content.Get()?.GlobalTuning.EntriesByName["simmotives"] ?? TuningEntry.DEFAULT;

        public int LastCategory = -1;
        public int[] LotMuls;
        public int[] FlatSimMotives;

        public VMAvatarMotiveDecay()
        {
            if (SimMotives.KeyValueCount == 0)
            {
                return;
            }

            FlatSimMotives = new int[]
            {
                ToFixed1000(SimMotives.GetNum("HungerDecrementRatio")), //0
                ToFixed1000(SimMotives.GetNum("ComfortDecrementActive")), //1
                ToFixed1000(SimMotives.GetNum("HygieneDecrementAsleep")), //2
                ToFixed1000(SimMotives.GetNum("HygieneDecrementAwake")), //3
                ToFixed1000(SimMotives.GetNum("BladderDecrementAsleep")), //4
                ToFixed1000(SimMotives.GetNum("BladderDecrementAwake")), //5
                ToFixed1000(SimMotives.GetNum("HungerToBladderMultiplier")), //6
                ToFixed1000(SimMotives.GetNum("EnergySpan")), //7
                (int)SimMotives.GetNum("WakeHours"), //8
                ToFixed1000(SimMotives.GetNum("EntDecrementAwake")), //9
                ToFixed1000(SimMotives.GetNum("SocialDecrementBase")), //10
                ToFixed1000(SimMotives.GetNum("SocialDecrementMultiplier")), //11
            };
        }

        public void UpdateCategory(VMContext context)
        {
            var cat = context.VM.TSOState.PropertyCategory;
            if (cat != LastCategory)
            {
                LotMuls = new int[7];
                string category = CategoryNames[(cat>10) ? 0 : cat];
                for (int i = 0; i < 7; i++)
                {
                    LotMuls[i] = ToFixed1000(LotMotives.GetNum(category + "_" + LotMotiveNames[i] + "Weight"));
                }
                LastCategory = cat;
            }
        }

        public void Tick(VMAvatar avatar, VMContext context)
        {
            var roomScore = context.GetRoomScore(context.GetRoomAt(avatar.Position));
            avatar.SetMotiveData(VMMotive.Room, roomScore);
            if (context.Clock.Minutes == LastMinute) return;
            if (avatar.GetPersonData(VMPersonDataVariable.Cheats) > 0) return;
            LastMinute = context.Clock.Minutes;

            UpdateCategory(context);
            int sleepState = (avatar.GetMotiveData(VMMotive.SleepState) == 0)?1:0;

            int moodSum = 0;

            for (int i = 0; i < 7; i++) {
                int lotMul = LotMuls[i];
                int frac = 0;
                var motive = avatar.GetMotiveData(DecrementMotives[i]);
                var r_Hunger = FracMul(FlatSimMotives[0] * (100 + avatar.GetMotiveData(VMMotive.Hunger)), LotMuls[0]);
                switch (i)
                {
                    case 0:
                        frac = r_Hunger; break;
                    case 1:
                        frac = FracMul(FlatSimMotives[1], lotMul); break;
                    case 2:
                        frac = FracMul(FlatSimMotives[2 + sleepState], lotMul); break;
                    case 3:
                        frac = FracMul(FlatSimMotives[4+sleepState], lotMul) + FracMul(r_Hunger, FlatSimMotives[6]); break;
                    case 4:
                        frac = (FlatSimMotives[7] / (60 * FlatSimMotives[8])); 
                        // TODO: wrong but appears to be close? need one which uses energy weight, which is about 2.4 on skills
                        break;
                    case 5:
                        frac = (sleepState == 0) ? 0 : FracMul(FlatSimMotives[9], lotMul);
                        break;
                    case 6:
                        frac = FlatSimMotives[10] + 
                            FracMul((FlatSimMotives[11] * (100+motive)), lotMul);
                        frac /= 2; //make this less harsh right now, til I can work out how multiplayer bonus is meant to work
                        break;
                }

                MotiveFractions[i] += (short)frac;
                if (MotiveFractions[i] >= 1000)
                {
                    motive -= (short)(MotiveFractions[i] / 1000);
                    MotiveFractions[i] %= 1000;
                    if (motive < -100) motive = -100;
                    avatar.SetMotiveData(DecrementMotives[i], motive);
                }
                moodSum += motive;
            }
            moodSum += roomScore;

            avatar.SetMotiveData(VMMotive.Mood, (short)(moodSum / 8));
        }

        public int ToFixed1000(float input)
        {
            return (int)(input * 1000);
        }

        public int FracMul(int input, int frac)
        {
            return (int)((long)input * frac) / 1000;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(LastMinute);
            for (int i = 0; i < 7; i++)
                 writer.Write(MotiveFractions[i]);
        }

        public void Deserialize(BinaryReader reader)
        {
            LastMinute = reader.ReadInt32();
            for (int i=0; i<7; i++)
                MotiveFractions[i] = reader.ReadInt16();
        }
    }
}
