/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    /// <summary>
    /// A constant used to tune an object.
    /// </summary>
    public class TuningConstant
    {
        public string Name;
        public float Value;
        public string Description;
    }

    /// <summary>
    /// Represents an FCNS chunk. Found exclusively in globals.iff,
    /// the FCNS chunk holds global floating point tuning constants.
    /// </summary>
    public class FCNS : IffChunk
    {
        private int m_Version;
        private uint m_ConstantCount;
        private List<TuningConstant> m_TuningConstants = new List<TuningConstant>();

        /// <summary>
        /// Creates a new FCNS instance.
        /// </summary>
        /// <param name="Chunk">The chunk to create this FCNS instance from.</param>
        public FCNS(IffChunk Chunk)
            : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.ReadBytes(4); //0
            m_Version = Reader.ReadInt32();
            Reader.ReadBytes(4); //"SNCF"
            m_ConstantCount = Reader.ReadUInt32();

            for(int i = 0; i < m_ConstantCount; i++)
            {
                TuningConstant TConstant = new TuningConstant();

                if (m_Version == 1)
                {
                    TConstant.Name = ReadZeroPaddedString(Reader);

                    byte[] Value = Reader.ReadBytes(4);

                    //This should totally not be neccessary, but once again
                    //Maxis has introduced the concept of 'half-empty-entries'!
                    if (Value.Length == 0)
                    {
                        m_TuningConstants.Add(TConstant);
                        break;
                    }

                    TConstant.Value = BitConverter.ToSingle(Value, 0);
                    TConstant.Description = ReadZeroPaddedString(Reader);
                }
                else if (m_Version == 2)
                {
                    TConstant.Name = Reader.ReadString();
                    TConstant.Value = Reader.ReadSingle();
                    TConstant.Description = Reader.ReadString();
                }

                m_TuningConstants.Add(TConstant);
            }
        }

        /// <summary>
        /// Reads a zero terminated string from a stream.
        /// </summary>
        /// <param name="Reader">The reader to read with.</param>
        /// <returns>The string that was read.</returns>
        private string ReadZeroPaddedString(BinaryReader Reader)
        {
            string Str = "";
            byte Chr = 0;

            while (Chr != 0xA3)
            {
                Chr = Reader.ReadByte();
                Str += (Char)Chr;
            }

            return Str;
        }
    }
}
