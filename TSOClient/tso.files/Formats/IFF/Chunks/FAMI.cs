/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This class defines a single family in the neighbourhood, and various properties such as their 
    /// budget and house assignment. These can be modified using GenericTS1Calls, but are mainly
    /// defined within CAS.
    /// </summary>
    public class FAMI : IffChunk
    {
        public uint Version;

        public int HouseNumber;
        public int FamilyNumber;
        public int Budget;
        public int NetWorth;
        public int FamilyFriends;
        public int Unknown; //19 or 1?
        public uint[] FamilyGUIDs;

        /// <summary>
        /// Reads a FAMI chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a OBJf chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32(); //0x9 for latest game
                string magic = io.ReadCString(4); //IMAF

                HouseNumber = io.ReadInt32();
                FamilyNumber = io.ReadInt32();
                Budget = io.ReadInt32();
                NetWorth = io.ReadInt32();
                FamilyFriends = io.ReadInt32();
                Unknown = io.ReadInt32();
                FamilyGUIDs = new uint[io.ReadInt32()];
                for (int i=0; i<FamilyGUIDs.Length; i++)
                {
                    FamilyGUIDs[i] = io.ReadUInt32();
                }
            }
        }
    }
}