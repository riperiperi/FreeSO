using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.formats.iff.chunks;
using TSO.Files.utils;

namespace TSO.Files.formats.iff
{
    /// <summary>
    /// Interchange File Format (IFF) is a chunk-based file format for binary resource data 
    /// intended to promote a common model for store and use by an executable.
    /// </summary>
    public class Iff
    {
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
            {"GLOB", typeof(GLOB)},
            {"BCON", typeof(BCON)},
            {"TTAB", typeof(TTAB)},
            {"OBJf", typeof(OBJf)},
            {"TTAs", typeof(TTAs)},
            {"FWAV", typeof(FWAV)},
            {"BMP_", typeof(BMP)}
        };

        private Dictionary<Type, Dictionary<ushort, object>> ByChunkId;
        private Dictionary<Type, List<object>> ByChunkType;

        /// <summary>
        /// Constructs a new IFF instance.
        /// </summary>
        public Iff()
        {
        }

        /// <summary>
        /// Constructs an IFF instance from a filepath.
        /// </summary>
        /// <param name="filepath">Path to the IFF.</param>
        public Iff(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                this.Read(stream);
            }
        }

        /// <summary>
        /// Reads an IFF from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {
            ByChunkId = new Dictionary<Type, Dictionary<ushort, object>>();
            ByChunkType = new Dictionary<Type, List<object>>();

            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                var identifier = io.ReadCString(60, false).Replace("\0", "");
                if (identifier != "IFF FILE 2.5:TYPE FOLLOWED BY SIZE JAMIE DOORNBOS & MAXIS 1")
                {
                    throw new Exception("Invalid iff file!");
                }

                var rsmpOffset = io.ReadUInt32();

                while (io.HasMore)
                {
                    var chunkType = io.ReadCString(4);
                    var chunkSize = io.ReadUInt32();
                    var chunkID = io.ReadUInt16();
                    var chunkFlags = io.ReadUInt16();
                    var chunkLabel = io.ReadCString(64);
                    var chunkDataSize = chunkSize - 76;

                    /** Do we understand this chunk type? **/
                    if (!CHUNK_TYPES.ContainsKey(chunkType))
                    {
                        /** Skip it! **/
                        io.Skip(Math.Min(chunkDataSize, stream.Length - stream.Position - 1)); //if the chunk is invalid, it will likely provide a chunk size beyond the limits of the file. (walls2.iff)
                    }else{
                        Type chunkClass = CHUNK_TYPES[chunkType];
                        IffChunk newChunk = (IffChunk)Activator.CreateInstance(chunkClass);
                        newChunk.ChunkID = chunkID;
                        newChunk.ChunkFlags = chunkFlags;
                        newChunk.ChunkLabel = chunkLabel;
                        newChunk.ChunkData = io.ReadBytes(chunkDataSize);
                        newChunk.ChunkParent = this;

                        if (!ByChunkType.ContainsKey(chunkClass)){
                            ByChunkType.Add(chunkClass, new List<object>());
                        }
                        if (!ByChunkId.ContainsKey(chunkClass)){
                            ByChunkId.Add(chunkClass, new Dictionary<ushort, object>());
                        }

                        ByChunkType[chunkClass].Add(newChunk);
                        ByChunkId[chunkClass].Add(chunkID, newChunk);
                    }
                }
            }
        }

        private T prepare<T>(object input)
        {
            IffChunk chunk = (IffChunk)input;
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
        /// <typeparam name="T">Type of the chunk.</typeparam>
        /// <param name="id">ID of the chunk.</param>
        /// <returns>A chunk.</returns>
        public T Get<T>(ushort id){
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
        /// <typeparam name="T">The type of the chunks to list.</typeparam>
        /// <returns>A list of chunks of the type.</returns>
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
