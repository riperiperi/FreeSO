/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Iffinator.Flash
{
    public class TTABCore
    {
        public short ActionFunction;
        public short GuardFunction;
        public int MotiveEntries;
        public int Flags;
        public int StrTableIndex;
        public int AttenuationCode;
        public int AttenuationValue;
        public int Autonomy;
        public int JoinIndex;
    }

    public class InteractionList : IffChunk
    {
        private short m_NumInteractions;
        private short m_Version;
        private byte m_CompressionCode;
        private List<TTABCore> m_Interactions = new List<TTABCore>();

        public InteractionList(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            if ((Reader.BaseStream.Length - Reader.BaseStream.Position) == 0)
                return; //Empty (strange, but it happens).

            m_NumInteractions = Reader.ReadInt16();

            if ((Reader.BaseStream.Length - Reader.BaseStream.Position) == 0 || 
                (Reader.BaseStream.Length - Reader.BaseStream.Position) < 2)
                return; //Too short (strange, but it happens).

            m_Version = Reader.ReadInt16();

            if (m_NumInteractions <= 0)
                return;

            if (m_Version == 9 || m_Version == 10)
            {
                m_CompressionCode = Reader.ReadByte();

                if (m_CompressionCode != 1)
                    return; //Compressioncode should always be 1.
            }

            switch (m_Version)
            {
                case 2:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        Interaction.ActionFunction = Reader.ReadInt16();
                        Interaction.GuardFunction = Reader.ReadInt16();
                        Interaction.MotiveEntries = Reader.ReadInt16();
                        Reader.ReadInt16(); //0xA3A3 (skip)
                        Reader.ReadInt32(); //4-byte float, no idea what it is used for.

                        m_Interactions.Add(Interaction);
                    }

                    break;
                case 3:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        Interaction.ActionFunction = Reader.ReadInt16();
                        Interaction.GuardFunction = Reader.ReadInt16();
                        Interaction.MotiveEntries = Reader.ReadInt16();
                        Interaction.Flags = Reader.ReadInt16();
                        Reader.ReadInt32(); //4-byte float, no idea what it is used for.

                        m_Interactions.Add(Interaction);
                    }

                    break;
                case 5:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        Interaction.ActionFunction = Reader.ReadInt16();
                        Interaction.GuardFunction = Reader.ReadInt16();
                        Interaction.MotiveEntries = Reader.ReadInt32();
                        Interaction.Flags = Reader.ReadInt32();
                        Interaction.StrTableIndex = Reader.ReadInt32();
                        Interaction.Autonomy = Reader.ReadInt32();
                        Interaction.JoinIndex = Reader.ReadInt32();

                        m_Interactions.Add(Interaction);
                    }

                    break;
                case 7:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        Interaction.ActionFunction = Reader.ReadInt16();
                        Interaction.GuardFunction = Reader.ReadInt16();
                        Interaction.MotiveEntries = Reader.ReadInt32();
                        Interaction.Flags = Reader.ReadInt32();
                        Interaction.StrTableIndex = Reader.ReadInt32();
                        Interaction.AttenuationCode = Reader.ReadInt32();
                        Interaction.AttenuationValue = Reader.ReadInt32();
                        Interaction.Autonomy = Reader.ReadInt32();
                        Interaction.JoinIndex = Reader.ReadInt32();

                        m_Interactions.Add(Interaction);
                    }

                    break;
                case 8:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        Interaction.ActionFunction = Reader.ReadInt16();
                        Interaction.GuardFunction = Reader.ReadInt16();
                        Interaction.MotiveEntries = Reader.ReadInt32();
                        Interaction.Flags = Reader.ReadInt32();
                        Interaction.StrTableIndex = Reader.ReadInt32();
                        Interaction.AttenuationCode = Reader.ReadInt32();
                        Interaction.AttenuationValue = Reader.ReadInt32();
                        Interaction.Autonomy = Reader.ReadInt32();
                        Interaction.JoinIndex = Reader.ReadInt32();

                        m_Interactions.Add(Interaction);
                    }

                    break;
                case 9:
                    for (int i = 0; i < m_NumInteractions; i++)
                    {
                        TTABCore Interaction = new TTABCore();

                        BitArray BArray = new BitArray(Reader.ReadBytes(2));
                        Interaction.ActionFunction = (short)GetShortBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.GuardFunction = (short)GetShortBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.MotiveEntries = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.Flags = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.StrTableIndex = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.AttenuationCode = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.AttenuationValue = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.Autonomy = (int)GetLongBits(BArray);

                        BArray = new BitArray(Reader.ReadBytes(4));
                        Interaction.JoinIndex = (int)GetLongBits(BArray);

                        m_Interactions.Add(Interaction);
                    }

                    break;
            }

            Reader.Close();
        }

        private long GetShortBits(BitArray BArray)
        {
            byte[] Widths = { 5, 8, 13, 16 };

            if (BArray.Length == 0 || BArray.Get(0) == false)
                return 0;

            byte Code1 = BArray.Get(1) ? (byte)1 : (byte)0;
            byte Code2 = BArray.Get(2) ? (byte)1 : (byte)0;
            byte Code = (byte)(Code1 & Code2);

            byte Width = Widths[Code];

            ArrayList Bits = new ArrayList(BArray.Length - 3);
            int Counter = 0;

            for (int i = 3; i < Width; i++)
            {
                Bits.Add((byte)((BArray.Get(i) == true) ? 1 : 0));
                Counter++;
            }

            byte[] Bytes = (byte[])Bits.ToArray(typeof(byte));
            long Value = 0;
            if (Bytes.Length < 8)
            {
                List<byte> bytes = new List<byte>(Bytes);
                while (bytes.Count != 8)
                    bytes.Add(0);
                Value = BitConverter.ToInt64(bytes.ToArray(), 0);
            }
            else
                Value = BitConverter.ToInt64(Bytes, 0);

            //magic incantation to sign-extend value.
            Value |= -(Value & (1 << Width));

            return Value;
        }

        private long GetLongBits(BitArray BArray)
        {
            byte[] Widths = { 6, 11, 21, 32 };

            if (BArray.Count == 0 || BArray.Get(0) == false)
                return 0;

            byte Code1 = BArray.Get(1) ? (byte)1 : (byte)0;
            byte Code2 = BArray.Get(2) ? (byte)1 : (byte)0;
            byte Code = (byte)(Code1 & Code2);

            byte Width = Widths[Code];

            ArrayList Bits = new ArrayList(BArray.Length - 3);
            int Counter = 0;

            for (int i = 3; i < Width; i++)
            {
                Bits.Add((byte)((BArray.Get(i) == true) ? 1 : 0));
                Counter++;
            }

            byte[] Bytes = (byte[])Bits.ToArray(typeof(byte));
            long Value = 0;
            if (Bytes.Length < 8)
            {
                List<byte> bytes = new List<byte>(Bytes);
                while (bytes.Count != 8)
                    bytes.Add(0);
                Value = BitConverter.ToInt64(bytes.ToArray(), 0);
            }
            else
                Value = BitConverter.ToInt64(Bytes, 0);

            //magic incantation to sign-extend value.
            Value |= -(Value & (1 << Width));

            return Value;
        }
    }
}
