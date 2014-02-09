/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    /// <summary>
    /// Interchange File Format (IFF) is a chunk-based file format for binary resource data 
    /// intended to promote a common model for store and use by an executable.
    /// </summary>
    public class Iff
    {
        //Static
        private static Dictionary<string, Type> CHUNK_TYPES = new Dictionary<string, Type>()
        {
            {"STR#", typeof(STR)},
            {"CTSS", typeof(CTSS)},
            {"PALT", typeof(PALT)},
            {"OBJD", typeof(OBJD)},
            {"DGRP", typeof(DGRP)},
            {"SPR#", typeof(SPR)},
            {"SPR2", typeof(SPR2)},
            {"BHAV", typeof(BHAV)},
            {"SLOT", typeof(SLOT)},
            {"BCON", typeof(BCON)},
            {"CST", typeof(CST)}
        };

        //Instance

        private Dictionary<Type, Dictionary<ushort, object>> ByChunkId;
        private Dictionary<Type, List<object>> ByChunkType;

        public Iff()
        {
        }
        public Iff(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                this.Read(stream);
            }
        }

        public void Read(Stream stream)
        {
            ByChunkId = new Dictionary<Type, Dictionary<ushort, object>>();
            ByChunkType = new Dictionary<Type, List<object>>();

            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                var identifier = io.ReadChars(60, false).Replace("\0", "");
                if (identifier != "IFF FILE 2.5:TYPE FOLLOWED BY SIZE JAMIE DOORNBOS & MAXIS 1")
                {
                    throw new Exception("Invalid iff file!");
                }

                var rsmpOffset = io.ReadUInt32();

                while (io.HasMore)
                {
                    var chunkType = io.ReadChars(4);
                    var chunkSize = io.ReadUInt32();
                    var chunkID = io.ReadUInt16();
                    var chunkFlags = io.ReadUInt16();
                    var chunkLabel = io.ReadChars(64);
                    var chunkDataSize = chunkSize - 76;

                    /** Do we understand this chunk type? **/
                    if (!CHUNK_TYPES.ContainsKey(chunkType))
                    {
                        /** Skip it! **/
                        io.Skip(chunkDataSize);
                    }
                    else
                    {
                        Type chunkClass = CHUNK_TYPES[chunkType];
                        AbstractIffChunk newChunk = (AbstractIffChunk)Activator.CreateInstance(chunkClass);
                        newChunk.ChunkID = chunkID;
                        newChunk.ChunkFlags = chunkFlags;
                        newChunk.ChunkLabel = chunkLabel;
                        newChunk.ChunkData = io.ReadBytes(chunkDataSize);
                        newChunk.ChunkParent = this;

                        if (!ByChunkType.ContainsKey(chunkClass))
                        {
                            ByChunkType.Add(chunkClass, new List<object>());
                        }
                        if (!ByChunkId.ContainsKey(chunkClass))
                        {
                            ByChunkId.Add(chunkClass, new Dictionary<ushort, object>());
                        }

                        ByChunkType[chunkClass].Add(newChunk);
                        //if (chunkID != 0){
                        ByChunkId[chunkClass].Add(chunkID, newChunk);
                        //}
                    }
                }
            }
        }

        private T prepare<T>(object input)
        {
            AbstractIffChunk chunk = (AbstractIffChunk)input;
            lock (chunk)
            {
                if (chunk.ChunkProcessed != true)
                {
                    using (var stream = new MemoryStream(chunk.ChunkData))
                    {
                        chunk.Read(this, stream);
                        chunk.ChunkProcessed = true;
                    }
                    return (T)input;
                }
                return (T)input;
            }
        }

        /// <summary>
        /// Get a chunk by its type and ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get<T>(ushort id)
        {
            Type typeofT = typeof(T);
            if (ByChunkId.ContainsKey(typeofT))
            {
                var lookup = ByChunkId[typeofT];
                if (lookup.ContainsKey(id))
                {
                    return prepare<T>(lookup[id]);
                }
            }
            return default(T);
        }

        /// <summary>
        /// List all chunks of a certain type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> List<T>()
        {
            Type typeofT = typeof(T);
            if (ByChunkType.ContainsKey(typeofT))
            {
                var result = new List<T>();
                foreach (var item in ByChunkType[typeofT])
                {
                    result.Add(this.prepare<T>(item));
                }
                return result;
            }
            return null;
        }
    }
}