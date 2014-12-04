/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Common.utils;
using Microsoft.Xna.Framework;
using TSO.Files.utils;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Represents an animation for a model.
    /// </summary>
    public class Animation
    {
        public string Name;
        public float Duration;
        public float Distance;
        public byte IsMoving;
        public Vector3[] Translations;
        public Quaternion[] Rotations;
        public AnimationMotion[] Motions;

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
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();
                Name = io.ReadLongPascalString();
                Duration = io.ReadFloat();
                Distance = io.ReadFloat();
                IsMoving = io.ReadByte();

                var translationCount = io.ReadUInt32();
                Translations = new Vector3[translationCount];
                for (var i = 0; i < translationCount; i++){
                    Translations[i] = new Vector3 {
                        X = -io.ReadFloat(),
                        Y = io.ReadFloat(),
                        Z = io.ReadFloat()
                    };
                }

                var rotationCount = io.ReadUInt32();
                Rotations = new Quaternion[rotationCount];
                for (var i = 0; i < rotationCount; i++){
                    Rotations[i] = new Quaternion {
                        X = io.ReadFloat(),
                        Y = -io.ReadFloat(),
                        Z = -io.ReadFloat(),
                        W = -io.ReadFloat()
                    };
                }

                var motionCount = io.ReadUInt32();
                Motions = new AnimationMotion[motionCount];
                for (var i = 0; i < motionCount; i++){
                    var motion = new AnimationMotion();
                    var unknown = io.ReadUInt32();
                    motion.BoneName = io.ReadPascalString();
                    motion.FrameCount = io.ReadUInt32();
                    motion.Duration = io.ReadFloat();
                    motion.HasTranslation = (io.ReadByte() == 1);
                    motion.HasRotation = (io.ReadByte() == 1);
                    motion.FirstTranslationIndex = io.ReadUInt32();
                    motion.FirstRotationIndex = io.ReadUInt32();

                    var hasPropsList = io.ReadByte() == 1;
                    if (hasPropsList)
                    {
                        var propListCount = io.ReadUInt32();
                        var props = new PropertyList[propListCount];
                        for (var x = 0; x < propListCount; x++){
                            props[x] = ReadPropertyList(io);
                        }
                        motion.Properties = props;
                    }

                    var hasTimeProps = io.ReadByte() == 1;
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
                                    Properties = ReadPropertyList(io)
                                };
                            }
                            timePropsList[x] = list;
                        }
                        motion.TimeProperties = timePropsList;
                    }

                    Motions[i] = motion;
                }
            }
        }

        /// <summary>
        /// Reads a property list from a stream.
        /// </summary>
        /// <param name="io">IOBuffer instance used to read an animation.</param>
        /// <returns>A PropertyList instance.</returns>
        private PropertyList ReadPropertyList(IoBuffer io)
        {
            var propsCount = io.ReadUInt32();
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
