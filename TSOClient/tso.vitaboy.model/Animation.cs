using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FSO.Files.Utils;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Represents an animation for a model.
    /// </summary>
    public class Animation
    {
        public string Name;
        public string XSkillName;
        public float Duration;
        public float Distance;
        public byte IsMoving;

        public uint TranslationCount;
        public uint RotationCount;

        public Vector3[] Translations;
        public Quaternion[] Rotations;
        public AnimationMotion[] Motions;

        public int FramesPerSecond { get; internal set; }

        /// <summary>
        /// Total number of frames in this animation.
        /// </summary>
        public int NumFrames { get; set; }

        public BCF ParentBCF;

        /// <summary>
        /// Reads an animation from a stream.
        /// </summary>
        /// <param name="stream">The Stream instance to read from.</param>
        public void Read(BCFReadProxy io, bool bcf)
        {
            if (bcf)
            {
                Name = io.ReadPascalString();
                XSkillName = io.ReadPascalString();
            }
            else
            {
                var version = io.ReadUInt32();
                Name = io.ReadLongPascalString();
            }
            Duration = io.ReadFloat();
            Distance = io.ReadFloat();
            IsMoving = (bcf) ? ((byte)io.ReadInt32()) : io.ReadByte();

            TranslationCount = io.ReadUInt32();
            if (!bcf)
            {
                Translations = new Vector3[TranslationCount];
                for (var i = 0; i < TranslationCount; i++)
                {
                    Translations[i] = new Vector3
                    {
                        X = -io.ReadFloat(),
                        Y = io.ReadFloat(),
                        Z = io.ReadFloat()
                    };
                }
            }

            RotationCount = io.ReadUInt32();
            if (!bcf)
            {
                Rotations = new Quaternion[RotationCount];
                for (var i = 0; i < RotationCount; i++)
                {
                    Rotations[i] = new Quaternion
                    {
                        X = io.ReadFloat(),
                        Y = -io.ReadFloat(),
                        Z = -io.ReadFloat(),
                        W = -io.ReadFloat()
                    };
                }
            }

            var motionCount = io.ReadUInt32();
            NumFrames = 0;
            Motions = new AnimationMotion[motionCount];
            for (var i = 0; i < motionCount; i++)
            {
                var motion = new AnimationMotion();
                if (!bcf)
                {
                    var unknown = io.ReadUInt32();
                }
                motion.BoneName = io.ReadPascalString();
                motion.FrameCount = io.ReadUInt32();
                if (motion.FrameCount > NumFrames) NumFrames = (int)motion.FrameCount;
                motion.Duration = io.ReadFloat();
                motion.HasTranslation = (((bcf) ? io.ReadInt32() : io.ReadByte()) == 1);
                motion.HasRotation = (((bcf) ? io.ReadInt32() : io.ReadByte()) == 1);
                motion.FirstTranslationIndex = io.ReadInt32();
                motion.FirstRotationIndex = io.ReadInt32();

                var hasPropsList = bcf || io.ReadByte() == 1;
                if (hasPropsList)
                {
                    var propListCount = io.ReadUInt32();
                    var props = new PropertyList[propListCount];
                    for (var x = 0; x < propListCount; x++)
                    {
                        props[x] = ReadPropertyList(io, bcf);
                    }
                    motion.Properties = props;
                }

                var hasTimeProps = bcf || io.ReadByte() == 1;
                if (hasTimeProps)
                {
                    var timePropsListCount = io.ReadUInt32();
                    var timePropsList = new TimePropertyList[timePropsListCount];

                    for (var x = 0; x < timePropsListCount; x++)
                    {
                        var list = new TimePropertyList();
                        var timePropsCount = io.ReadUInt32();
                        list.Items = new TimePropertyListItem[timePropsCount];
                        for (var y = 0; y < timePropsCount; y++)
                        {
                            var id = io.ReadInt32();
                            list.Items[y] = new TimePropertyListItem
                            {
                                ID = id,
                                Properties = ReadPropertyList(io, bcf)
                            };
                        }
                        timePropsList[x] = list;
                    }
                    motion.TimeProperties = timePropsList;
                }

                Motions[i] = motion;
            }
            UpdateFPS();
        }

        public void UpdateFPS()
        {
            FramesPerSecond = (int)Math.Round(NumFrames / (Duration / 1000));
        }

        public void Write(BCFWriteProxy io, bool bcf)
        {
            if (bcf)
            {
                io.WritePascalString(Name);
                io.WritePascalString(XSkillName);
            }
            else
            {
                io.WriteUInt32(2);
                io.WriteLongPascalString(Name);
            }
            io.WriteFloat(Duration);
            io.WriteFloat(Distance);
            if (bcf) io.WriteInt32(IsMoving);
            else io.WriteByte(IsMoving);

            io.WriteUInt32(TranslationCount);
            io.SetGrouping(3);
            if (!bcf)
            {
                for (var i = 0; i < TranslationCount; i++)
                {
                    var trans = Translations[i];
                    io.WriteFloat(-trans.X);
                    io.WriteFloat(trans.Y);
                    io.WriteFloat(trans.Z);
                }
            }
            io.SetGrouping(1);

            io.WriteUInt32(RotationCount);
            io.SetGrouping(4);
            if (!bcf)
            {
                for (var i = 0; i < RotationCount; i++)
                {
                    var rot = Rotations[i];
                    io.WriteFloat(rot.X);
                    io.WriteFloat(-rot.Y);
                    io.WriteFloat(-rot.Z);
                    io.WriteFloat(-rot.W);
                }
            }
            io.SetGrouping(1);

            io.WriteUInt32((uint)Motions.Length);
            foreach (var motion in Motions)
            {
                if (!bcf) io.WriteUInt32(0); //unknown
                io.WritePascalString(motion.BoneName);
                io.WriteUInt32(motion.FrameCount);
                io.WriteFloat(motion.Duration);
                if (bcf)
                {
                    io.WriteInt32(motion.HasTranslation ? 1 : 0);
                    io.WriteInt32(motion.HasRotation ? 1 : 0);
                }
                else
                {
                    io.WriteByte((byte)(motion.HasTranslation ? 1 : 0));
                    io.WriteByte((byte)(motion.HasRotation ? 1 : 0));
                }
                io.WriteInt32(motion.FirstTranslationIndex);
                io.WriteInt32(motion.FirstRotationIndex);

                if (!bcf)
                {
                    io.WriteByte(1); //has props
                }
                //write props list
                var props = motion.Properties;
                io.WriteUInt32((uint)props.Length);
                foreach (var prop in props)
                {
                    //write property list
                    WritePropertyList(prop, io, bcf);
                }

                if (!bcf)
                {
                    io.WriteByte(1); //has time props
                }
                //write time props list
                var timePropsList = motion.TimeProperties;
                io.WriteUInt32((uint)timePropsList.Length);
                foreach (var list in timePropsList)
                {
                    io.WriteUInt32((uint)list.Items.Length);
                    foreach (var item in list.Items)
                    {
                        io.WriteInt32(item.ID);
                        WritePropertyList(item.Properties, io, bcf);
                    }
                }
            }
        }

        /// <summary>
        /// Reads a property list from a stream.
        /// </summary>
        /// <param name="io">IOBuffer instance used to read an animation.</param>
        /// <returns>A PropertyList instance.</returns>
        private PropertyList ReadPropertyList(BCFReadProxy io, bool shortPairs)
        {
            var propsCount = (shortPairs) ? 1 : io.ReadUInt32();
            var result = new PropertyListItem[propsCount];

            for (var y = 0; y < propsCount; y++)
            {
                var item = new PropertyListItem();
                var pairsCount = io.ReadUInt32();
                for (var z = 0; z < pairsCount; z++)
                {
                    item.KeyPairs.Add(new KeyValuePair<string, string>(
                        io.ReadPascalString(),
                        io.ReadPascalString()
                    ));
                }
                result[y] = item;
            }

            return new PropertyList {
                Items = result
            };
        }

        private void WritePropertyList(PropertyList props, BCFWriteProxy io, bool shortPairs)
        {
            if (!shortPairs) io.WriteUInt32((uint)props.Items.Length);
            foreach (var prop in props.Items)
            {
                io.WriteUInt32((uint)prop.KeyPairs.Count);
                foreach (var pair in prop.KeyPairs)
                {
                    io.WritePascalString(pair.Key);
                    io.WritePascalString(pair.Value);
                }
            }
        }
    }

    /// <summary>
    /// An animation consists of a number of motions that each move a bone.
    /// A motion can have properties associated with it, enumerated in a 
    /// time property list, which has a list of properties.
    /// </summary>
    public class AnimationMotion
    {
        public string BoneName;
        public uint FrameCount;
        public float Duration;
        public bool HasTranslation;
        public bool HasRotation;
        public int FirstTranslationIndex;
        public int FirstRotationIndex;

        public PropertyList[] Properties;
        public TimePropertyList[] TimeProperties;
    }

    /// <summary>
    /// Lists properties associated with a motion.
    /// </summary>
    public class PropertyList
    {
        public PropertyListItem[] Items;

        /// <summary>
        /// Gets a PropertyList TimePropertyListItem instance
        /// from this PropertyList instance.
        /// </summary>
        /// <param name="key">The key of a PropertyListItem (see Bones.cs)</param>
        /// <returns>A string, which is the value of the PropertyListItem.</returns>
        public string this[string key]
        {
            get
            {
                foreach (var item in Items)
                {
                    foreach (var keypair in item.KeyPairs)
                    {
                        if (keypair.Key == key)
                        {
                            return keypair.Value;
                        }
                    }
                }
                return null;
            }
        }
    }

    /// <summary>
    /// Lists property lists associated with a motion.
    /// </summary>
    public class TimePropertyList
    {
        public TimePropertyListItem[] Items;
    }

    /// <summary>
    /// Stores PropertyList instances and associates them with an ID.
    /// </summary>
    public class TimePropertyListItem
    {
        public int ID;
        public PropertyList Properties;
    }
}
