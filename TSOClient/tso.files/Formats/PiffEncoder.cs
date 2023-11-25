using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Files.Formats
{
    public static class PiffEncoder
    {
        private static string FindComment(IffChunk chunk, PIFF oldPIFF)
        {
            var oldEntry = oldPIFF?.Entries?.FirstOrDefault(entry =>
                entry.Type == chunk.ChunkType &&
                ((chunk.AddedByPatch) ? chunk.ChunkID : chunk.OriginalID) == entry.ChunkID);
            return oldEntry?.Comment ?? "";
        }

        public static IffFile GeneratePiff(IffFile iff, HashSet<Type> allowedTypes, HashSet<Type> disallowedTypes, PIFF oldPIFF)
        {
            var piffFile = new IffFile();

            var piff = new PIFF();
            piff.SourceIff = iff.Filename;
            if (oldPIFF != null) piff.Comment = oldPIFF.Comment;
            var entries = new List<PIFFEntry>();
            var chunks = iff.ListAll();
            
            //write removals first
            foreach (var c in iff.RemovedOriginal)
            {
                lock (c)
                {
                    if ((allowedTypes == null || allowedTypes.Contains(c.GetType()))
                        && (disallowedTypes == null || !disallowedTypes.Contains(c.GetType())))
                    {
                        entries.Add(new PIFFEntry {
                            Type = c.ChunkType, ChunkID = c.OriginalID, EntryType = PIFFEntryType.Remove,
                            ChunkLabel = c.ChunkLabel, ChunkFlags = c.ChunkFlags, Comment = FindComment(c, oldPIFF)
                        });
                    }
                }
            }

            bool anyAdded = false;
            foreach (var c in chunks)
            {
                //find a comment for this chunk
                lock (c)
                {
                    if ((allowedTypes == null || allowedTypes.Contains(c.GetType()))
                        && (disallowedTypes == null || !disallowedTypes.Contains(c.GetType())))
                    {
                        if (c.AddedByPatch)
                        {
                            //this chunk has been newly added.
                            var oldParent = c.ChunkParent;
                            anyAdded = true;
                            piffFile.AddChunk(c);
                            c.ChunkParent = oldParent;

                            //make an entry for it!
                            entries.Add(new PIFFEntry()
                            {
                                ChunkID = c.ChunkID,
                                ChunkLabel = c.ChunkLabel,
                                ChunkFlags = c.ChunkFlags,
                                EntryType = PIFFEntryType.Add,
                                NewDataSize = (uint)(c.ChunkData?.Length ?? 0),
                                Type = c.ChunkType,
                                Comment = FindComment(c, oldPIFF)
                            });
                        }
                        else if ((c.RuntimeInfo == ChunkRuntimeState.Modified || c.RuntimeInfo == ChunkRuntimeState.Patched))
                        {
                            var chunkD = MakeChunkDiff(c);
                            if (chunkD != null && (chunkD.Patches.Length > 0 || c.OriginalLabel != c.ChunkLabel || c.OriginalID != c.ChunkID))
                            {
                                chunkD.Comment = FindComment(c, oldPIFF);
                                entries.Add(chunkD);
                            }
                            c.RuntimeInfo = ChunkRuntimeState.Patched;
                        }
                    }
                }
            }
            if (entries.Count == 0 && !anyAdded) return null; //no patch data...
            piff.Entries = entries.ToArray();
            piff.ChunkID = 256;
            piff.ChunkLabel = (piff.SourceIff + " patch");
            piff.ChunkProcessed = true;

            piffFile.AddChunk(piff);
            piffFile.Filename = (oldPIFF != null) ? oldPIFF.ChunkParent.Filename : null; // (piff.SourceIff.Substring(0, piff.SourceIff.Length - 4)+".piff")
            return piffFile;
        }

        public static PIFFEntry MakeChunkDiff(IffChunk chk)
        {
            var e = new PIFFEntry { Type = chk.ChunkType, ChunkID = chk.OriginalID, NewChunkID = chk.ChunkID };
            if (chk == null)
            {
                e.EntryType = PIFFEntryType.Remove;
                return e;
            }

            byte[] newData = null;
            using (var stream = new MemoryStream())
            {
                if (!chk.Write(chk.ChunkParent, stream))
                {
                    return null; //use original
                }
                newData = stream.ToArray();
            }

            e.ChunkLabel = (chk.OriginalLabel==chk.ChunkLabel)?"":chk.ChunkLabel;
            e.ChunkFlags = chk.ChunkFlags;
            e.NewDataSize = (uint)newData.Length;

            //encode difference as sequence of changes
            var oldData = chk.OriginalData;
            var patches = new List<PIFFPatch>();

            int i;
            for (i=0; i<newData.Length; i += 1000)
            {
                if (i >= oldData.Length)
                {
                    //no more comparisons, just add the remainder
                    var remain = new byte[newData.Length-i];
                    Array.Copy(newData, i, remain, 0, remain.Length);
                    patches.Add(new PIFFPatch
                    {
                        Mode = PIFFPatchMode.Add,
                        Data = remain,
                        Offset = (uint)i,
                        Size = (uint)remain.Length
                    });
                    break;
                }

                //dynamic programming matrix.
                int m = Math.Min(1000, Math.Max(0, newData.Length - i))+1;
                int n = Math.Min(1000, Math.Max(0, oldData.Length - i))+1;
                ushort[,] comp = new ushort[m, n];
                for (int x=1; x<m; x++)
                {
                    for (int y=1; y<n; y++)
                    {
                        if (newData[i+x-1] == oldData[i + y -1]) comp[x, y] = (ushort)(comp[x - 1, y - 1] + 1);
                        else comp[x, y] = Math.Max(comp[x, y - 1], comp[x - 1, y]);
                    }
                }

                var changes = new Stack<byte>();
                //backtrack through compare
                {
                    int x = m-1, y = n-1;
                    while (true)
                    {
                        if (x>0 && y>0 && newData[i + x -1] == oldData[i + y -1])
                        {
                            x--; y--; changes.Push(0); //no change
                        } else if (y>0 && (x==0 || comp[x,y-1] >= comp[x-1,y]))
                        {
                            y--; changes.Push(2); //remove
                        } else if (x>0 && (y==0 || comp[x,y-1] < comp[x-1,y]))
                        {
                            x--; changes.Push(1); //add
                        } else
                        {
                            break;
                        }
                    }
                }

                byte lastC = 0;
                PIFFPatch? curr = null;
                List<byte> addArray = null;
                int ptr = 0;
                foreach (var c in changes)
                {
                    if (c != lastC && curr != null)
                    {
                        var patch = curr.Value;
                        if (lastC == 1) patch.Data = addArray.ToArray();
                        patches.Add(patch);
                        curr = null;
                    }
                    if (c == 0) ptr++;
                    else if (c == 1)
                    {
                        if (lastC != 1)
                        {
                            curr = new PIFFPatch { Mode = PIFFPatchMode.Add, Offset = (uint)(i + ptr), Size = 1 };
                            addArray = new List<byte>();
                            addArray.Add(newData[i + ptr]);
                        }
                        else
                        {
                            var patch = curr.Value;
                            patch.Size++;
                            curr = patch;
                            addArray.Add(newData[i + ptr]);
                        }
                        ptr++;
                    }
                    else
                    {
                        if (lastC != 2)
                            curr = new PIFFPatch { Mode = PIFFPatchMode.Remove, Offset = (uint)(i + ptr), Size = 1 };
                        else
                        {
                            var patch = curr.Value;
                            patch.Size++;
                            curr = patch;
                        }
                    }
                    lastC = c;
                }

                if (curr != null)
                {
                    var patch = curr.Value;
                    if (lastC == 1) patch.Data = addArray.ToArray();
                    patches.Add(patch);
                }

                if (m < n)
                {
                    //remainder on src to be removed
                    patches.Add(new PIFFPatch { Mode = PIFFPatchMode.Remove, Offset = (uint)(i+ptr), Size = (uint)(n-m) });
                }
                /*else if (m != n)
                {
                    //remainder on dest to be added
                    var remain = new byte[m-n];
                    Array.Copy(newData, i+ptr, remain, 0, remain.Length);
                    patches.Add(new PIFFPatch
                    {
                        Mode = PIFFPatchMode.Add,
                        Data = remain,
                        Offset = (uint)(i+ ptr),
                        Size = (uint)remain.Length
                    });
                }*/
            }

            if (oldData.Length > i)
            {
                //ran out of new data, but old is still going. Remove the remainder.
                patches.Add(new PIFFPatch
                {
                    Mode = PIFFPatchMode.Remove,
                    Offset = (uint)newData.Length,
                    Size = (uint)(oldData.Length - i)
                });
            }

            e.Patches = patches.ToArray();

            return e;
        }
    }
}
