using FSO.Common;
using FSO.Common.MeshSimplify;
using FSO.Common.Rendering;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC.Utils;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.Files.RC
{
    public class DGRP3DMesh
    {
        //1: initial 3d format
        //2: normals
        //3: depth mask (for sinks, fireplaces)
        public static int CURRENT_VERSION = 3;
        public static int CURRENT_RECONSTRUCT = 15;

        public static DGRPRCParams DefaultParams = new DGRPRCParams();
        public static Dictionary<string, DGRPRCParams> ParamsByIff = new Dictionary<string, DGRPRCParams>()
        {
            {"windows2.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false } } },
            {"windows.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false } } },
            {"windows5.iff", new DGRPRCParams() { DoorFix = true } },

            {"windowslodge.iff", new DGRPRCParams() { DoorFix = true } },
            {"doors.iff", new DGRPRCParams() { DoorFix = true, Flags = DGRPRCFlags.DisableFlip } },
            {"doors5.iff", new DGRPRCParams() { DoorFix = true } },
            {"doorsmagic.iff", new DGRPRCParams() { DoorFix = true } },

            {"phones.iff", new DGRPRCParams() { Rotations = new bool[] {true, true, false, false }, StartDGRP = 200, EndDGRP = 207 } },

            {"countercasino.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters2.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters3.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters4.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters5.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counters6.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"counterwall.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"oj-rest-counters.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"oj-rest-pickup-counters.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFix } },
            {"dishwashers.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },
            {"trashcompactor.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },
            {"3tileclock.iff", new DGRPRCParams() { BlenderTweak = true } },

            {"fencessuperstar.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"fencesnowbank.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"fencesunleashed.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"fencelodgestone.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"fencecarnival.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"fencesspellbound.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },
            {"columnarchmagic.iff", new DGRPRCParams() { Flags = DGRPRCFlags.ObjectFenceFix } },

            {"awnings.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },
            {"awnings3.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },
            {"awnings4.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },
            {"awningthatch.iff", new DGRPRCParams() { Flags = DGRPRCFlags.CounterFixBasic } },

            {"plantboxfenceunleashed.iff", new DGRPRCParams() { Flags = DGRPRCFlags.TileFix } },
            {"fenceflowersnow.iff", new(DGRPRCFlags.ObjectFenceFix) },
            {"conveyorbelt.iff", new(DGRPRCFlags.TileFix) },
            {"castlefence.iff", new(DGRPRCFlags.ObjectFenceFix) },
            {"fenceshd.iff", new(DGRPRCFlags.ObjectFenceFix) },
            {"flowersoutdoor.iff", new(DGRPRCFlags.TileFix) },
            {"columnarchscifi.iff", new(DGRPRCFlags.ObjectFenceFix) },

            {"fenceparty.iff", new(DGRPRCFlags.ObjectFenceFix) },
            {"fences.iff", new(DGRPRCFlags.ObjectFenceFix) },

            // depends on dgrp # (box left mid l t x)
            {"fencesstonevacation.iff", new(DGRPRCFlags.ObjectFence6Graphic) },
            {"plantboxfence.iff", new(DGRPRCFlags.ObjectFence6Graphic) },

            // needs one flip to complete the other side?
            // tablesend5
            
            {"elevatorfreight.iff", new(DGRPRCFlags.TileFix) { Rotations = [true, false, true, false] } },
            {"elevatorhotel.iff", new(DGRPRCFlags.TileFix) { Rotations = [true, false, true, false] } },
            {"elevatormodern.iff", new(DGRPRCFlags.TileFix) { Rotations = [true, false, true, false] } },
            {"paintings2.iff", new(DGRPRCFlags.DisableFlip | DGRPRCFlags.BackfaceAdjust) },
            {"paintings3.iff", new(DGRPRCFlags.DisableFlip | DGRPRCFlags.BackfaceAdjust) },
            {"paintings4.iff", new(DGRPRCFlags.DisableFlip | DGRPRCFlags.BackfaceAdjust) },
            {"paintings7.iff", new(DGRPRCFlags.DisableFlip | DGRPRCFlags.BackfaceAdjust) },
            {"mirrors2.iff", new(DGRPRCFlags.DisableFlip | DGRPRCFlags.BackfaceAdjust) },
            {"windows4.iff", new(DGRPRCFlags.DisableFlip) },

            {"timer.iff", new(DGRPRCFlags.DisableFlip) },
            {"fso_fireball.iff", new(DGRPRCFlags.IncreaseDensity) },
        };

        /// <summary>
        /// Disabling mesh simplification for these trees costs too much for the benefit.
        /// </summary>
        public string[] TreeExclusions =
        [
            "Poplar",
            "Cypress",
            "Saguaro",
            "AgaveBloom",
            "PricklyPear",
            "Banyan",
            "Banyon",
            "Barrel Palm"
        ];

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

        public static void SaveAsync(DGRP3DMesh mesh)
        {
            mesh.IncrementDataRef();
            QueueWork(() =>
            {
                mesh.Save();
                mesh.DecrementDataRef();
            });
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

        private List<List<DGRP3DGeometry>> UnloadedGeoms;

        /// <summary>
        /// Create a DGRP3DMesh from FSOM data.
        /// </summary>
        /// <remarks>The source stream will be disposed by this method, either immediately or when asset streaming completes.</remarks>
        /// <param name="dgrp">The DGRP this mesh represents</param>
        /// <param name="source">Source stream containing FSOM mesh data</param>
        /// <param name="gd">Graphics device</param>
        public DGRP3DMesh(DGRP dgrp, OBJD obj, Stream source, GraphicsDevice gd)
        {
            Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();
            if (AssetStreaming.LoadingType > AssetStreamingMode.None)
            {
                ReconstructVersion = CURRENT_RECONSTRUCT;
                Name = "Loading";

                AssetStreaming.AddLoadingResource();

                Task.Run(() =>
                {
                    try
                    {
                        LoadData(dgrp, source, gd);
                    }
                    catch
                    {
                        AssetStreaming.InStreamUpdate(() =>
                        {
                            CleanupFailedLoad(dgrp, obj, gd, null);
                            AssetStreaming.RemoveLoadingResource();
                        });
                    }
                    finally
                    {
                        source.Dispose();
                    }

                    AssetStreaming.InStreamUpdate(() =>
                    {
                        CompleteFSOMLoad(gd);

                        AssetStreaming.RemoveLoadingResource();
                    });
                });
            }
            else
            {
                try
                {
                    LoadData(dgrp, source, gd);
                }
                catch (Exception e)
                {

                }
                finally
                {
                    source.Dispose();
                }

                CompleteFSOMLoad(gd);
            }
        }

        /// <summary>
        /// Create a DGRP3DMesh from FSOM data.
        /// </summary>
        /// <param name="dgrp">The DGRP this mesh represents</param>
        /// <param name="filePath">Path to a file containing FSOM mesh data</param>
        /// <param name="gd">Graphics device</param>
        public DGRP3DMesh(DGRP dgrp, OBJD obj, string filePath, GraphicsDevice gd)
        {
            Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();
            if (AssetStreaming.LoadingType > AssetStreamingMode.None)
            {
                ReconstructVersion = CURRENT_RECONSTRUCT;
                Name = "Loading";

                AssetStreaming.AddLoadingResource();

                Task.Run(() =>
                {
                    // Open the stream in the task, as the file ctor can be a bit expensive.
                    try
                    {
                        using (var source = File.OpenRead(filePath))
                        {
                            LoadData(dgrp, source, gd);
                        }
                    }
                    catch
                    {
                        AssetStreaming.InStreamUpdate(() =>
                        {
                            CleanupFailedLoad(dgrp, obj, gd, filePath);
                            AssetStreaming.RemoveLoadingResource();
                        });
                    }

                    AssetStreaming.InStreamUpdate(() =>
                    {
                        CompleteFSOMLoad(gd);

                        AssetStreaming.RemoveLoadingResource();
                    });
                });
            }
            else
            {
                using (var source = File.OpenRead(filePath))
                {
                    LoadData(dgrp, source, gd);
                }

                CompleteFSOMLoad(gd);
            }
        }

        private void CleanupFailedLoad(DGRP dgrp, OBJD obj, GraphicsDevice gd, string filePath)
        {
            UnloadedGeoms?.Clear();

            if (obj != null)
            {
                GenerateMesh(dgrp, obj, gd);
            }
            else
            {
                CompleteFSOMLoad(gd);
            }
        }

        private void LoadData(DGRP dgrp, Stream source, GraphicsDevice gd)
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
                    UnloadedGeoms = new List<List<DGRP3DGeometry>>();
                    for (int i = 0; i < geomCount; i++)
                    {
                        var d = new List<DGRP3DGeometry>();
                        var subCount = io.ReadInt32();
                        for (int j = 0; j < subCount; j++)
                        {
                            var geom = new DGRP3DGeometry(io, dgrp, gd, Version);
                            //if (geom.Pixel == null && geom.PrimCount > 0) throw new Exception("Invalid Mesh! (old format)"); //TODO?
                            d.Add(geom);
                        }
                        UnloadedGeoms.Add(d);
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

        private void CompleteFSOMLoad(GraphicsDevice gd)
        {
            if (UnloadedGeoms != null)
            {
                foreach (var group in UnloadedGeoms)
                {
                    var d = new Dictionary<Texture2D, DGRP3DGeometry>();
                    foreach (var geom in group)
                    {
                        geom.CompleteFSOMLoad(gd);

                        if (geom.Pixel != null)
                        {
                            d.Add(geom.Pixel, geom);
                        }
                    }

                    Geoms.Add(d);
                }
            }

            DepthMask?.CompleteFSOMLoad(gd);
        }

        public string SaveDirectory;

        private static void ExtrapolateEdges(List<VertexPositionTexture> verts, Dictionary<int, int> dict, uint rotation, int w, bool xp, bool yp, bool xn, bool yn, bool noEdge)
        {
            //axis extrapolation
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

            if (xp || xn)
            {
                foreach (var vert in dict)
                {
                    var vpos = verts[vert.Value].Position;
                    var dist = Math.Abs(vpos.X);
                    if (dist > clip)
                    {
                        if (vpos.X > 0)
                            invalid1.Add(vert);
                        else
                            invalid2.Add(vert);
                    }
                    else if (dist > (clip - bWidth))
                    {
                        if (vpos.X > 0)
                            border1.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                        else
                            border2.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                    }
                }

                var edge = 0.499f + 0.001f * (rotation % 2);

                if (border1.Count > 0 && xp)
                {
                    foreach (var vert in invalid1)
                    {
                        var vstr = verts[vert.Value];
                        var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                        var vpos = vstr.Position;
                        var closest = border1.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                        var dist = Vector2.Distance(closest.Item1, pos2d);

                        vpos.X = closest.Item2.X + Vector2.Distance(closest.Item1, pos2d) / 71.55f;

                        if (noEdge || vpos.X <= 0.5f)
                        {
                            vpos.Y = closest.Item2.Y;
                            vpos.Z = closest.Item2.Z;
                        }

                        if (vpos.X > 0.5f)
                        {
                            vpos.X = edge;
                        }

                        vstr.Position = vpos;
                        verts[vert.Value] = vstr;
                    }
                }

                if (border2.Count > 0 && xn)
                {
                    foreach (var vert in invalid2)
                    {
                        var vstr = verts[vert.Value];
                        var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                        var vpos = vstr.Position;
                        var closest = border2.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                        vpos.X = closest.Item2.X - Vector2.Distance(closest.Item1, pos2d) / 71.55f;

                        if (noEdge || vpos.X >= -0.5f)
                        {
                            vpos.Y = closest.Item2.Y;
                            vpos.Z = closest.Item2.Z;
                        }

                        if (vpos.X < -0.5f)
                        {
                            vpos.X = -edge;
                        }

                        vstr.Position = vpos;
                        verts[vert.Value] = vstr;
                    }
                }

                if (yp || yn)
                {
                    border1.Clear();
                    invalid1.Clear();
                    border2.Clear();
                    invalid2.Clear();
                }
            }

            if (yp || yn)
            {
                foreach (var vert in dict)
                {
                    var vpos = verts[vert.Value].Position;
                    var dist = Math.Abs(vpos.Z);
                    if (dist > clip)
                    {
                        if (vpos.Z > 0)
                            invalid1.Add(vert);
                        else
                            invalid2.Add(vert);
                    }
                    else if (dist > (clip - bWidth))
                    {
                        if (vpos.Z > 0)
                            border1.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                        else
                            border2.Add(new Tuple<Vector2, Vector3>(new Vector2(vert.Key % w, vert.Key / w), vpos));
                    }
                }

                var edge = 0.499f + 0.001f * (rotation / 2);

                if (border1.Count > 0 && yp)
                {
                    foreach (var vert in invalid1)
                    {
                        var vstr = verts[vert.Value];
                        var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                        var vpos = vstr.Position;
                        var closest = border1.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                        vpos.Z = closest.Item2.Z + Vector2.Distance(closest.Item1, pos2d) / 71.55f;

                        if (noEdge || vpos.Z <= 0.5f)
                        {
                            vpos.Y = closest.Item2.Y;
                            vpos.X = closest.Item2.X;
                        }

                        if (vpos.Z > 0.5f)
                        {
                            vpos.Z = edge;
                        }

                        vstr.Position = vpos;
                        verts[vert.Value] = vstr;
                    }
                }

                if (border2.Count > 0 && yn)
                {
                    foreach (var vert in invalid2)
                    {
                        var vstr = verts[vert.Value];
                        var pos2d = new Vector2(vert.Key % w, vert.Key / w);
                        var vpos = vstr.Position;
                        var closest = border2.OrderBy(x => Vector2.DistanceSquared(x.Item1, pos2d)).First();

                        vpos.Z = closest.Item2.Z - Vector2.Distance(closest.Item1, pos2d) / 71.55f;

                        if (noEdge || vpos.Z >= -0.5f)
                        {
                            vpos.Y = closest.Item2.Y;
                            vpos.X = closest.Item2.X;
                        }

                        if (vpos.Z < -0.5f)
                        {
                            vpos.Z = -edge;
                        }

                        vstr.Position = vpos;
                        verts[vert.Value] = vstr;
                    }
                }
            }
        }

        public DGRP3DMesh(DGRP dgrp, OBJD obj, GraphicsDevice gd)
        {
            Geoms = new List<Dictionary<Texture2D, DGRP3DGeometry>>();

            GenerateMesh(dgrp, obj, gd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong EdgeID(int i1, int i2)
        {
            return (((ulong)Math.Min((uint)i1, (uint)i2)) << 32) | ((ulong)Math.Max((uint)i1, (uint)i2));
        }

        private void Extrude(List<int> indices, List<VertexPositionTexture> vertices, float dist)
        {
            Dictionary<ulong, int> edgeCount = [];

            var inds = CollectionsMarshal.AsSpan(indices);
            var verts = CollectionsMarshal.AsSpan(vertices);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void addEdge(int i1, int i2)
            {
                ulong id = EdgeID(i1, i2);

                if (!edgeCount.TryGetValue(id, out int count))
                {
                    edgeCount[id] = 1;
                }
                else
                {
                    edgeCount[id] += count;
                }
            }

            for (int i = 0; i < inds.Length; i += 3)
            {
                addEdge(inds[i], inds[i + 1]);
                addEdge(inds[i + 1], inds[i + 2]);
                addEdge(inds[i + 2], inds[i]);
            }

            Dictionary<int, (int, Vector3)> borderVertices = [];

            foreach (var pair in edgeCount)
            {
                if (pair.Value == 1)
                {
                    var id = pair.Key;
                    borderVertices[(int)id] = default;
                    borderVertices[(int)(id >> 32)] = default;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void offsetBorder(ref (int, Vector3) accumulator, int iFrom, int iTo, Span<VertexPositionTexture> verts)
            {
                var vFrom = verts[iFrom].Position;
                var vTo = verts[iTo].Position;

                var dir = vTo - vFrom;
                dir.Normalize();

                if (!float.IsNaN(dir.X))
                {
                    accumulator.Item1++;
                    accumulator.Item2 += dir;
                }
            }

            for (int i = 0; i < inds.Length; i += 3)
            {
                int i0 = inds[i];
                int i1 = inds[i + 1];
                int i2 = inds[i + 2];

                bool border0 = borderVertices.TryGetValue(i0, out var accumulator0);
                bool border1 = borderVertices.TryGetValue(i1, out var accumulator1);
                bool border2 = borderVertices.TryGetValue(i2, out var accumulator2);

                if (border0)
                {
                    if (!border1) offsetBorder(ref accumulator0, i1, i0, verts);
                    if (!border2) offsetBorder(ref accumulator0, i2, i0, verts);
                    borderVertices[i0] = accumulator0;
                }

                if (border1)
                {
                    if (!border0) offsetBorder(ref accumulator1, i0, i1, verts);
                    if (!border2) offsetBorder(ref accumulator1, i2, i1, verts);
                    borderVertices[i1] = accumulator1;
                }

                if (border2)
                {
                    if (!border1) offsetBorder(ref accumulator2, i1, i2, verts);
                    if (!border0) offsetBorder(ref accumulator2, i0, i2, verts);
                    borderVertices[i2] = accumulator2;
                }
            }

            foreach (var pair in borderVertices)
            {
                var index = pair.Key;
                var accumulator = pair.Value;

                if (accumulator.Item1 > 0)
                {
                    var dir = accumulator.Item2;
                    dir.Normalize();
                    verts[index].Position += dir * dist;
                }
            }
        }

        private void GenerateMesh(DGRP dgrp, OBJD obj, GraphicsDevice gd)
        {
            var saveDirectory = Path.Combine(FSOEnvironment.UserDir, "MeshCache/");
            ReconstructVersion = CURRENT_RECONSTRUCT;
            SaveDirectory = saveDirectory;
            if (dgrp == null) return;
            Name = obj.ChunkParent.Filename.Replace('.', '_') + "_" + dgrp.ChunkID;
            var lower = obj.ChunkParent.Filename.ToLowerInvariant();
            var config = obj.ChunkParent.List<FSOR>()?.FirstOrDefault()?.Params;
            if (config == null)
            {
                if (!ParamsByIff.TryGetValue(lower, out config)) config = DefaultParams;
            }
            if (!config.InRange(dgrp.ChunkID)) config = DefaultParams;

            if (obj.ChunkParent.Filename.Contains("tree", StringComparison.InvariantCultureIgnoreCase) && !TreeExclusions.Any(x => obj.ChunkLabel.Contains(x)))
            {
                config = new DGRPRCParams(config);
                config.Flags |= DGRPRCFlags.IncreaseDensity;
            }

            bool disableFlip = config.Flags.HasFlag(DGRPRCFlags.DisableFlip);

            int totalSpr = 0;
            for (uint rotation = 0; rotation < 4; rotation++)
            {
                if (config.DoorFix)
                {
                    if ((obj.SubIndex & 0xFF) == 1)
                    {
                        if ((rotation + 1) % 4 > 1) continue;
                    }
                    else
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
                mat *= Matrix.CreateRotationY(((float)Math.PI / 4) * (1 + rotation * 2));

                var factor = (config.BlenderTweak) ? 0.40f : 0.39f;
                bool isBack = (rotation == 0 || rotation == 3);

                int curSpr = 0;
                foreach (var sprite in img.Sprites)
                {
                    if (disableFlip && sprite.Flip)
                    {
                        continue;
                    }

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

                    if (depthB == null) continue;

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
                        iterations = 125;
                        aggressiveness = 3.5f;
                    }

                    QueueWork(() =>
                    {
                        if (depth == null && depthB != null)
                        {
                            depth = depthB.Select(x => x / 255f).ToArray();
                        }

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
                                int total = 0;
                                Span<int?> quad = [
                                    QuickTryGet(dict, x+y*w, ref total),
                                    QuickTryGet(dict, x+1+y*w, ref total),
                                    QuickTryGet(dict, x+1+(y+1)*w, ref total),
                                    QuickTryGet(dict, x+(y+1)*w, ref total)
                                ];

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

                        if (config.Flags.HasFlag(DGRPRCFlags.IncreaseDensity))
                        {
                            Extrude(indices, verts, xInc.Length() * 2f);
                        }

                        int index = dgrp.ChunkID - obj.BaseGraphicID;

                        bool mergeVertices = false;

                        if (config.Flags.HasFlag(DGRPRCFlags.CounterFixBasic) || (config.Flags.HasFlag(DGRPRCFlags.CounterFix) && index == 0))
                        {
                            ExtrapolateEdges(verts, dict, rotation, w, true, false, true, false, false);
                            mergeVertices = true;
                        }
                        else if (config.Flags.HasFlag(DGRPRCFlags.CounterFix) && index == 1)
                        {
                            ExtrapolateEdges(verts, dict, rotation, w, true, false, false, true, false);
                            mergeVertices = true;
                        }
                        else if (config.Flags.HasFlag(DGRPRCFlags.TileFix) || config.Flags.HasFlag(DGRPRCFlags.ObjectFenceFix))
                        {
                            ExtrapolateEdges(verts, dict, rotation, w, true, true, true, true, config.Flags.HasFlag(DGRPRCFlags.ObjectFenceFix));
                            mergeVertices = true;
                        }
                        else if (config.Flags.HasFlag(DGRPRCFlags.ObjectFence6Graphic))
                        {
                            mergeVertices = true;

                            bool xp = false;
                            bool yp = false;
                            bool xn = false;
                            bool yn = false;

                            switch (index)
                            {
                                case 1:
                                    xn = true;
                                    break;
                                case 2:
                                    xn = true;
                                    xp = true;
                                    break;
                                case 3:
                                    xp = true;
                                    yp = true;
                                    break;
                                case 4:
                                    xp = true;
                                    xn = true;
                                    yp = true;
                                    break;
                                case 5:
                                    xp = true;
                                    xn = true;
                                    yp = true;
                                    yn = true;
                                    break;
                            }

                            ExtrapolateEdges(verts, dict, rotation, w, xp, yp, xn, yn, true);
                        }

                        lock (BoundPts) BoundPts.AddRange(boundPts);
                        var useSimplification = config.Simplify && !config.Flags.HasFlag(DGRPRCFlags.IncreaseDensity);

                        if (useSimplification)
                        {
                            if (mergeVertices)
                            {
                                //MergeVertices(verts, indices);
                            }

                            var vertices = verts.Select(x => new MSVertex() { p = x.Position, t = x.TextureCoordinate }).ToArray();
                            var triangles = new MSTriangle[indices.Count / 3];
                            int ind = 0;
                            for (int t = 0; t < indices.Count; t += 3)
                            {
                                var i1 = indices[t];
                                var i2 = indices[t + 1];
                                var i3 = indices[t + 2];

                                triangles[ind++] = (new MSTriangle()
                                {
                                    v = new MSTriangleIndices(i1, i2, i3)
                                });
                            }

                            var simple = new Simplify(triangles, vertices, ind);

                            simple.simplify_mesh(triangles.Length / triDivisor, agressiveness: aggressiveness, iterations: iterations);

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
                                indices.Add(t.v.i0);
                                indices.Add(t.v.i1);
                                indices.Add(t.v.i2);
                            }
                        }

                        var verts2 = verts.Select(v => new DGRP3DVert(v.Position, Vector3.Zero, v.TextureCoordinate)).ToList();
                        DGRP3DVert.GenerateNormals(!sprite.Flip, verts2, indices);

                        if (config.Flags.HasFlag(DGRPRCFlags.BackfaceAdjust) && isBack)
                        {
                            for (int j = 0; j < verts2.Count; j++)
                            {
                                var vert = verts2[j];
                                vert.Position.Z += 0.01f;
                                verts2[j] = vert;
                            }
                        }

                        AssetStreaming.InStreamUpdate(() =>
                        {
                            if (geom.SVerts == null)
                            {
                                geom.SVerts = new List<DGRP3DVert>();
                                geom.SIndices = new List<int>();
                            }

                            var bID = geom.SVerts.Count;
                            foreach (var id in indices) geom.SIndices.Add(id + bID);
                            geom.SVerts.AddRange(verts2);

                            lock (this)
                            {
                                if (++CompletedCount == TotalSprites) Complete(gd);
                            }
                        });

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
            SaveAsync(this);
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
                using (var memstream = new MemoryStream())
                {
                    Save(memstream);

                    memstream.Position = 0;
                    using (var cstream = new GZipStream(stream, CompressionMode.Compress))
                        memstream.CopyTo(cstream);
                }
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

        public void IncrementDataRef()
        {
            foreach (var geom in Geoms)
            {
                foreach (var texGeom in geom.Values)
                {
                    texGeom.IncrementDataRef();
                }
            }
        }

        public void DecrementDataRef()
        {
            foreach (var geom in Geoms)
            {
                foreach (var texGeom in geom.Values)
                {
                    texGeom.DecrementDataRef();
                }
            }
        }


        private int? QuickTryGet(Dictionary<int, int> dict, int pt, ref int count)
        {
            int result;
            if (dict.TryGetValue(pt, out result))
            {
                count++;
                return result;
            }
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
