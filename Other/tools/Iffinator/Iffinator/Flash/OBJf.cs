/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
