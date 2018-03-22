/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Common.Utils;
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

        public int FramesPerSecond
        {
            get
            {
                return (int)Math.Round(NumFrames / (Duration / 1000));
            }
        }

        /// <summary>
        /// Total number of frames in this animation.
        /// </summary>
        public int NumFrames
        {
            get
            {
                var result = 0;
                foreach (var motion in Motions)
                {
                    result = (int)Math.Max(result, motion.FrameCount);
                }
                return result;
            }
        }

        /// <summary>
        /// Reads an animation from a stream.
        /// </summary>
        /// <param name="stream">The Stream instance to read from.</param>
        public void Read(BCFReadProxy io, bool bcf)
        {

                if (!bcf)
                {
                    var version = io.ReadUInt32();
                }

                if (bcf)
                {
                    Name = io.ReadPascalString();
                    XSkillName = io.ReadPascalString();
                }
                else
                {
                    Name = io.ReadLongPascalString();
                }
                Duration = io.ReadFloat();
                Distance = io.ReadFloat();
                IsMoving = (bcf)?((byte)io.ReadInt32()):io.ReadByte();

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
                Motions = new AnimationMotion[motionCount];
                for (var i = 0; i < motionCount; i++){
                    var motion = new AnimationMotion();
                    if (!bcf)
                    {
                        var unknown = io.ReadUInt32();
                    }
                    motion.BoneName = io.ReadPascalString();
                    motion.FrameCount = io.ReadUInt32();
                    motion.Duration = io.ReadFloat();
                    motion.HasTranslation = (((bcf) ? io.ReadInt32() : io.ReadByte()) == 1);
                    motion.HasRotation = (((bcf) ? io.ReadInt32() : io.ReadByte()) == 1);
                    motion.FirstTranslationIndex = io.ReadUInt32();
                    motion.FirstRotationIndex = io.ReadUInt32();

                    var hasPropsList = bcf || io.ReadByte() == 1;
                    if (hasPropsList)
                    {
                        var propListCount = io.ReadUInt32();
                        var props = new PropertyList[propListCount];
                        for (var x = 0; x < propListCount; x++){
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
                                var id = io.ReadUInt32();
                                list.Items[y] = new TimePropertyListItem {
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
        public uint FirstTranslationIndex;
        public uint FirstRotationIndex;

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
        public uint ID;
        public PropertyList Properties;
    }
}
