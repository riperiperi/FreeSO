﻿using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Marshals
{
    public class VMAvatarMarshal : VMEntityMarshal
    {
        public VMAnimationStateMarshal[] Animations;
        public VMAnimationStateMarshal CarryAnimationState; //NULLable

        public string Message = "";

        public int MessageTimeout;
        
        public VMMotiveChange[] MotiveChanges = new VMMotiveChange[16];
        public VMIMotiveDecay MotiveDecay;
        public short[] PersonData = new short[101];
        public short[] MotiveData = new short[16];
        public short HandObject;
        public float RadianDirection;
        public int KillTimeout = -1; //version 2

        public VMAvatarDefaultSuits DefaultSuits;
        public VMAvatarDynamicSuits DynamicSuits;
        public VMAvatarDecoration Decoration;
        public string[] BoundAppearances;

        public VMOutfitReference BodyOutfit;
        public VMOutfitReference HeadOutfit;
        public AppearanceType SkinTone;

        public VMAvatarMarshal() { }
        public VMAvatarMarshal(int version, bool ts1) : base(version, ts1) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            var anims = reader.ReadInt32();
            Animations = new VMAnimationStateMarshal[anims];
            for (int i = 0; i < anims; i++) Animations[i] = new VMAnimationStateMarshal(reader);

            var carry = reader.ReadBoolean();
            if (carry) CarryAnimationState = new VMAnimationStateMarshal(reader);

            Message = reader.ReadString();
            MessageTimeout = reader.ReadInt32();

            var motCs = reader.ReadInt32();
            MotiveChanges = new VMMotiveChange[motCs];
            for (int i = 0; i < motCs; i++)
            {
                MotiveChanges[i] = new VMMotiveChange();
                MotiveChanges[i].Deserialize(reader);
            }
            MotiveDecay = (TS1) ? (VMIMotiveDecay)new VMTS1MotiveDecay() : new VMAvatarMotiveDecay();
            MotiveDecay.Deserialize(reader);

            var pdats = reader.ReadInt32();
            PersonData = new short[Math.Max(101,pdats)];
            for (int i = 0; i < pdats; i++) PersonData[i] = reader.ReadInt16();

            var mdats = reader.ReadInt32();
            MotiveData = new short[mdats];
            for (int i = 0; i < mdats; i++) MotiveData[i] = reader.ReadInt16();

            HandObject = reader.ReadInt16();
            RadianDirection = reader.ReadSingle();

            if (Version > 1) KillTimeout = reader.ReadInt32();

            DefaultSuits = new VMAvatarDefaultSuits(reader);

            if(Version >= 15)
            {
                DynamicSuits = new VMAvatarDynamicSuits(reader);
                Decoration = new VMAvatarDecoration(reader);
            } else
            {
                DynamicSuits = new VMAvatarDynamicSuits(false);
                Decoration = new VMAvatarDecoration();
            }

            var aprs = reader.ReadInt32();
            BoundAppearances = new string[aprs];
            for (int i = 0; i < aprs; i++) BoundAppearances[i] = reader.ReadString();

            BodyOutfit = new VMOutfitReference(reader);
            HeadOutfit = new VMOutfitReference(reader);
            SkinTone = (AppearanceType)reader.ReadByte();
        }
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(Animations.Length);
            foreach (var item in Animations) { item.SerializeInto(writer); }

            writer.Write(CarryAnimationState != null);
            if (CarryAnimationState != null) CarryAnimationState.SerializeInto(writer);

            writer.Write(Message);
            writer.Write(MessageTimeout);

            writer.Write(MotiveChanges.Length);
            foreach (var item in MotiveChanges) { item.SerializeInto(writer); }
            MotiveDecay.SerializeInto(writer);
            writer.Write(PersonData.Length);
            writer.Write(VMSerializableUtils.ToByteArray(PersonData));
            //foreach (var item in PersonData) { writer.Write(item); }
            writer.Write(MotiveData.Length);
            writer.Write(VMSerializableUtils.ToByteArray(MotiveData));
            //foreach (var item in MotiveData) { writer.Write(item); }
            writer.Write(HandObject);
            writer.Write(RadianDirection);

            writer.Write(KillTimeout);

            DefaultSuits.SerializeInto(writer);
            DynamicSuits.SerializeInto(writer);
            Decoration.SerializeInto(writer);

            writer.Write(BoundAppearances.Length);
            foreach (var item in BoundAppearances) { writer.Write(item); }

            if (BodyOutfit == null) writer.Write((ulong)0);
            else BodyOutfit.SerializeInto(writer);
            if (HeadOutfit == null) writer.Write((ulong)0);
            else HeadOutfit.SerializeInto(writer);
            writer.Write((byte)SkinTone);
        }
    }
}
