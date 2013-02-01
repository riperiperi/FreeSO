/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Dressup
{
    /// <summary>
    /// List of properties in a motion.
    /// </summary>
    public class PropertyList
    {
        public uint PropsCount;
        public List<Property> PList = new List<Property>();
    }

    /// <summary>
    /// A property can be part of a motion, and
    /// contains two strings. A property can be
    /// used to define, for instance, a specific
    /// sound to be played for a particular motion.
    /// </summary>
    public class Property
    {
        public string Key, Value;
    }

    /// <summary>
    /// A list of time properties in a motion.
    /// </summary>
    public class TimePropertyList
    {
        public uint TimePropsCount;
        public List<TimeProperty> TList = new List<TimeProperty>();
    }

    /// <summary>
    /// A time property can be part of a motion,
    /// and contains an ID and a list of properties.
    /// A time proprty can be used to define, for instance,
    /// a sound to be played for a specific length of time
    /// as a motion is performed.
    /// </summary>
    public class TimeProperty
    {
        public uint TPropID;
        public PropertyList PList = new PropertyList();
    }

    /// <summary>
    /// An animation consists of a list of motions, each of which contains
    /// a set of translations and rotations that are applied to a character's
    /// skeleton's bone in order to create an animation.
    /// </summary>
    public class Motion
    {
        public uint Unknown;    //1
        public string BoneName;
        public uint NumFrames; //A 4-byte unsigned integer specifying the number of frames used for this motion.
        public float Duration; //A 4-byte float in little-endian byte order specifying the duration of this motion in secs.
        
        public byte HasTranslation;
        public byte HasRotation;

        public uint TranslationsOffset;
        public uint RotationsOffset;
        public float[,] Translations;
        public float[,] Rotations;
        
        public byte HasPropertyLists;
        public List<PropertyList> PropertyLists = new List<PropertyList>();

        public byte HasTimePropertyLists;
        public List<TimePropertyList> TimePropertyLists = new List<TimePropertyList>();
    }

    /// <summary>
    /// Represents a 3D animation.
    /// </summary>
    public class Anim
    {
        //Should be equal to 2 for TSO animations.
        private uint m_Version;
        //A Pascal string with 2 string length bytes specifying the name of the animation contained in this file.
        private string m_Name;
        //A 4-byte float in little-endian byte order specifying the duration of this animation in seconds.
        private float m_Duration;
        //A 4-byte float in little-endian byte order specifying the distance to move the character while
        //performing this animation.
        private float m_Distance;
        //One byte specifying whether or not this animation will move the character.
        private byte m_IsMoving;
        //A 2-byte unsigned integer specifying the total number of translations contained in this animation.
        private uint m_NumTranslations;
        private long m_TranslationsTableOffset;
        //A 2-byte unsigned integer specifying the total number of rotations contained in this animation.
        private uint m_NumRotations;
        private long m_RotationsTableOffset;

        //A 4-byte unsigned integer specifying the number of motions to follow.
        private uint m_MotionCount;
        private List<Motion> m_Motions = new List<Motion>();

        public List<Motion> Motions
        {
            get { return m_Motions; }
        }

        /// <summary>
        /// Reads a *.anim file into this Anim instance.
        /// </summary>
        /// <param name="FileData">The filedata for the *.anim file.</param>
        public Anim(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Name = Encoding.ASCII.GetString(Reader.ReadBytes(Endian.SwapInt16(Reader.ReadInt16())));
            m_Duration = Reader.ReadSingle() / 1000; //Why does this have to be divided by 1000? o_O
            m_Distance = Reader.ReadSingle();
            m_IsMoving = Reader.ReadByte();

            m_NumTranslations = Endian.SwapUInt32(Reader.ReadUInt32());
            m_TranslationsTableOffset = Reader.BaseStream.Position;

            Reader.BaseStream.Seek(m_TranslationsTableOffset + 12 * m_NumTranslations, SeekOrigin.Begin);

            m_NumRotations = Endian.SwapUInt32(Reader.ReadUInt32());
            m_RotationsTableOffset = Reader.BaseStream.Position;

            Reader.BaseStream.Seek(m_RotationsTableOffset + 16 * m_NumRotations, SeekOrigin.Begin);

            m_MotionCount = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int i = 0; i < m_MotionCount; i++)
                m_Motions.Add(ReadMotion(Reader));
        }

        /// <summary>
        /// Reads a motion from a *.anim file.
        /// </summary>
        /// <param name="Reader">The BinaryReader instance used to read the *.anim file.</param>
        /// <returns>A Motion instance.</returns>
        private Motion ReadMotion(BinaryReader Reader)
        {
            Motion Mot = new Motion();

            Mot.Unknown = Endian.SwapUInt32(Reader.ReadUInt32());
            Mot.BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
            Mot.NumFrames = Endian.SwapUInt32(Reader.ReadUInt32());
            Mot.Duration = Reader.ReadSingle() / 1000;

            Mot.HasTranslation = Reader.ReadByte();
            Mot.HasRotation = Reader.ReadByte();
            Mot.TranslationsOffset = Endian.SwapUInt32(Reader.ReadUInt32());
            Mot.RotationsOffset = Endian.SwapUInt32(Reader.ReadUInt32());

            if (Mot.HasTranslation != 0)
            {
                Mot.Translations = new float[Mot.NumFrames, 3];
                long CurrentOffset = Reader.BaseStream.Position;
                Reader.BaseStream.Seek(m_TranslationsTableOffset + 12 * Mot.TranslationsOffset, SeekOrigin.Begin);

                for (int i = 0; i < Mot.NumFrames; i++)
                {
                    Mot.Translations[i, 0] = Reader.ReadSingle();
                    Mot.Translations[i, 1] = Reader.ReadSingle();
                    Mot.Translations[i, 2] = Reader.ReadSingle();
                }

                Reader.BaseStream.Seek(CurrentOffset, SeekOrigin.Begin);
            }

            if (Mot.HasRotation != 0)
            {
                Mot.Rotations = new float[Mot.NumFrames, 4];
                long CurrentOffset = Reader.BaseStream.Position;
                Reader.BaseStream.Seek(m_RotationsTableOffset + 16 * Mot.RotationsOffset, SeekOrigin.Begin);

                for (int i = 0; i < Mot.NumFrames; i++)
                {
                    Mot.Rotations[i, 0] = Reader.ReadSingle();
                    Mot.Rotations[i, 1] = Reader.ReadSingle();
                    Mot.Rotations[i, 2] = Reader.ReadSingle();
                    Mot.Rotations[i, 3] = Reader.ReadSingle();
                }

                Reader.BaseStream.Seek(CurrentOffset, SeekOrigin.Begin);
            }

            Mot.HasPropertyLists = Reader.ReadByte();

            if (Mot.HasPropertyLists != 0)
            {
                ReadPropertyLists(ref Mot, ref Reader);
            }

            Mot.HasTimePropertyLists = Reader.ReadByte();

            if (Mot.HasTimePropertyLists != 0)
            {
                ReadTimePropertyLists(ref Mot, ref Reader);
            }

            return Mot;
        }

        /// <summary>
        /// Reads all the property lists in a motion.
        /// </summary>
        /// <param name="Mot">The motion that the propertylists belong to.</param>
        /// <param name="Reader">The BinaryReader instance used to read the *.anim file.</param>
        private void ReadPropertyLists(ref Motion Mot, ref BinaryReader Reader)
        {
            uint Count = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int i = 0; i < Count; i++)
                Mot.PropertyLists.Add(ReadPropList(Reader));
        }

        /// <summary>
        /// Reads a list of properties from the *.anim file.
        /// </summary>
        /// <param name="Reader">The BinaryReader instance used to read the *.anim file.</param>
        /// <returns>A PropertyList instance filled with properties.</returns>
        private PropertyList ReadPropList(BinaryReader Reader)
        {
            PropertyList PList = new PropertyList();
            PList.PropsCount = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int j = 0; j < PList.PropsCount; j++)
            {
                Property Prop = new Property();

                uint PairsCount = Endian.SwapUInt32(Reader.ReadUInt32());

                for (int k = 0; k < PairsCount; k++)
                {
                    Prop.Key = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                    Prop.Value = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                }

                PList.PList.Add(Prop);
            }

            return PList;
        }

        /// <summary>
        /// Reads all the timeproperty lists in a motion.
        /// </summary>
        /// <param name="Mot">The motion that the timeproperty lists belong to.</param>
        /// <param name="Reader">The BinaryReader instance used to read the *.anim file.</param>
        private void ReadTimePropertyLists(ref Motion Mot, ref BinaryReader Reader)
        {
            uint Count = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int i = 0; i < Count; i++)
            {
                TimePropertyList TList = new TimePropertyList();
                TList.TimePropsCount = Endian.SwapUInt32(Reader.ReadUInt32());

                for (int j = 0; j < TList.TimePropsCount; j++)
                {
                    TimeProperty TProp = new TimeProperty();
                    TProp.TPropID = Endian.SwapUInt32(Reader.ReadUInt32());
                    TProp.PList = ReadPropList(Reader);
                }

                Mot.TimePropertyLists.Add(TList);
            }
        }
    }
}
