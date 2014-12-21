/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the Iffinator.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Iffinator.Flash
{
    /// <summary>
    /// Each OBJf resource contains a table of pairs of IDs.
    /// The first ID corresponds to a BHAV with a guard function.
    /// If it returns true, the next function can be run.
    /// </summary>
    public class IDPair
    {
        public ushort GuardFuncID = 0x00;
        public ushort FunctionID = 0x00;
    }

    public class OBJf : IffChunk
    {
        private int m_NumEntries;
        private List<IDPair> m_FuncIDs = new List<IDPair>();

        /// <summary>
        /// The table of function IDs stored in this
        /// OBJf resource.
        /// </summary>
        public List<IDPair> FunctionIDs
        {
            get { return m_FuncIDs; }
        }

        public OBJf(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            //Unknown + version (always 0)
            Reader.ReadBytes(8);

            string Header = Encoding.ASCII.GetString(Reader.ReadBytes(4));

            if (Header != "fJBO")
                return; //Error? This shouldn't occur...

            m_NumEntries = Reader.ReadInt32();

            for (int i = 0; i < m_NumEntries; i++)
            {
                IDPair FuncIDs = new IDPair();
                FuncIDs.GuardFuncID = Reader.ReadUInt16();
                FuncIDs.FunctionID = Reader.ReadUInt16();

                m_FuncIDs.Add(FuncIDs);
            }
        }
    }
}
