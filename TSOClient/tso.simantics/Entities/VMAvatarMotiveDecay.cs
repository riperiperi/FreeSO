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
    public class VMAvatarMotiveDecay : VMSerializable
    {
        public string[] LotMotiveNames = new string[]
        {
            "Hunger",
            "Comfort",
            "Hygiene",
            "Bladder",

            "Energy",
            "Fun",
            "Social"
        };

        public VMMotive[] DecrementMotives = new VMMotive[]
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
            if (context.Clock.Minutes == LastMinute) return;
            LastMinute = context.Clock.Minutes;

            string category = "Skills";
            string sleepState = (avatar.GetMotiveData(VMMotive.SleepState) == 0)?"Awake":"Asleep";

            int moodSum = 0;

            for (int i = 0; i < 7; i++) {
                if (avatar.IsPet && i == 5) return;
                float lotMul = LotMotives.GetNum(category + "_" + LotMotiveNames[i] + "Weight");
                float frac = 0;
                var motive = avatar.GetMotiveData(DecrementMotives[i]);
                var r_Hunger = (SimMotives.GetNum("HungerDecrementRatio") * (100+avatar.GetMotiveData(VMMotive.Hunger))) * LotMotives.GetNum(category+"_HungerWeight");
                switch (i)
                {
                    case 0:
                        frac = r_Hunger; break;
                    case 1:
                        frac = (SimMotives.GetNum("ComfortDecrementActive") * lotMul); break;
                    case 2:
                        frac = (SimMotives.GetNum("HygieneDecrement" + sleepState) * lotMul); break;
                    case 3:
                        frac = (SimMotives.GetNum("BladderDecrement" + sleepState) * lotMul) + (SimMotives.GetNum("HungerToBladderMultiplier") * r_Hunger); break;
                    case 4:
                        frac = (SimMotives.GetNum("EnergySpan") / (60 * SimMotives.GetNum("WakeHours"))); 
                        // TODO: wrong but appears to be close? need one which uses energy weight, which is about 2.4 on skills
                        break;
                    case 5:
                        frac = (sleepState == "Asleep") ? 0 : (SimMotives.GetNum("EntDecrementAwake") * lotMul);
                        break;
                    case 6:
                        frac = (SimMotives.GetNum("SocialDecrementBase") + (SimMotives.GetNum("SocialDecrementMultiplier") * (100+motive))) * lotMul;
                        frac /= 2; //make this less harsh right now, til I can work out how multiplayer bonus is meant to work
                        break;
                }

                MotiveFractions[i] += (short)(frac * 1000);
                if (MotiveFractions[i] >= 1000)
                {
                    motive -= (short)(MotiveFractions[i] / 1000);
                    MotiveFractions[i] %= 1000;
                    if (motive < -100) motive = -100;
                    avatar.SetMotiveData(DecrementMotives[i], motive);
                }
                moodSum += motive;
            }
            avatar.SetMotiveData(VMMotive.Mood, (short)(moodSum / 7));
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
