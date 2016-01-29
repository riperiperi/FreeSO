using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats
{
    public static class PiffEncoder
    {
        public static IffFile GeneratePiff(IffFile iff)
        {
            var piffFile = new IffFile();

            var piff = new PIFF();
            piff.SourceIff = iff.Filename;
            var entries = new List<PIFFEntry>();
            var chunks = iff.ListAll();
            foreach (var c in chunks)
            {
                var chunkD = MakeChunkDiff(c);
                if (chunkD != null && chunkD.Patches.Length > 0)
                {
                    entries.Add(chunkD);
                }
            }
            piff.Entries = entries.ToArray();
            piff.ChunkID = 256;
            piff.ChunkLabel = (piff.SourceIff + " patch");
            piff.ChunkProcessed = true;

            piffFile.AddChunk(piff);
            piffFile.Filename = piff.SourceIff.Substring(0, piff.SourceIff.Length - 4)+".piff";
            return piffFile;
        }

        public static PIFFEntry MakeChunkDiff(IffChunk chk)
        {
            var e = new PIFFEntry { Type = chk.ChunkType, ChunkID = chk.ChunkID };
            if (chk == null)
            {
                e.Delete = true;
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

            e.ChunkLabel = chk.ChunkLabel;
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
                    Array.Copy(newData, newData.Length - remain.Length, remain, 0, remain.Length);
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
                        if (newData[x-1] == oldData[y-1]) comp[x, y] = (ushort)(comp[x - 1, y - 1] + 1);
                        else comp[x, y] = Math.Max(comp[x, y - 1], comp[x - 1, y]);
                    }
                }

                var changes = new Stack<byte>();
                //backtrack through compare
                {
                    int x = m-1, y = n-1;
                    while (true)
                    {
                        if (x>0 && y>0 && newData[x-1] == oldData[y-1])
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
                PIFFPatch curr = null;
                List<byte> addArray = null;
                int ptr = 0;
                foreach (var c in changes)
                {
                    if (c != lastC && curr != null)
                    {
                        if (lastC == 1) curr.Data = addArray.ToArray();
                        patches.Add(curr);
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
                            curr.Size++;
                            addArray.Add(newData[i + ptr]);
                        }
                        ptr++;
                    }
                    else
                    {
                        if (lastC != 2)
                            curr = new PIFFPatch { Mode = PIFFPatchMode.Remove, Offset = (uint)(i + ptr), Size = 1 };
                        else
                            curr.Size++;
                    }
                    lastC = c;
                }

                if (curr != null)
                {
                    if (lastC == 1) curr.Data = addArray.ToArray();
                    patches.Add(curr);
                }
            }

            if (oldData.Length > i)
            {
                //ran out of new data, but old is still going. Remove the remainder.
                patches.Add(new PIFFPatch
                {
                    Mode = PIFFPatchMode.Remove,
                    Offset = (uint)i,
                    Size = (uint)(oldData.Length - i)
                });
            }

            e.Patches = patches.ToArray();

            return e;
        }
    }
}
