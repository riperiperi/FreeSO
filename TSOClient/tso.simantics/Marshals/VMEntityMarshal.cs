using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.LotView.Model;
using FSO.LotView;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using FSO.SimAntics.Model.TS1Platform;
using FSO.SimAntics.Model.Platform;

namespace FSO.SimAntics.Marshals
{
    public class VMEntityMarshal : VMSerializable
    {
        public short ObjectID;
        public uint PersistID;
        public VMPlatformState PlatformState;
        public short[] ObjectData;
        public short[] MyList;

        public VMRuntimeHeadlineMarshal Headline;

        public uint GUID;
        public uint MasterGUID;

        public short MainParam; //parameters passed to main on creation.
        public short MainStackOBJ;

        public short[] Contained; //object ids
        public short Container;
        public short ContainerSlot;

        public short[] Attributes;
        public VMEntityRelationshipMarshal[] MeToObject;
        public VMEntityPersistRelationshipMarshal[] MeToPersist;

        public ulong DynamicSpriteFlags;
        public ulong DynamicSpriteFlags2;
        public LotTilePos Position;

        public uint TimestampLockoutCount;
        public Color LightColor = Color.White;

        public int Version;
        public bool TS1;

        //runtime
        public bool LoadFailed;

        public VMEntityMarshal() { }
        public VMEntityMarshal(int version, bool ts1) { Version = version; TS1 = ts1; }

        public virtual void Deserialize(BinaryReader reader)
        {
            ObjectID = reader.ReadInt16();
            PersistID = reader.ReadUInt32();

            if (this is VMGameObjectMarshal) PlatformState = (TS1)? (VMAbstractEntityState)new VMTS1ObjectState(Version):new VMTSOObjectState(Version);
            else PlatformState = (TS1) ? (VMAbstractEntityState)new VMTS1AvatarState(Version):new VMTSOAvatarState(Version);

            PlatformState.Deserialize(reader);

            var datas = reader.ReadInt32();
            ObjectData = new short[datas];
            for (int i = 0; i < datas; i++) ObjectData[i] = reader.ReadInt16();

            var listLen = reader.ReadInt32();
            MyList = new short[listLen];
            for (int i = 0; i < listLen; i++) MyList[i] = reader.ReadInt16();

            if (reader.ReadBoolean())
            {
                Headline = new VMRuntimeHeadlineMarshal();
                Headline.Deserialize(reader);
            }

            GUID = reader.ReadUInt32();
            MasterGUID = reader.ReadUInt32();

            MainParam = reader.ReadInt16();
            MainStackOBJ = reader.ReadInt16();

            var contN = reader.ReadInt32();
            Contained = new short[contN];
            for (int i = 0; i < contN; i++) Contained[i] = reader.ReadInt16();
            Container = reader.ReadInt16();
            ContainerSlot = reader.ReadInt16();

            var attrN = reader.ReadInt32();
            Attributes = new short[attrN];
            for (int i = 0; i < attrN; i++) Attributes[i] = reader.ReadInt16();

            var relN = reader.ReadInt32();
            MeToObject = new VMEntityRelationshipMarshal[relN];
            for (int i = 0; i < relN; i++) {
                MeToObject[i] = new VMEntityRelationshipMarshal();
                MeToObject[i].Deserialize(reader);
            }

            if (Version > 7)
            {
                var prelN = reader.ReadInt32();
                MeToPersist = new VMEntityPersistRelationshipMarshal[prelN];
                for (int i = 0; i < prelN; i++)
                {
                    MeToPersist[i] = new VMEntityPersistRelationshipMarshal();
                    MeToPersist[i].Deserialize(reader);
                }
            }
            else MeToPersist = new VMEntityPersistRelationshipMarshal[0];

            DynamicSpriteFlags = reader.ReadUInt64();
            if (Version > 2) DynamicSpriteFlags2 = reader.ReadUInt64();
            Position = new LotTilePos();
            Position.Deserialize(reader);

            if (Version > 16)
            {
                TimestampLockoutCount = reader.ReadUInt32();
            }

            if (Version > 19)
            {
                LightColor = new Color(reader.ReadUInt32());
            }
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(PersistID);
            PlatformState.SerializeInto(writer);
            writer.Write(ObjectData.Length);
            writer.Write(VMSerializableUtils.ToByteArray(ObjectData));
            //foreach (var item in ObjectData) writer.Write(item);
            writer.Write(MyList.Length);
            writer.Write(VMSerializableUtils.ToByteArray(MyList));
            //foreach (var item in MyList) writer.Write(item);

            writer.Write(Headline != null);
            if (Headline != null) Headline.SerializeInto(writer);

            writer.Write(GUID);
            writer.Write(MasterGUID);

            writer.Write(MainParam); //parameters passed to main on creation.
            writer.Write(MainStackOBJ);

            writer.Write(Contained.Length); //object ids
            writer.Write(VMSerializableUtils.ToByteArray(Contained));
            //foreach (var item in Contained) writer.Write(item);
            writer.Write(Container);
            writer.Write(ContainerSlot);

            writer.Write(Attributes.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Attributes));
            //foreach (var item in Attributes) writer.Write(item);
            writer.Write(MeToObject.Length);
            foreach (var item in MeToObject) item.SerializeInto(writer);
            writer.Write(MeToPersist.Length);
            foreach (var item in MeToPersist) item.SerializeInto(writer);

            writer.Write(DynamicSpriteFlags); /** Used to show/hide dynamic sprites **/
            writer.Write(DynamicSpriteFlags2);
            Position.SerializeInto(writer);

            writer.Write(TimestampLockoutCount);
            writer.Write(LightColor.PackedValue);
        }
    }

    public class VMEntityRelationshipMarshal : VMSerializable
    {
        public ushort Target;
        public short[] Values;

        public virtual void Deserialize(BinaryReader reader)
        {
            Target = reader.ReadUInt16();
            var rels = reader.ReadInt32();
            Values = new short[rels];
            for (int i=0; i<rels; i++)
            {
                Values[i] = reader.ReadInt16();
            }
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Target);
            writer.Write(Values.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Values));
        }
    }

    public class VMEntityPersistRelationshipMarshal : VMSerializable
    {
        public uint Target;
        public short[] Values;

        public virtual void Deserialize(BinaryReader reader)
        {
            Target = reader.ReadUInt32();
            var rels = reader.ReadInt32();
            Values = new short[rels];
            for (int i = 0; i < rels; i++)
            {
                Values[i] = reader.ReadInt16();
            }
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Target);
            writer.Write(Values.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Values));
        }
    }
}
