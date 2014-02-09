/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Propeng.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LogThis;

namespace SimsLib.IFF
{
    /// <summary>
    /// An interaction that can be selected in a pie menu.
    /// </summary>
    public class Interaction
    {
        /// <summary>
        /// A 2-byte chunk ID referring to the BHAV subroutine executed when the interaction is selected.
        /// </summary>
        public short ActionFuncID;

        /// <summary>
        /// A 2-byte chunk ID referring to a BHAV subroutine. The action function is only allowed to run if the guard 
        /// function returns true, or if the field is set to zero.
        /// </summary>
        public short GuardFuncID;

        /// <summary>
        /// A 4-byte unsigned integer specifying the number of motive entries defined in this interaction (always 16).
        /// </summary>
        public uint MotiveEntryCount;

        /// <summary>
        /// A 4-byte unsigned integer used as a bit field.
        /// </summary>
        public uint Flags;

        /// <summary>
        /// A 4-byte unsigned integer specifying the index of the string used by this interaction in the 
        /// corresponding TTAs chunk.
        /// </summary>
        public uint TTAID;

        /// <summary>
        /// (added in version 7) - A 4-byte unsigned integer that controls the attenuation value described below. 
        /// The possible values for this field are 0 to 4, which correspond to custom (specified in the next field), 
        /// none, low, moderate and high respectively. Presumably, the attenuation values of these constants are 0, 
        /// 0.02, 0.1 and 0.2, which are the most commonly used values in TTAB versions predating this field.
        /// </summary>
        public uint AttenuationCode;

        /// <summary>
        /// A 4-byte IEEE 754 float. If the attenuation code is non-zero, this field is usually zero/garbage, 
        /// otherwise it specifies how quickly the interaction advertisement fades.
        /// </summary>
        public float AttenuationValue;

        /// <summary>
        /// A 4-byte unsigned integer, usually 50 or 100. Only Sims whose autonomy level is higher than or equal to the 
        /// threshold are able to perform this interaction autonomously.
        /// </summary>
        public uint AutonomyThreshold;

        /// <summary>
        /// A 4-byte signed integer that, if not -1, indicates the ID of a joinable activity.
        /// </summary>
        public int JoiningIndex;

        public List<Motive> Motives = new List<Motive>();
    }

    /// <summary>
    /// A motive that can be fulfilled by an interaction.
    /// </summary>
    public class Motive
    {
        /// <summary>
        /// (added in version 7) - A 2-byte signed integer specifying the lowest end of the advertised motive effect range. 
        /// If this field is not present or set to zero, the maximum value is used.
        /// </summary>
        public short EffectRangeMinimum;

        /// <summary>
        /// A 2-byte signed integer specifying the highest end of the advertised motive effect range.
        /// </summary>
        public short EffectRangeMaximum;

        /// <summary>
        /// (added in version 7) - A 2-byte unsigned integer used to determine a value within the range specified by the two 
        /// previous fields. Possible values for this field are 1 to 22, labeled in behaviour.iff chunk 00E8. For example, 
        /// if the personality modifier is 14 (cleaning skill) and the effect range is 1-8, then the strength of the motive 
        /// advertisement to a Sim with level 5 (50%) cleaning skill would be 4.
        /// </summary>
        public ushort PersonalityModifier;
    }

    /// <summary>
    /// This chunk type defines a list of interactions for an object and assigns a BHAV subroutine for each interaction. 
    /// The pie menu labels shown to the user are stored in a TTAs chunk with the same ID.
    /// </summary>
    public class TTAB : IffChunk
    {
        private ushort m_InteractionCount;
        private ushort m_Version;
        private FieldReader m_FReader = new FieldReader();
        private FieldEncodingData m_FEncodingData;
        private byte m_CompressionCode;
        private List<Interaction> m_Interactions = new List<Interaction>();

        /// <summary>
        /// A 2-byte unsigned integer specifying the number of interactions defined by this chunk.
        /// </summary>
        public ushort InteractionCount
        {
            get { return m_InteractionCount; }
        }

        /// <summary>
        /// A 2-byte unsigned integer specifying the version of this TTAB chunk. The highest known version is 11; 
        /// versions found in The Sims Online which are documented below are 5, 8, 9, 10 and 11.
        /// </summary>
        public ushort Version
        {
            get { return m_Version; }
        }

        /// <summary>
        /// Creates a new TTAB instance.
        /// </summary>
        /// <param name="Chunk">The data for the chunk.</param>
        public TTAB(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_InteractionCount = Reader.ReadUInt16();
            
            if (m_InteractionCount > 0)
            {
                m_Version = Reader.ReadUInt16();

                try
                {
                    long FieldValue = 0;

                    if (m_Version == 9 || m_Version == 10)
                    {
                        m_CompressionCode = Reader.ReadByte();

                        m_FEncodingData.CompressionCode = m_CompressionCode;
                        m_FEncodingData.FieldWidths = new byte[] { 5, 8, 13, 16, 6, 11, 21, 32 };
                        m_FEncodingData.EncodedDataLength = (uint)(Reader.BaseStream.Length - Reader.BaseStream.Position);
                        m_FEncodingData.EncodedData = Reader.ReadBytes((int)m_FEncodingData.EncodedDataLength);
                    }

                    for (ushort i = 0; i < m_InteractionCount; i++)
                    {
                        if (m_Version == 9 || m_Version == 10)
                        {
                            Interaction Action = new Interaction();
                            m_FReader.DecodeField(ref m_FEncodingData, 0, ref FieldValue);
                            Action.ActionFuncID = (short)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 0, ref FieldValue);
                            Action.GuardFuncID = (short)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.MotiveEntryCount = (uint)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.Flags = (uint)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.TTAID = (uint)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.AttenuationCode = (uint)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.AttenuationValue = (float)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.AutonomyThreshold = (uint)FieldValue;

                            m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue);
                            Action.JoiningIndex = (int)FieldValue;

                            for (uint j = 0; j < Action.MotiveEntryCount; j++)
                            {
                                Motive MotiveEntry = new Motive();

                                m_FReader.DecodeField(ref m_FEncodingData, 0, ref FieldValue);
                                MotiveEntry.EffectRangeMinimum = (short)FieldValue;

                                m_FReader.DecodeField(ref m_FEncodingData, 0, ref FieldValue);
                                MotiveEntry.EffectRangeMaximum = (short)FieldValue;

                                m_FReader.DecodeField(ref m_FEncodingData, 0, ref FieldValue);
                                MotiveEntry.PersonalityModifier = (ushort)FieldValue;

                                Action.Motives.Add(MotiveEntry);
                            }

                            m_Interactions.Add(Action);

                            if(m_Version == 10)
                                m_FReader.DecodeField(ref m_FEncodingData, 1, ref FieldValue); //Unknown...
                        }
                        else if (m_Version == 5 || m_Version == 7 || m_Version == 8 || m_Version == 11)
                        {
                            Interaction Action = new Interaction();
                            Action.ActionFuncID = Reader.ReadInt16();
                            Action.GuardFuncID = Reader.ReadInt16();
                            Action.MotiveEntryCount = Reader.ReadUInt32();

                            Action.Flags = Reader.ReadUInt32();
                            Action.TTAID = Reader.ReadUInt32();

                            if (m_Version >= 7)
                                Action.AttenuationCode = Reader.ReadUInt32();

                            Action.AttenuationValue = Reader.ReadUInt32();
                            Action.AutonomyThreshold = Reader.ReadUInt32();
                            Action.JoiningIndex = Reader.ReadInt32();

                            for (uint j = 0; j < Action.MotiveEntryCount; j++)
                            {
                                Motive MotiveEntry = new Motive();

                                if (m_Version >= 7)
                                    MotiveEntry.EffectRangeMinimum = Reader.ReadInt16();

                                MotiveEntry.EffectRangeMaximum = Reader.ReadInt16();

                                if(m_Version >= 7)
                                    MotiveEntry.PersonalityModifier = Reader.ReadUInt16();

                                Action.Motives.Add(MotiveEntry);
                            }

                            m_Interactions.Add(Action);

                            if (m_Version == 11)
                                Reader.ReadBytes(4); //Unknown.
                        }
                    }
                }
                catch (Exception E)
                {
                    Log.LogThis("Failed parsing a TTAB chunk!\r\n" + 
                        "Version: " + m_Version + "\r\n" + "InteractionCount: " + m_InteractionCount + "\r\n" + 
                        E.ToString(), eloglevel.error);
                }
            }

            Reader.Close();
        }
    }
}
