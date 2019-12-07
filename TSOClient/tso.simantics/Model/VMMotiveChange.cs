﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files;
using FSO.SimAntics.Entities;

namespace FSO.SimAntics.Model
{
    public class VMMotiveChange : VMSerializable
    {
        public short PerHourChange;
        public short MaxValue;
        public VMMotive Motive;
        private double fractional;
        public bool Ticked;

        public static TuningEntry? LotMotives;

        public void Clear()
        {
            PerHourChange = 0;
            MaxValue = short.MaxValue;
        }

        public void Tick(VMAvatar avatar)
        {
            Ticked = true;
            if (PerHourChange != 0)
            {
                
                double rate = (PerHourChange/60.0)/(30.0); //timed for 5 second minutes
                if (!Content.Content.Get().TS1) rate /= 5;
                fractional += rate;
                if (Math.Abs(fractional) >= 1)
                {
                    var motive = avatar.GetMotiveData(Motive);
                    if (((rate > 0) && (motive > MaxValue)) || ((rate < 0) && (motive < MaxValue))) { return; } //we're already over, do nothing. (do NOT clamp)
                    motive += (short)(fractional);
                    fractional %= 1.0;

                    if (((rate > 0) && (motive > MaxValue)) || ((rate < 0) && (motive < MaxValue))) { motive = MaxValue; }
                    //DO NOT CLEAR MOTIVE WHEN IT HITS MAX VALUE! fixes pet, maybe shower.
                    avatar.SetMotiveData(Motive, motive);
                }
            }
        }

        public static int ScaleRate(VM vm, int rate, VMMotive type)
        {
            if (vm.TS1)
            {
                if (type == VMMotive.Energy && rate > 0)
                {
                    rate *= (int)VMTS1MotiveDecay.Constants[0] / (24 - (int)VMTS1MotiveDecay.Constants[1]);
                }
                return rate;
            } else
            {
                if (rate < 0) return rate;
                if (LotMotives == null) LotMotives = Content.Content.Get().GlobalTuning.EntriesByName["lotmotives"];
                if (vm.TSOState.PropertyCategory == 4 && type > 0) rate = (rate * 3) / 2; //1.5x gain multiplier on services lots
                if (VMMotive.Comfort == type) return rate;
                var ind = Array.IndexOf(VMAvatarMotiveDecay.DecrementMotives, type);
                var cat = vm.TSOState.PropertyCategory;
                if (cat > 10) cat = 0;
                string category = VMAvatarMotiveDecay.CategoryNames[cat];
                var weight = ToFixed1000(LotMotives.Value.GetNum(category + "_" + VMAvatarMotiveDecay.LotMotiveNames[ind] + "Weight"));
                return (rate * 1000) / weight;
            }
        }

        public static short ScaleMax(VM vm, short oldMax, VMMotive type)
        {
            return (short)((oldMax - 100) + vm.TuningCache.GetLimit(type));
        }

        private static int ToFixed1000(float input)
        {
            return (int)(input * 1000);
        }

        private static int FracMul(int input, int frac)
        {
            return (int)((long)input * frac) / 1000;
        }


        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(PerHourChange);
            writer.Write(MaxValue);
            writer.Write((byte)Motive);
            writer.Write(fractional);
        }

        public void Deserialize(BinaryReader reader)
        {
            PerHourChange = reader.ReadInt16();
            MaxValue = reader.ReadInt16();
            Motive = (VMMotive)reader.ReadByte();
            fractional = reader.ReadDouble();
        }
    }
}
