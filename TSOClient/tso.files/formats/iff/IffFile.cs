/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using System.Reflection;

namespace FSO.Files.Formats.IFF
{
    /// <summary>
    /// Interchange File Format (IFF) is a chunk-based file format for binary resource data 
    /// intended to promote a common model for store and use by an executable.
    /// </summary>
    public class IffFile : IFileInfoUtilizer
    {
        /// <summary>
        /// Set to true to force the game to retain a copy of all chunk data at time of loading (used to generate piffs)
        /// Should really only be set when the user wants to use the IDE, as it uses a lot more memory.
        /// </summary>
        public static bool RETAIN_CHUNK_DATA = false;

        public string Filename;

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
            {"TPRP", typeof(TPRP)},
            {"SLOT", typeof(SLOT)},
            {"GLOB", typeof(GLOB)},
            {"BCON", typeof(BCON)},
            {"TTAB", typeof(TTAB)},
            {"OBJf", typeof(OBJf)},
            {"TTAs", typeof(TTAs)},
            {"FWAV", typeof(FWAV)},
            {"BMP_", typeof(BMP)},
            {"PIFF", typeof(PIFF) }
        };

        public IffRuntimeInfo RuntimeInfo = new IffRuntimeInfo();
        private Dictionary<Type, Dictionary<ushort, object>> ByChunkId;
        private Dictionary<Type, List<object>> ByChunkType;
        public List<IffChunk> RemovedOriginal = new List<IffChunk>();

        /// <summary>
        /// Constructs a new IFF instance.
        /// </summary>
        public IffFile()
        {
            ByChunkId = new Dictionary<Type, Dictionary<ushort, object>>();
            ByChunkType = new Dictionary<Type, List<object>>();
        }

        /// <summary>
        /// Constructs an IFF instance from a filepath.
        /// </summary>
        /// <param name="filepath">Path to the IFF.</param>
        public IffFile(string filepath) : this()
        {
            using (var stream = File.OpenRead(filepath))
            {
                this.Read(stream);
                SetFilename(Path.GetFileName(filepath));
            }
        }

        /// <summary>
        /// Reads an IFF from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {

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
                    var chunkLabel = io.ReadCString(64).TrimEnd('\0');
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
                        newChunk.OriginalID = chunkID;
                        newChunk.ChunkFlags = chunkFlags;
                        newChunk.ChunkLabel = chunkLabel;
                        newChunk.ChunkType = chunkType;
                        newChunk.ChunkData = io.ReadBytes(chunkDataSize);
                        if (RETAIN_CHUNK_DATA)
                        {
                            newChunk.OriginalLabel = chunkLabel;
                            newChunk.OriginalData = newChunk.ChunkData;
                        }
                        newChunk.ChunkParent = this;

                        if (!ByChunkType.ContainsKey(chunkClass)){
                            ByChunkType.Add(chunkClass, new List<object>());
                        }
                        if (!ByChunkId.ContainsKey(chunkClass)){
                            ByChunkId.Add(chunkClass, new Dictionary<ushort, object>());
                        }

                        ByChunkType[chunkClass].Add(newChunk);
                        if (!ByChunkId[chunkClass].ContainsKey(chunkID)) ByChunkId[chunkClass].Add(chunkID, newChunk);
                    }
                }
            }
        }

        public void Write(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                io.WriteCString("IFF FILE 2.5:TYPE FOLLOWED BY SIZE\0 JAMIE DOORNBOS & MAXIS 1", 60);
                io.WriteUInt32(0); //todo: resource map offset

                var chunks = ListAll();
                foreach (var c in chunks)
                {
                    var typeString = CHUNK_TYPES.FirstOrDefault(x => x.Value == c.GetType()).Key;

                    io.WriteCString((typeString == null) ? c.ChunkType : typeString, 4);

                    byte[] data;
                    using (var cstr = new MemoryStream())
                    {
                        if (c.Write(this, cstr)) data = cstr.ToArray();
                        else data = c.OriginalData;
                    }

                    //todo: exporting PIFF as IFF SHOULD NOT DO THIS
                    c.OriginalData = data; //if we revert, it is to the last save.
                    c.AddedByPatch = false;
                    c.RuntimeInfo = ChunkRuntimeState.Normal;

                    io.WriteUInt32((uint)data.Length+76);
                    io.WriteUInt16(c.ChunkID);
                    io.WriteUInt16(c.ChunkFlags);
                    io.WriteCString(c.ChunkLabel, 64);
                    io.WriteBytes(data);
                }
            }
        }

        private T prepare<T>(object input)
        {
            IffChunk chunk = (IffChunk)input;
            if (chunk.ChunkProcessed != true)
            {
                lock (chunk)
                {
                    if (chunk.ChunkProcessed != true)
                    {
                        using (var stream = new MemoryStream(chunk.ChunkData))
                        {
                            chunk.Read(this, stream);
                            chunk.ChunkData = null;
                            chunk.ChunkProcessed = true;
                        }
                    }
                }
            }
            return (T)input;
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

        public List<IffChunk> ListAll()
        {
            var result = new List<IffChunk>();
            foreach (var type in ByChunkType.Values)
            {
                foreach (var chunk in type)
                {
                    result.Add(this.prepare<IffChunk>(chunk));
                }
            }
            return result;
        }

        public List<IffChunk> SilentListAll()
        {
            var result = new List<IffChunk>();
            foreach (var type in ByChunkType.Values)
            {
                foreach (var chunk in type)
                {
                    result.Add((IffChunk)chunk);
                }
            }
            return result;
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

        public void RemoveChunk(IffChunk chunk)
        {
            var type = chunk.GetType();
            ByChunkId[type].Remove(chunk.ChunkID);
            ByChunkType[type].Remove(chunk);
        }

        public void FullRemoveChunk(IffChunk chunk)
        {
            //register this chunk as one that has been hard removed
            if (!chunk.AddedByPatch) RemovedOriginal.Add(chunk);
            RemoveChunk(chunk);
        }

        public void AddChunk(IffChunk chunk)
        {
            var type = chunk.GetType();
            chunk.ChunkParent = this;

            if (!ByChunkType.ContainsKey(type))
            {
                ByChunkType.Add(type, new List<object>());
            }
            if (!ByChunkId.ContainsKey(type))
            {
                ByChunkId.Add(type, new Dictionary<ushort, object>());
            }

            ByChunkId[type].Add(chunk.ChunkID, chunk);
            ByChunkType[type].Add(chunk);
        }

        public void MoveAndSwap(IffChunk chunk, ushort targID)
        {
            if (chunk.ChunkID == targID) return;
            var type = chunk.GetType();
            object targ = null;
            if (ByChunkId.ContainsKey(type))
            {
                ByChunkId[type].TryGetValue(targID, out targ);
            }

            IffChunk tChunk = (IffChunk)targ;

            if (tChunk != null) RemoveChunk(tChunk);
            var oldID = chunk.ChunkID;
            RemoveChunk(chunk);
            chunk.ChunkID = targID;
            AddChunk(chunk);
            if (tChunk != null)
            {
                tChunk.ChunkID = oldID;
                AddChunk(tChunk);
            }
        }

        public void Revert()
        {
            //revert all iffs and rerun patches

            var toRemove = new List<IffChunk>();
            foreach (var chunk in SilentListAll())
            {
                if (chunk.AddedByPatch) chunk.ChunkParent.RemoveChunk(chunk);
                else
                {
                    if (chunk.OriginalData != null)
                    { 
                        //revert if we have a state to revert to. for new chunks this is not really possible.
                        chunk.ChunkData = chunk.OriginalData;
                        chunk.ChunkParent.MoveAndSwap(chunk, chunk.OriginalID);
                        chunk.Dispose();
                        chunk.ChunkProcessed = false;
                    }
                }
            }

            //add back removed chunks
            foreach (var chunk in RemovedOriginal)
            {
                chunk.ChunkParent.AddChunk(chunk);
            }
            RemovedOriginal.Clear();

            //rerun patches
            foreach (var piff in RuntimeInfo.Patches)
            {
                Patch(piff);
            }

            foreach (var type in ByChunkType.Values)
            {
                foreach (IffChunk chunk in type)
                {
                    prepare<IffChunk>(chunk);
                }
            }
        }

        public void Revert<T>(T chunk) where T : IffChunk
        {
            chunk.RuntimeInfo = ChunkRuntimeState.Normal;
            if (RuntimeInfo.State != IffRuntimeState.Standalone && chunk.AddedByPatch)
            {
                //added by piff. 
                foreach (var piff in RuntimeInfo.Patches)
                {
                    var oldC = piff.Get<T>(chunk.ChunkID);
                    if (oldC != null)
                    {
                        chunk.ChunkData = oldC.OriginalData;
                        chunk.Dispose();
                        chunk.ChunkProcessed = false;
                        chunk.RuntimeInfo = ChunkRuntimeState.Patched;
                        prepare<T>(chunk);
                    }
                }
            }
            else
            {
                chunk.ChunkData = chunk.OriginalData;
                foreach (var piffFile in RuntimeInfo.Patches)
                {
                    var piff = piffFile.List<PIFF>()[0];
                    foreach (var e in piff.Entries)
                    { 
                        var type = CHUNK_TYPES[e.Type];
                        if (!(type.IsAssignableFrom(chunk.GetType())) || e.ChunkID != chunk.ChunkID) continue;

                        chunk.ChunkData = e.Apply(chunk.ChunkData);
                        chunk.RuntimeInfo = ChunkRuntimeState.Patched;
                    }
                }
                chunk.ChunkProcessed = false;
                chunk.Dispose();
                prepare<T>(chunk);
            }
        }

        public void Patch(IffFile piffFile)
        {
            if (RuntimeInfo.State == IffRuntimeState.ReadOnly) RuntimeInfo.State = IffRuntimeState.PIFFPatch;
            var piff = piffFile.List<PIFF>()[0];

            //patch existing chunks using the PIFF chunk
            //also delete chunks marked for deletion

            var moveChunks = new List<IffChunk>();
            var newIDs = new List<ushort>();

            foreach (var e in piff.Entries)
            {
                var type = CHUNK_TYPES[e.Type];

                Dictionary<ushort, object> chunks = null;
                ByChunkId.TryGetValue(type, out chunks);
                if (chunks == null) continue;
                object objC = null;
                chunks.TryGetValue(e.ChunkID, out objC);
                if (objC == null) continue;

                var chunk = (IffChunk)objC;
                if (e.Delete)
                {
                    FullRemoveChunk(chunk); //removed by PIFF
                }
                else
                {
                    chunk.ChunkData = e.Apply(chunk.ChunkData);
                    if (e.ChunkLabel != "") chunk.ChunkLabel = e.ChunkLabel;
                    chunk.RuntimeInfo = ChunkRuntimeState.Patched;

                    if (e.ChunkID != e.NewChunkID)
                    {
                        moveChunks.Add(chunk);
                        newIDs.Add(e.NewChunkID);
                    }
                }
            }

            for (int i=0; i<moveChunks.Count; i++)
                MoveAndSwap(moveChunks[i], newIDs[i]);

            //add chunks present in the piff to the original file
            foreach (var typeG in piffFile.ByChunkType)
            {
                if (typeG.Key == typeof(PIFF)) continue;
                foreach (var res in typeG.Value)
                {
                    var chunk = (IffChunk)res;
                    chunk.AddedByPatch = true;
                    if (!ByChunkId.ContainsKey(chunk.GetType()) || !ByChunkId[chunk.GetType()].ContainsKey(chunk.ChunkID)) this.AddChunk(chunk);
                }
            }
        }

        public void SetFilename(string filename)
        {
            Filename = filename;
            var piffs = PIFFRegistry.GetPIFFs(filename);
            RuntimeInfo.Patches.Clear();
            if (piffs != null)
            {
                //apply patches
                foreach (var piff in piffs)
                {
                    Patch(piff);
                    if (RETAIN_CHUNK_DATA) RuntimeInfo.Patches.Add(piff);
                }
            }
        }
    }
}
