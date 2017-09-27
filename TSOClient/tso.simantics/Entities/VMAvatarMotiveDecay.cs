using FSO.Files;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        TuningEntry LotMotives = Content.Content.Get().GlobalTuning.EntriesByName["lotmotives"];
        TuningEntry SimMotives = Content.Content.Get().GlobalTuning.EntriesByName["simmotives"];

        public void Tick(VMAvatar avatar, VMContext context)
        {
            var roomScore = context.GetRoomScore(context.GetRoomAt(avatar.Position));
            avatar.SetMotiveData(VMMotive.Room, roomScore);
            if (context.Clock.Minutes == LastMinute) return;
            LastMinute = context.Clock.Minutes;

            var cat = context.VM.TSOState.PropertyCategory;
            string category = CategoryNames[(cat==255)?0:cat];
            string sleepState = (avatar.GetMotiveData(VMMotive.SleepState) == 0)?"Awake":"Asleep";

            int moodSum = 0;

            for (int i = 0; i < 7; i++) {
                int lotMul = ToFixed1000(LotMotives.GetNum(category + "_" + LotMotiveNames[i] + "Weight"));
                int frac = 0;
                var motive = avatar.GetMotiveData(DecrementMotives[i]);
                var r_Hunger = FracMul(ToFixed1000(SimMotives.GetNum("HungerDecrementRatio")) * (100+avatar.GetMotiveData(VMMotive.Hunger)), ToFixed1000(LotMotives.GetNum(category+"_HungerWeight")));
                switch (i)
                {
                    case 0:
                        frac = r_Hunger; break;
                    case 1:
                        frac = FracMul(ToFixed1000(SimMotives.GetNum("ComfortDecrementActive")), lotMul); break;
                    case 2:
                        frac = FracMul(ToFixed1000(SimMotives.GetNum("HygieneDecrement" + sleepState)), lotMul); break;
                    case 3:
                        frac = FracMul(ToFixed1000(SimMotives.GetNum("BladderDecrement" + sleepState)), lotMul) + FracMul(r_Hunger, ToFixed1000(SimMotives.GetNum("HungerToBladderMultiplier"))); break;
                    case 4:
                        frac = (ToFixed1000(SimMotives.GetNum("EnergySpan")) / (60 * (int)SimMotives.GetNum("WakeHours"))); 
                        // TODO: wrong but appears to be close? need one which uses energy weight, which is about 2.4 on skills
                        break;
                    case 5:
                        frac = (sleepState == "Asleep") ? 0 : FracMul(ToFixed1000(SimMotives.GetNum("EntDecrementAwake")), lotMul);
                        break;
                    case 6:
                        frac = ToFixed1000(SimMotives.GetNum("SocialDecrementBase")) + 
                            FracMul((ToFixed1000(SimMotives.GetNum("SocialDecrementMultiplier")) * (100+motive)), lotMul);
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
