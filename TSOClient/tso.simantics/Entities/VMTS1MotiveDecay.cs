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
    public class VMTS1MotiveDecay : VMIMotiveDecay
    {
        public static float[] Constants = new float[]
        {
            180, //energy span 0
            16, //wake hours 1
            7, //wake hour 2
            0.01f, //energy drift 3
            0.3f, //hunger to bladder 4
            0.0021f, //hunger decrement ratio 5
            0.055f, //social decrement base 6
            0.000125f, //social decrement multiplier 7
            0.25f, //ent dec awake 8
            1f, //ent mult asleep 9
            0.17f, //hyg decrement awake 10
            0.08f, //hyg decrement asleep 11
            0.3f, //blad decrement awake 12
            0.15f, //blad decrement asleep 13
            0.4f, //comfort decrement active 14
            0.6f, //comfort decrement lazy 15
            0.5f, //comfort decrement 16
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
            if (context.Clock.Minutes/2 == LastMinute) return;
            LastMinute = context.Clock.Minutes/2;
            var sleeping = (avatar.GetMotiveData(VMMotive.SleepState) != 0);

            int moodSum = 0;

            for (int i = 0; i < 7; i++)
            {
                int frac = 0;
                var dm = DecrementMotives[i];
                if (avatar.HasMotiveChange(dm) && dm != VMMotive.Energy) continue;
                var motive = avatar.GetMotiveData(dm);
                var r_Hunger = ToFixed1000(Constants[5]) * (100 + avatar.GetMotiveData(VMMotive.Hunger));
                switch (i)
                {
                    case 0:
                        frac = r_Hunger; break;
                    case 1:
                        var active = avatar.GetPersonData(VMPersonDataVariable.ActivePersonality);
                        if (active > 666)
                            frac = ToFixed1000(Constants[14]);
                        else if (active < 666)
                            frac = ToFixed1000(Constants[15]);
                        else
                            frac = ToFixed1000(Constants[16]);
                        break;
                    case 2:
                        frac = ToFixed1000(Constants[sleeping ? 11 : 10]); break;
                    case 3:
                        frac = ToFixed1000(Constants[sleeping ? 13 : 12]) + FracMul(r_Hunger, ToFixed1000(Constants[4])); break;
                    case 4:
                        if (sleeping)
                        {
                            frac = (context.Clock.Hours >= Constants[2]) ? ToFixed1000(Constants[3]) : 0;
                        } else
                        {
                            frac = (ToFixed1000(Constants[0]) / (30 * (int)Constants[1]));
                        }
                        //energy span over wake hours. small energy drift applied if asleep during the day.
                        break;
                    case 5:
                        frac = (sleeping)?0:ToFixed1000(Constants[8]);
                        break;
                    case 6:
                        frac = ToFixed1000(Constants[6]) +
                            ToFixed1000(Constants[7]) * avatar.GetPersonData(VMPersonDataVariable.OutgoingPersonality);
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
            for (int i = 0; i < 7; i++)
                MotiveFractions[i] = reader.ReadInt16();
        }
    }
}
