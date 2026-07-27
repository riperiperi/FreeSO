using FSO.Common.Utils;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace FSO.Files.RC
{
    public struct DGRP3DTextureSource
    {
        public Texture2D Literal;
        public IDGRP3DTextureHolder Holder;
        public DGRPSprite Sprite;

        public DGRP3DTextureSource(Texture2D literal)
        {
            Literal = literal;
            Holder = null;
            Sprite = null;
        }

        public DGRP3DTextureSource(IDGRP3DTextureHolder holder)
        {
            Literal = null;
            Holder = holder;
            Sprite = null;
        }

        public DGRP3DTextureSource(DGRPSprite sprite)
        {
            Literal = null;
            Holder = null;
            Sprite = sprite;
        }

        public static DGRP3DTextureSource? WithDecoded(IDGRP3DTextureHolder holder, GraphicsDevice gd)
        {
            if (holder == null)
            {
                return null;
            }

            holder.Decode(gd);

            return new DGRP3DTextureSource(holder);
        }
    }

    public class DGRP3DGeometry
    {

        public bool Rendered = false;
        public Texture2D Pixel;
        private DGRP3DTextureSource PixelSource;
        public ushort PixelSPR;
        public ushort PixelDir;

        public ushort CustomTexture;
        public static Func<string, ushort, DGRP3DTextureSource?> ReplTextureProvider;

        public List<DGRP3DVert> SVerts; //simplified vertices
        public List<int> SIndices; //simplified indices

        public VertexBuffer Verts;
        public IndexBuffer Indices;
        public int PrimCount;

        public int RefCount = 1;

        public void SComplete(GraphicsDevice gd)
        {
            Rendered = true;
            Verts?.Dispose();
            Indices?.Dispose();

            PrimCount = SIndices.Count / 3;
            if (PrimCount > 0)
            {
                Verts = new VertexBuffer(gd, typeof(DGRP3DVert), SVerts.Count, BufferUsage.None);
                Verts.SetData(SVerts.ToArray());
                Indices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, SIndices.Count, BufferUsage.None);
                Indices.SetData(SIndices.ToArray());
            }

            if (!IffFile.RETAIN_CHUNK_DATA)
            {
                DecrementDataRef();
            }
        }

        public DGRP3DGeometry() { }
        /// <summary>
        /// Initializes DGRP3DGeometry from an FSOM. CompleteFSOMLoad should be called from the GPU thread to make it renderable.
        /// </summary>
        /// <param name="io"></param>
        /// <param name="source"></param>
        /// <param name="Version"></param>
        public DGRP3DGeometry(IoBuffer io, DGRP source, GraphicsDevice gd, int Version)
        {
            PixelSPR = io.ReadUInt16();
            PixelDir = io.ReadUInt16();
            if (PixelDir == 65535)
            {
                CustomTexture = 1;
                if (source == null)
                {
                    //temporary system for models without DGRP
                    PixelSource = ReplTextureProvider("FSO", PixelSPR) ?? default;
                }
                else
                {
                    var name = source.ChunkParent.Filename.Replace('.', '_').Replace("spf", "iff").ToLowerInvariant();
                    var pxSource = ReplTextureProvider(name, PixelSPR) ?? DGRP3DTextureSource.WithDecoded(source.ChunkParent.Get<MTEX>(PixelSPR), gd);
                    PixelSource = pxSource ?? default;
                }
            }
            else
            {
                PixelSource = new DGRP3DTextureSource(source.GetImage(1, 3, PixelDir).Sprites[PixelSPR]);
            }

            var vertCount = io.ReadInt32();
            SVerts = new List<DGRP3DVert>();

            if (Version > 1)
            {
                var bytes = io.ReadBytes(vertCount * Marshal.SizeOf(typeof(DGRP3DVert)));
                var readVerts = new DGRP3DVert[vertCount];
                var pinnedHandle = GCHandle.Alloc(readVerts, GCHandleType.Pinned);
                Marshal.Copy(bytes, 0, pinnedHandle.AddrOfPinnedObject(), bytes.Length);
                pinnedHandle.Free();
                SVerts = readVerts.ToList();
            }
            else
            {
                for (int i = 0; i < vertCount; i++)
                {
                    var x = io.ReadFloat();
                    var y = io.ReadFloat();
                    var z = io.ReadFloat();
                    var u = io.ReadFloat();
                    var v = io.ReadFloat();
                    var normal = new Vector3();
                    SVerts.Add(new DGRP3DVert(new Vector3(x, y, z), normal, new Vector2(u, v)));
                }
            }
            var indexCount = io.ReadInt32();
            SIndices = ToTArray<int>(io.ReadBytes(indexCount * 4)).ToList();

            // bottom up triangle ordering. useful for trees.
            /*
            var triBase = new int[SIndices.Count / 3][];
            for (int i = 0; i < triBase.Length; i++) triBase[i] = new int[] { SIndices[i * 3], SIndices[i*3 + 1], SIndices[i * 3 + 2] };

            var ordered = triBase.OrderBy(x => SVerts[x[0]].Position.Y);
            SIndices.Clear();
            foreach (var item in ordered) SIndices.AddRange(item);
            */


            if (Version < 2) GenerateNormals(false);
        }

        private void GetPixelFromSource(GraphicsDevice gd)
        {
            if (PixelSource.Holder != null)
            {
                Pixel = PixelSource.Holder.GetTexture(gd);
            }
            else if (PixelSource.Sprite != null)
            {
                Pixel = PixelSource.Sprite.GetTexture(gd);
            }

            PixelSource = default;
        }

        public void CompleteFSOMLoad(GraphicsDevice gd)
        {
            GetPixelFromSource(gd);

            SComplete(gd);
        }

        public DGRP3DGeometry(string[] splitName, OBJ obj, List<int[]> indices, DGRP source, GraphicsDevice gd)
        {
            if (splitName[1] == "SPR")
            {
                PixelSPR = ushort.Parse(splitName[3]);
                PixelDir = ushort.Parse(splitName[2].Substring(3));
                Pixel = source.GetImage(1, 3, PixelDir).Sprites[PixelSPR].GetTexture(gd);
            }
            else if (splitName[1] != "MASK")
            {
                PixelSPR = ushort.Parse(splitName[2]);
                CustomTexture = 1;
                PixelDir = 65535;

                var name = source.ChunkParent.Filename.Replace('.', '_').Replace("spf", "iff").ToLowerInvariant();
                var pxSource = ReplTextureProvider(name, PixelSPR);
                if (pxSource == null)
                {
                    pxSource = DGRP3DTextureSource.WithDecoded(source.ChunkParent.Get<MTEX>(PixelSPR), gd);
                }

                PixelSource = pxSource ?? default;
            }

            GetPixelFromSource(gd);

            SVerts = new List<DGRP3DVert>();
            SIndices = new List<int>();
            var dict = new Dictionary<Tuple<int, int, int>, int>();
            var hasNormals = false;

            foreach (var ind in indices)
            {
                var tup = new Tuple<int, int, int>(ind[0], ind[1], (ind.Length > 2) ? ind[2] : -1);
                int targ;
                if (!dict.TryGetValue(tup, out targ))
                {
                    //add a vertex
                    targ = SVerts.Count;
                    Vector3 normal = Vector3.Zero;
                    if (tup.Item3 > -1)
                    {
                        normal = obj.Normals[tup.Item3 - 1];
                        hasNormals = true;
                    }
                    var vert = new DGRP3DVert(obj.Vertices[ind[0] - 1], normal, obj.TextureCoords[ind[1] - 1]);
                    vert.TextureCoordinate.Y = 1 - vert.TextureCoordinate.Y;
                    SVerts.Add(vert);
                    dict[tup] = targ;
                }
                SIndices.Add(targ);
            }

            if (!hasNormals) GenerateNormals(false);
            
            /*
            var triBase = new int[SIndices.Count / 3][];
            for (int i = 0; i < triBase.Length; i++) triBase[i] = new int[] { SIndices[i * 3], SIndices[i * 3 + 1], SIndices[i * 3 + 2] };

            var ordered = triBase.OrderBy(x => SVerts[x[0]].Position.Y + SVerts[x[1]].Position.Y + SVerts[x[2]].Position.Y);
            SIndices.Clear();
            foreach (var item in ordered) SIndices.AddRange(item);
            */

            SComplete(gd);
        }

        public void GenerateNormals(bool invert)
        {
            DGRP3DVert.GenerateNormals(invert, SVerts, SIndices);
        }

        public void Save(IoWriter io, List<DGRP3DVert> verts = null, List<int> indices = null)
        {
            if (verts == null) verts = SVerts;
            if (indices == null) indices = SIndices;

            io.WriteUInt16(PixelSPR);
            io.WriteUInt16(PixelDir);
            io.WriteInt32(SVerts.Count);
            foreach (var vert in SVerts)
            {
                io.WriteFloat(vert.Position.X);
                io.WriteFloat(vert.Position.Y);
                io.WriteFloat(vert.Position.Z);
                io.WriteFloat(vert.TextureCoordinate.X);
                io.WriteFloat(vert.TextureCoordinate.Y);
                io.WriteFloat(vert.Normal.X);
                io.WriteFloat(vert.Normal.Y);
                io.WriteFloat(vert.Normal.Z);
            }
            io.WriteInt32(SIndices.Count);
            io.WriteBytes(ToByteArray(SIndices.ToArray()));
        }

        private string GetOName(int dyn)
        {
            if (CustomTexture == 0)
            {
                return dyn + "_SPR_rot" + PixelDir + "_" + PixelSPR;
            }
            else
            {
                return dyn + "_TEX_" + PixelSPR;
            }
        }

        public void SaveOBJ(StreamWriter io, int dyn, ref int baseInd)
        {
            string o_name = GetOName(dyn);

            io.WriteLine("usemtl " + o_name);
            io.WriteLine("o " + o_name);
            foreach (var vert in SVerts)
            {
                io.WriteLine("v " + vert.Position.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Z.ToString(CultureInfo.InvariantCulture));
            }
            foreach (var vert in SVerts)
            {
                io.WriteLine("vt " + vert.TextureCoordinate.X.ToString(CultureInfo.InvariantCulture) + " " + (1 - vert.TextureCoordinate.Y).ToString(CultureInfo.InvariantCulture));
            }
            foreach (var vert in SVerts)
            {
                io.WriteLine("vn " + vert.Normal.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Normal.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Normal.Z.ToString(CultureInfo.InvariantCulture));
            }

            io.Write("f ");
            var ticker = 0;
            var j = 0;
            foreach (var ind in SIndices)
            {
                var i = ind + baseInd;
                io.Write(i + "/" + i + "/" + i + " ");
                if (++ticker == 3)
                {
                    io.WriteLine("");
                    if (j < SIndices.Count - 1) io.Write("f ");
                    ticker = 0;
                }
                j++;
            }
            baseInd += SVerts.Count;
        }

        public void SaveMTL(StreamWriter io, int dyn, string path)
        {
            var oname = GetOName(dyn);
            if (Pixel != null)
            {
                Common.Utils.GameThread.NextUpdate(x =>
                {
                    try
                    {
                        using (var io2 = File.Open(Path.Combine(path, oname + ".png"), FileMode.Create))
                            Pixel.SaveAsPng(io2, Pixel.Width, Pixel.Height);
                    }
                    catch (Exception e)
                    {

                    }
                });
            }
            io.WriteLine("newmtl " + oname);
            io.WriteLine("Ka 1.000 1.000 1.000");
            io.WriteLine("Kd 1.000 1.000 1.000");
            io.WriteLine("Ks 0.000 0.000 0.000");

            io.WriteLine("Ns 10.0000");
            io.WriteLine("illum 2");

            io.WriteLine("map_Kd " + oname + ".png");
            io.WriteLine("map_d " + oname + ".png");
        }

        public void Dispose()
        {
            Verts?.Dispose();
            Indices?.Dispose();
        }

        private static T[] ToTArray<T>(byte[] input)
        {
            var result = new T[input.Length / Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, input.Length);
            return result;
        }

        private static byte[] ToByteArray<T>(T[] input)
        {
            var result = new byte[input.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, result.Length);
            return result;
        }

        public void IncrementDataRef()
        {
            Interlocked.Increment(ref RefCount);
        }

        public void DecrementDataRef()
        {
            if (Interlocked.Decrement(ref RefCount) == 0)
            {
                SVerts = null;
                SIndices = null;
            }
        }
    }
}
