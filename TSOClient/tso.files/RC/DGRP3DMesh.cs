using FSO.Common.MeshSimplify;
using FSO.Common.Rendering;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC.Utils;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace FSO.Files.RC
{
    public class DGRP3DMesh
    {
        //1: initial 3d format
        //2: normals
        //3: depth mask (for sinks, fireplaces)
        public static int CURRENT_VERSION = 3;
        public static int CURRENT_RECONSTRUCT = 2;

        public static DGRPRCParams DefaultParams = new DGRPRCParams();
        public static Dictionary<string, DGRPRCParams> ParamsByIff = new Dictionary<string, DGRPRCParams>()
        {
            {"windows2.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false } } },
            {"windows.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false } } },
            {"windows5.iff", new DGRPRCParams() { DoorFix = true } },

            {"windowslodge.iff", new DGRPRCParams() { DoorFix = true } },
            {"doors.iff", new DGRPRCParams() { DoorFix = true } },
            {"doors5.iff", new DGRPRCParams() { DoorFix = true } },
            {"doorsmagic.iff", new DGRPRCParams() { DoorFix = true } },

            {"phones.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false }, StartDGRP = 200, EndDGRP = 207 } },

            {"countercasino.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters2.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters3.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters4.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters5.iff", new DGRPRCParams() { CounterFix = true } },
            {"counters6.iff", new DGRPRCParams() { CounterFix = true } },
            {"counterwall.iff", new DGRPRCParams() { CounterFix = true } },
            {"oj-rest-counters.iff", new DGRPRCParams() { CounterFix = true } },
            {"oj-rest-pickup-counters.iff", new DGRPRCParams() { CounterFix = true } },
            {"dishwashers.iff", new DGRPRCParams() { CounterFix = true } },
            {"trashcompactor.iff", new DGRPRCParams() { CounterFix = true } },
            {"3tileclock.iff", new DGRPRCParams() { BlenderTweak = true } },

            {"fencessuperstar.iff", new DGRPRCParams() { CounterFix = true } },
            {"fencesnowbank.iff", new DGRPRCParams() { CounterFix = true } },
            {"fencesunleashed.iff", new DGRPRCParams() { CounterFix = true } },
            {"fencelodgestone.iff", new DGRPRCParams() { CounterFix = true } },
            {"fenceparty.iff", new DGRPRCParams() { CounterFix = true } },
            {"fencecarnival.iff", new DGRPRCParams() { CounterFix = true } },
            {"fencesspellbound.iff", new DGRPRCParams() { CounterFix = true } },
            {"columnarchmagic.iff", new DGRPRCParams() { CounterFix = true } },

            {"awnings.iff", new DGRPRCParams() { CounterFix = true } },
            {"awnings3.iff", new DGRPRCParams() { CounterFix = true } },
            {"awnings4.iff", new DGRPRCParams() { CounterFix = true } },
            {"awningthatch.iff", new DGRPRCParams() { CounterFix = true } }
        };

        //STATIC: multithreading for 

        public static Queue<Action> QueuedRC = new Queue<Action>();
        public static AutoResetEvent NewRecon = new AutoResetEvent(false);

        public static bool Sync;
        public static void InitRCWorkers()
        {
            var cores = Math.Max(1, Environment.ProcessorCount-1); //maybe detect hyperthreading somehow
            for (int i=0; i<cores; i++)
            {
                var thread = new Thread(RCWorkerLoop);
                thread.Priority = ThreadPriority.BelowNormal;
                //todo: priority below normal, so we dont disrupt the game?
                thread.Start();
            }
        }

        public static void QueueWork(Action work)
        {
            if (Sync) work();
            else
            {
                lock (QueuedRC) QueuedRC.Enqueue(work);
                NewRecon.Set();
            }
        }

        public static int GetWorkCount()
        {
            lock (QueuedRC) return QueuedRC.Count;
        }

        public static void RCWorkerLoop()
        {
            while (!GameThread.Killed)
            {
                Action item = null;
                while (true)
                {
                    lock (QueuedRC)
                    {
                        if (QueuedRC.Count > 0) item = QueuedRC.Dequeue();
                        else break;
                    }
                    item?.Invoke();
                }
                WaitHandle.WaitAny(new WaitHandle[] { NewRecon, GameThread.OnKilled });
            }
        }

        //END STATIC

        public int Version = CURRENT_VERSION;
        public int ReconstructVersion;
        public string Name;
        public List<Dictionary<Texture2D, DGRP3DGeometry>> Geoms;
        public DGRP3DMaskType MaskType = DGRP3DMaskType.None;
        public DGRP3DGeometry DepthMask;
        public BoundingBox? Bounds;


        //for internal use
        private int TotalSprites;
        private int CompletedCount;
        private float MaxAllowedSq = 0.065f * 0.065f;
        public List<Vector3> BoundPts = new List<Vector3>();


        public DGRP3DMesh(DGRP dgrp, Stream source, GraphicsDevice gd)
        {
            using (var cstream = new GZipStream(source, CompressionMode.Decompress))
            {
                using (var io = IoBuffer.FromStream(cstream, ByteOrder.LITTLE_ENDIAN))
                {
                    var fsom = io.ReadCString(4);
                    Version = io.ReadInt32();
                    ReconstructVersion = io.ReadInt32();
                    if (ReconstructVersion != 0 && ReconstructVersion < CURRENT_RECONSTRUCT)
                        throw new Exception("Reconstruction outdated, must be rerun!");
                    Name = io.ReadPascalString();

                    var geomCount = io.ReadInt32();
                    Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();
                    for (int i = 0; i < geomCount; i++)
                    {
                        var d = new Dictionary<Texture2D, DGRP3DGeometry>();
                        var subCount = io.ReadInt32();
                        for (int j = 0; j < subCount; j++)
                        {
                            var geom = new DGRP3DGeometry(io, dgrp, gd, Version);
                            if (geom.Pixel == null && geom.PrimCount > 0) throw new Exception("Invalid Mesh! (old format)");
                            d.Add(geom.Pixel, geom);
                        }
                        Geoms.Add(d);
                    }

                    if (Version > 2)
                    {
                        MaskType = (DGRP3DMaskType)io.ReadInt32();
                        if (MaskType > DGRP3DMaskType.None)
                            DepthMask = new DGRP3DGeometry(io, dgrp, gd, Version);
                    }

                    var x = io.ReadFloat();
                    var y = io.ReadFloat();
                    var z = io.ReadFloat();
                    var x2 = io.ReadFloat();
                    var y2 = io.ReadFloat();
                    var z2 = io.ReadFloat();
                    Bounds = new BoundingBox(new Vector3(x, y, z), new Vector3(x2, y2, z2));
                }
            }
        }

        public string SaveDirectory;

        public DGRP3DMesh(DGRP dgrp, OBJD obj, GraphicsDevice gd, string saveDirectory)
        {
            ReconstructVersion = CURRENT_RECONSTRUCT;
            SaveDirectory = saveDirectory;
            Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();
            if (dgrp == null) return;
            Name = obj.ChunkParent.Filename.Replace('.', '_') + "_" + dgrp.ChunkID;
            var lower = obj.ChunkParent.Filename.ToLowerInvariant();
            var config = obj.ChunkParent.List<FSOR>()?.FirstOrDefault()?.Params;
            if (config == null)
            {
                if (!ParamsByIff.TryGetValue(lower, out config)) config = DefaultParams;
            }
            if (!config.InRange(dgrp.ChunkID)) config = DefaultParams;

            int totalSpr = 0;
            for (uint rotation = 0; rotation < 4; rotation++)
            {
                if (config.DoorFix)
                {
                    if ((obj.SubIndex & 0xFF) == 1)
                    {
                        if ((rotation+1)%4 > 1) continue;
                    } else
                    {
                        if ((rotation + 1) % 4 < 2) continue;
                    }
                }
                else if (!config.Rotations[rotation]) continue;
                var img = dgrp.GetImage(1, 3, rotation);
                
                var zOff = (config.BlenderTweak) ? -57.5f : -55f;

                var mat = Matrix.CreateTranslation(new Vector3(-72, -344, zOff));
                mat *= Matrix.CreateScale((1f / (128)) * 1.43f);//1.4142135623730f);
                mat *= Matrix.CreateScale(1, -1, 1);

                mat *= Matrix.CreateRotationX((float)Math.PI / -6);
                mat *= Matrix.CreateRotationY(((float)Math.PI / 4) * (1+rotation*2));

                var factor = (config.BlenderTweak) ? 0.40f : 0.39f;

                int curSpr = 0;
                foreach (var sprite in img.Sprites)
                {
                    var sprMat = mat * Matrix.CreateTranslation(new Vector3(sprite.ObjectOffset.X, sprite.ObjectOffset.Z, sprite.ObjectOffset.Y) * new Vector3(1f / 16f, 1f / 5f, 1f / 16f));
                    var inv = Matrix.Invert(sprMat);
                    var tex = sprite.GetTexture(gd);

                    if (tex == null)
                    {
                        curSpr++;
                        continue;
                    }
                    var isDynamic = sprite.SpriteID >= obj.DynamicSpriteBaseId && sprite.SpriteID < (obj.DynamicSpriteBaseId + obj.NumDynamicSprites);
                    var dynid = (isDynamic) ? (int)(1 + sprite.SpriteID - obj.DynamicSpriteBaseId) : 0;

                    while (Geoms.Count <= dynid) Geoms.Add(new Dictionary<Texture2D, DGRP3DGeometry>());

                    DGRP3DGeometry geom = null;
                    if (!Geoms[dynid].TryGetValue(tex, out geom))
                    {
                        geom = new DGRP3DGeometry() { Pixel = tex };
                        Geoms[dynid][geom.Pixel] = geom;
                    }
                    geom.PixelDir = (ushort)rotation;
                    geom.PixelSPR = (ushort)(curSpr++);
                    totalSpr++;

                    var depthB = sprite.GetDepth();

                    var useDequantize = false;
                    float[] depth = null;
                    int iterations = 125;
                    int triDivisor = 100;
                    float aggressiveness = 3.5f;
                    if (useDequantize)
                    {
                        var dtex = new Texture2D(gd, ((TextureInfo)tex.Tag).Size.X, ((TextureInfo)tex.Tag).Size.Y, false, SurfaceFormat.Color);
                        dtex.SetData(depthB.Select(x => new Color(x, x, x, x)).ToArray());
                        depth = DepthTreatment.DequantizeDepth(gd, dtex);
                        dtex.Dispose();

                        iterations = 500;
                        aggressiveness = 2.5f;
                        MaxAllowedSq = 0.05f * 0.05f;
                    }
                    else if (depthB != null)
                    {
                        depth = depthB.Select(x => x / 255f).ToArray();
                        iterations = 125;
                        aggressiveness = 3.5f;
                    }

                    if (depth == null) continue;

                    QueueWork(() =>
                    {
                        var boundPts = new List<Vector3>();
                        //begin async part
                        var w = ((TextureInfo)tex.Tag).Size.X;
                        var h = ((TextureInfo)tex.Tag).Size.Y;

                        var pos = sprite.SpriteOffset + new Vector2(72, 348 - h);
                        var tl = Vector3.Transform(new Vector3(pos, 0), sprMat);
                        var tr = Vector3.Transform(new Vector3(pos + new Vector2(w, 0), 0), sprMat);
                        var bl = Vector3.Transform(new Vector3(pos + new Vector2(0, h), 0), sprMat);
                        var tlFront = Vector3.Transform(new Vector3(pos, 110.851251f), sprMat);

                        var xInc = (tr - tl) / w;
                        var yInc = (bl - tl) / h;
                        var dFactor = (tlFront - tl) / (factor);

                        if (sprite.Flip)
                        {
                            tl = tr;
                            xInc *= -1;
                        }

                        var dict = new Dictionary<int, int>();
                        var verts = new List<VertexPositionTexture>();
                        var indices = new List<int>();

                        var lastPt = new Vector3();
                        var i = 0;
                        var verti = 0;
                        for (int y = 0; y < h; y++)
                        {
                            if (y > 0) boundPts.Add(lastPt);
                            bool first = true;
                            var vpos = tl;
                            for (int x = 0; x < w; x++)
                            {
                                var d = depth[i++];
                                if (d < 0.999f)
                                {
                                    lastPt = vpos + (1f - d) * dFactor;
                                    if (first) { boundPts.Add(lastPt); first = false; }
                                    var vert = new VertexPositionTexture(lastPt, new Vector2((float)x / w, (float)y / h));
                                    verts.Add(vert);
                                    dict.Add(y * w + x, verti++);
                                }
                                vpos += xInc;
                            }
                            tl += yInc;
                        }

                        for (int y = 0; y < h - 1; y++)
                        {
                            for (int x = 0; x < w - 1; x++)
                            {
                                //try make a triangle or two
                                var quad = new int?[] {
                                    QuickTryGet(dict, x+y*w),
                                    QuickTryGet(dict, x+1+y*w),
                                    QuickTryGet(dict, x+1+(y+1)*w),
                                    QuickTryGet(dict, x+(y+1)*w)
                                };
                                var total = quad.Sum(v => (v == null) ? 0 : 1);
                                if (total == 4)
                                {
                                    var d1 = Vector3.DistanceSquared(verts[quad[0].Value].Position, verts[quad[2].Value].Position);
                                    var d2 = Vector3.DistanceSquared(verts[quad[1].Value].Position, verts[quad[3].Value].Position);

                                    if (d1 > MaxAllowedSq || d2 > MaxAllowedSq) continue;

                                    indices.Add(quad[0].Value);
                                    indices.Add(quad[1].Value);
                                    indices.Add(quad[2].Value);

                                    indices.Add(quad[0].Value);
                                    indices.Add(quad[2].Value);
                                    indices.Add(quad[3].Value);
                                }
                                else if (total == 3)
                                {
                                    //clockwise anyways. we can only make one
                                    int? last = null;
                                    int? first = null;
                                    bool exit = false;
                                    foreach (var v in quad)
                                    {
                                        if (v != null)
                                        {
                                            if (last != null && Vector3.DistanceSquared(verts[last.Value].Position, verts[v.Value].Position) > MaxAllowedSq)
                                            {
                                                exit = true;
                                                break;
                                            }
                                            last = v.Value;
                                            if (first == null) first = last;
                                        }
                                    }

                                    if (!exit && Vector3.DistanceSquared(verts[last.Value].Position, verts[first.Value].Position) > MaxAllowedSq) exit = true;
                                    if (exit) continue;

                                    foreach (var v in quad)
                                    {
                                        if (v != null) indices.Add(v.Value);
                                    }
                                }
                            }
                        }

                                                if (config.CounterFix)
                        {
                            //x axis extrapolation
                            //clip: -0.4 to 0.4

                            //identify vertices very close to clipping range(border)
                            //! for each vertex outwith clipping range
                            //- idendify closest border pixel bp in image space
                            //- result.zy = bp.zy
                            //- result.x = (resultIMAGE.x - bpIMAGE.x) / 64;
                            //- clip x to -0.5, 0.5f.

                            var clip = 0.4;
                            var bWidth = 0.02;
                            var border1 = new List<Tuple<Vector2, Vector3>>();
                            var invalid1 = new List<KeyValuePair<int, int>>();
                            var border2 = new List<Tuple<Vector2, Vector3>>();
                            var invalid2 = new List<KeyValuePair<int, int>>();
                            foreach (var vert in dict) {
                                var vpos = verts[vert.Value].Position;
                                var dist = Math.Abs(vpos.X);
                                if (dist > clip)
                                {
                                    if (vpos.X > 0)
                                        invalid1.Add(vert);
                                    else
                                        invalid2.Add(vert);
                                } else if (dist > (clip - bWidth))
                                {
                                    if (vpos.X > 0)
                                        border1.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                                    else
                                        border2.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                                }
                            }

                            var edge = 0.498f + 0.001f * (rotation % 2);

                            if (border1.Count > 0)
                            {
                                foreach (var vert in invalid1)
                                {
                                    var vstr = verts[vert.Value];
                                    var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                                    var vpos = vstr.Position;
                                    var closest = border1.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                                    vpos.X = closest.Item2.X + Vector2.Distance(closest.Item1, pos2d) / 71.55f;
                                    if (vpos.X > 0.5f)
                                    {
                                        vpos.X = edge;
                                    }
                                    else
                                    {
                                        vpos.Y = closest.Item2.Y;
                                        vpos.Z = closest.Item2.Z;
                                    }

                                    vstr.Position = vpos;
                                    verts[vert.Value] = vstr;
                                }
                            }

                            if (border2.Count > 0)
                            {
                                foreach (var vert in invalid2)
                                {
                                    var vstr = verts[vert.Value];
                                    var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                                    var vpos = vstr.Position;
                                    var closest = border2.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                                    vpos.X = closest.Item2.X - Vector2.Distance(closest.Item1, pos2d) / 71.55f;
                                    if (vpos.X < -0.5f)
                                    {
                                        vpos.X = -edge;
                                    }
                                    else
                                    {
                                        vpos.Y = closest.Item2.Y;
                                        vpos.Z = closest.Item2.Z;
                                    }

                                    vstr.Position = vpos;
                                    verts[vert.Value] = vstr;
                                }
                            }
                        }


                        lock (BoundPts) BoundPts.AddRange(boundPts);
                        var useSimplification = config.Simplify;

                        if (useSimplification)
                        {
                            var simple = new Simplify();
                            simple.vertices = verts.Select(x => new MSVertex() { p = x.Position, t = x.TextureCoordinate }).ToList();
                            for (int t = 0; t < indices.Count; t += 3)
                            {
                                simple.triangles.Add(new MSTriangle()
                                {
                                    v = new int[] { indices[t], indices[t + 1], indices[t + 2] }
                                });
                            }
                            simple.simplify_mesh(simple.triangles.Count / triDivisor, agressiveness: aggressiveness, iterations: iterations);

                            verts = simple.vertices.Select(x =>
                            {
                                var iv = Vector3.Transform(x.p, inv);
                                //DGRP3DVert
                                return new VertexPositionTexture(x.p,
                                    new Vector2(
                                        (sprite.Flip) ? (1 - ((iv.X - pos.X + 0.5f) / w)) : ((iv.X - pos.X + 0.5f) / w),
                                        (iv.Y - pos.Y + 0.5f) / h));
                            }
                                ).ToList();
                            indices.Clear();
                            foreach (var t in simple.triangles)
                            {
                                indices.Add(t.v[0]);
                                indices.Add(t.v[1]);
                                indices.Add(t.v[2]);
                            }

                            GameThread.NextUpdate(x =>
                            {
                                if (geom.SVerts == null)
                                {
                                    geom.SVerts = new List<DGRP3DVert>();
                                    geom.SIndices = new List<int>();
                                }

                                var bID = geom.SVerts.Count;
                                foreach (var id in indices) geom.SIndices.Add(id + bID);
                                var verts2 = verts.Select(v => new DGRP3DVert(v.Position, Vector3.Zero, v.TextureCoordinate)).ToList();
                                DGRP3DVert.GenerateNormals(!sprite.Flip, verts2, indices);
                                geom.SVerts.AddRange(verts2);

                                lock (this)
                                {
                                    if (++CompletedCount == TotalSprites) Complete(gd);
                                }
                            });
                        }
                        else
                        {
                            GameThread.NextUpdate(x =>
                            {
                                if (geom.SVerts == null)
                                {
                                    geom.SVerts = new List<DGRP3DVert>();
                                    geom.SIndices = new List<int>();
                                }

                                var baseID = geom.SVerts.Count;
                                foreach (var id in indices) geom.SIndices.Add(id + baseID);
                                var verts2 = verts.Select(v => new DGRP3DVert(v.Position, Vector3.Zero, v.TextureCoordinate)).ToList();
                                DGRP3DVert.GenerateNormals(!sprite.Flip, verts2, indices);
                                geom.SVerts.AddRange(verts2);
                                lock (this)
                                {
                                    if (++CompletedCount == TotalSprites) Complete(gd);
                                }
                            });
                        }
                    });
                }
            }
            TotalSprites = totalSpr;

        }

        /// <summary>
        /// Create a DGRPMesh from a .OBJ file.
        /// </summary>
        public DGRP3DMesh(DGRP dgrp, OBJ source, GraphicsDevice gd)
        {
            Bounds = source.Vertices.Count>0?BoundingBox.CreateFromPoints(source.Vertices):new BoundingBox();
            Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();
            if (dgrp == null) return;
            Name = dgrp.ChunkParent.Filename.Replace('.', '_').Replace("spf", "iff") + "_" + dgrp.ChunkID;

            foreach (var obj in source.FacesByObjgroup.OrderBy(x => x.Key))
            {
                if (obj.Key == "_default") continue;
                var split = obj.Key.Split('_');
                if (split[0] == "DEPTH")
                {
                    DepthMask = new DGRP3DGeometry(split, source, obj.Value, dgrp, gd);
                    if (split.Length > 2 && split[2] == "PORTAL")
                    {
                        MaskType = DGRP3DMaskType.Portal;

                        var verts = new List<Vector3>();
                        var objs = source.FacesByObjgroup.Where(x => !x.Key.StartsWith("DEPTH_MASK_PORTAL")).Select(x => x.Value);
                        foreach (var obj2 in objs)
                        {
                            foreach (var tri in obj2)
                            {
                                verts.Add(source.Vertices[tri[0] - 1]);
                            }
                        }
                        
                        Bounds = BoundingBox.CreateFromPoints(verts);
                    }
                    else
                        MaskType = DGRP3DMaskType.Normal;
                }
                else
                {
                    //0: dynsprite id, 1: SPR or custom, 2: rotation, 3: index
                    var id = int.Parse(split[0]);
                    while (Geoms.Count <= id) Geoms.Add(new Dictionary<Texture2D, DGRP3DGeometry>());
                    var dict = Geoms[id];
                    var geom = new DGRP3DGeometry(split, source, obj.Value, dgrp, gd);
                    dict[geom.Pixel] = geom;
                }
            }
        }

        private void Complete(GraphicsDevice gd)
        {
            Bounds = (BoundPts.Count == 0) ? new BoundingBox() : BoundingBox.CreateFromPoints(BoundPts);
            BoundPts = null;
            Save();
            foreach (var g in Geoms)
                foreach (var e in g)
                {
                    e.Value.SComplete(gd);
                }
        }

        public void Save()
        {
            if (SaveDirectory == null) return;
            var dir = Path.Combine(SaveDirectory, Name + ".fsom");
            Directory.CreateDirectory(SaveDirectory);
            using (var stream = File.Open(dir, FileMode.Create))
            {
                using (var cstream = new GZipStream(stream, CompressionMode.Compress))
                    Save(cstream);
            }
        }

        public void Save(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteCString("FSOm", 4);
                io.WriteInt32(CURRENT_VERSION);
                io.WriteInt32(ReconstructVersion);
                io.WritePascalString(Name);

                io.WriteInt32(Geoms.Count);
                foreach (var g in Geoms)
                {
                    io.WriteInt32(g.Count);
                    foreach (var m in g.Values)
                    {
                        m.Save(io);
                    }
                }

                io.WriteInt32((int)MaskType);
                if (DepthMask != null)
                {
                    DepthMask.Save(io);
                }

                var b = Bounds.Value;
                io.WriteFloat(b.Min.X);
                io.WriteFloat(b.Min.Y);
                io.WriteFloat(b.Min.Z);
                io.WriteFloat(b.Max.X);
                io.WriteFloat(b.Max.Y);
                io.WriteFloat(b.Max.Z);
            }
        }

        public void SaveOBJ(Stream stream, string filename)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Generated by the FreeSO FSOm Exporter tool.");
                io.WriteLine("# Meshes can be cleaned up then re-imported via Volcanic.");
                io.WriteLine("# One material per object... Note that material names must follow this format:");
                io.WriteLine("# - '$_SPR_rot#_#': uses the texture from the SPR this DGRP would normally use, ");
                io.WriteLine("#                   at the given rotation and index.");
                io.WriteLine("# - '$_TEX_#': import a custom PNG texture at the given chunk ID. Textures can be ");
                io.WriteLine("#              shared across multiple DGRPs by using the same ID.");
                io.WriteLine("# Replace $ with the dynamic sprite index. 0 means base. (untoggleable)");
                io.WriteLine("# Textures are assumed to have a filename equivalent to their material name, plus png.");


                io.WriteLine("mtllib "+filename+".mtl");
                io.WriteLine("s 1");

                int dyn = 0;
                int indCount = 1;
                foreach (var g in Geoms)
                {
                    foreach (var m in g.Values)
                    {
                        m.SaveOBJ(io, dyn, ref indCount);
                    }
                    dyn++;
                }
            }
        }

        public void SaveMTL(Stream stream, string path)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Generated by the FreeSO FSOm Exporter tool.");
                io.WriteLine("# Contains material information for exported objects.");
                io.WriteLine("# See the associated .obj file for more information.");

                int dyn = 0;
                foreach (var g in Geoms)
                {
                    foreach (var m in g.Values)
                    {
                        m.SaveMTL(io, dyn, path);
                    }
                    dyn++;
                }
            }
        }


        private int? QuickTryGet(Dictionary<int, int> dict, int pt)
        {
            int result;
            if (dict.TryGetValue(pt, out result)) return result;
            return null;
        }
    }

    public enum DGRP3DMaskType
    {
        None = 0,
        Normal = 1,
        Portal = 2
    }
}
