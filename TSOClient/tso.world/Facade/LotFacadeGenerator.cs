using FSO.Common.MeshSimplify;
using FSO.Common.Utils;
using FSO.Files.RC;
using FSO.LotView.Model;
using FSO.LotView.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Facade
{
    /// <summary>
    /// A lot facade is a minimal 3D representation of a lot to be used from city or neighbourhood view.
    /// It includes textured walls, simple roof geometry (sharing textures via the content system) and floor textures for each level.
    /// 
    /// The texture interpolation mode for the lot facade is a little weird. We premultiply alpha and use bilinear
    /// interpolation to get smooth texturing, but the premultiplied alpha is actually divided out of the RGB on draw,
    /// and alpha is actually an on/off that discards when a is less than 0.5. This gives us hard edges 
    /// </summary>
    public class LotFacadeGenerator
    {
        public static int WALL_WIDTH = 8;
        public static int WALL_HEIGHT = 22;
        public static int MAX_WALL_WIDTH = 64; //above this tile width the pixel width for the wall will not increase.
        public static int GAP = 1;

        public int FLOOR_RES_PER_TILE = 2;
        public int FLOOR_TILES = 64;//98;

        public int GROUND_SUBDIV = 5;

        public int LastIndex;
        public string LotName = "lot";

        private List<LotFacadeWallBin> WallBins = new List<LotFacadeWallBin>();
        public RasterizerState Scissor = new RasterizerState() { ScissorTestEnable = true, CullMode = CullMode.None };

        public RenderTarget2D WallTarget;
        public VertexPositionTexture[] WallVerts;
        public int[] WallIndices;

        public Texture2D RoofTexture;
        public VertexPositionTexture[] RoofVerts;
        public int[] RoofIndices;

        public Texture2D FloorTexture;
        public VertexPositionTexture[] FloorVerts;
        public int[] FloorIndices;

        public bool RoofOnFloor;

        public void GenerateWalls(GraphicsDevice gd, WorldRC world, Blueprint bp, bool justTexture)
        {
            //generate wall geometry and texture.
            if (!justTexture)
            {
                foreach (var room in bp.Rooms)
                {
                    //add walls for each outside room.
                    if (room.Base == room.RoomID && room.IsOutside)
                    {
                        var walls = new List<Vector2[]>(room.WallLines);
                        walls.AddRange(room.FenceLines);
                        foreach (var wall in walls)
                        {
                            var facadeWall = new LotFacadeWall(wall, room);
                            AddToBin(facadeWall);
                        }
                    }
                }
            }

            //ok, allocate the texture for the wall.
            var tex = new RenderTarget2D(gd, MAX_WALL_WIDTH * WALL_WIDTH, CeilToFour(Math.Max(1, WallBins.Count * (WALL_HEIGHT + GAP * 2) - GAP * 2)), false, SurfaceFormat.Color, DepthFormat.Depth24);
            gd.SetRenderTarget(tex);
            gd.DepthStencilState = DepthStencilState.Default;
            gd.Clear(Color.TransparentBlack);
            //ace, let's draw each wall

            var oldLevel = world.State.Level;
            world.State.SilentLevel = bp.Stories;
            world.State.ZeroWallOffset = true;
            var cuts = bp.Cutaway;
            bp.Cutaway = new bool[cuts.Length];
            bp.WCRC?.Generate(gd, world.State, false);

            int wallCount = 0;
            int bini = 0;
            foreach (var bin in WallBins)
            {
                var yPos = bini * (WALL_HEIGHT + GAP * 2);
                var xPos = 0;
                foreach (var wall in bin.Walls)
                {
                    wallCount++;
                    //get a camera for this wall first
                    //create a look at matrix for the wall. first find a camera point at one side of the wall,
                    //then create an orthographic projection with the size of the wall in mind. 

                    var ctr = (wall.Points[0] + wall.Points[1]) / (2 * 16);
                    var rNorm = wall.Points[1] - wall.Points[0];
                    rNorm = new Vector2(rNorm.Y, -rNorm.X);
                    rNorm.Normalize();
                    //which side is "outside"?
                    //check one side. assume the other is outside if we fail
                    var testPos = (ctr + rNorm * 0.6f).ToPoint();
                    if (testPos.X >= 0 && testPos.X < bp.Width && testPos.Y >= 0 && testPos.Y < bp.Height)
                    {
                        var room = bp.RoomMap[wall.Room.Floor][testPos.X + testPos.Y * bp.Width];
                        if (!bp.Rooms[bp.Rooms[(ushort)room].Base].IsOutside)
                        {
                            rNorm *= -1;
                        }
                    }

                    var height = (wall.Room.Floor + 0.5f) * 2.95f * 3 + bp.InterpAltitude(new Vector3(ctr, 0)) * 3f + 0.2f;
                    var camp = (ctr + rNorm) * 3;
                    var objp = ctr * 3;
                    var lookat = Matrix.CreateLookAt(new Vector3(camp.X, height, camp.Y), new Vector3(objp.X, height, objp.Y), Vector3.Up);
                    var ortho = Matrix.CreateOrthographic(3 * wall.PhysicalLength, 2.90f * 3, 0, 6);

                    //rescale our camera matrix to render to the correct part of the render target. Apply scissor test for that area.
                    var rect = new Rectangle(xPos + (wall.EffectiveLength - (wall.Length + GAP)), yPos, wall.Length, WALL_HEIGHT);
                    gd.RasterizerState = Scissor;
                    gd.ScissorRectangle = rect;

                    var trans = Matrix.CreateScale((rect.Width / ((float)tex.Width)), (rect.Height / ((float)tex.Height)), 1) *
                        Matrix.CreateTranslation((-(rect.X * -2 - wall.Length) / (float)tex.Width) - 1f, (-(rect.Y * 2 + WALL_HEIGHT - 2) / (float)tex.Height) + 1f, 0);

                    var frustrum = new BoundingFrustum(lookat * ortho);
                    ortho = ortho * trans;

                    //draw the walls and objects for this wall segment. this is a little slow right now.
                    var effect = WorldContent.RCObject;
                    gd.BlendState = BlendState.NonPremultiplied;
                    var vp = lookat * ortho;
                    effect.Parameters["ViewProjection"].SetValue(vp);

                    bp.WCRC?.Draw(gd, world.State);

                    effect.CurrentTechnique = effect.Techniques["Draw"];

                    var objs = bp.Objects.Where(x => x.Level >= wall.Room.Floor - 5 && frustrum.Intersects(((ObjectComponentRC)x).GetBounds()))
                        .OrderBy(x => ((ObjectComponentRC)x).SortDepth(vp));
                    foreach (var obj in objs)
                    {
                        obj.Draw(gd, world.State);
                    }

                    xPos += wall.EffectiveLength;
                }
                bini++;
            }
            gd.RasterizerState = RasterizerState.CullNone;

            bp.Cutaway = cuts;
            bp.WCRC?.Generate(gd, world.State, false);
            world.State.ZeroWallOffset = false;
            world.State.SilentLevel = oldLevel;

            //generate wall geometry
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);

            var verts = new VertexPositionTexture[wallCount * 4];
            var indices = new int[wallCount * 6];

            bini = 0;
            var verti = 0;
            var indi = 0;
            foreach (var bin in WallBins)
            {
                var xInt = 0;
                var yInt = bini * (WALL_HEIGHT + GAP * 2);
                var yPos = bini * (WALL_HEIGHT + GAP * 2) / (float)tex.Height;
                var xPos = 0f;
                var div = WALL_HEIGHT / (float)tex.Height;
                foreach (var wall in bin.Walls)
                {
                    var rect = new Rectangle(xInt + (wall.EffectiveLength - (wall.Length + GAP)), yInt, wall.Length, WALL_HEIGHT);
                    BleedRect(data, rect, tex.Width, tex.Height);

                    if (!justTexture)
                    {
                        var ctr = (wall.Points[0] + wall.Points[1]) / (2 * 16);
                        var off = (wall.EffectiveLength - (wall.Length + GAP)) / (float)tex.Width;
                        var height1 = ((wall.Room.Floor) * 2.95f + bp.InterpAltitude(new Vector3(ctr, 0)));
                        var height2 = height1 + 2.95f;
                        var pt1 = wall.Points[0] / 16f;
                        var pt2 = wall.Points[1] / 16f;
                        verts[verti++] = new VertexPositionTexture(new Vector3(pt1.X, height2, pt1.Y), new Vector2(xPos + off, yPos));
                        verts[verti++] = new VertexPositionTexture(new Vector3(pt2.X, height2, pt2.Y), new Vector2(xPos + off + wall.Length / (float)tex.Width, yPos));
                        verts[verti++] = new VertexPositionTexture(new Vector3(pt2.X, height1, pt2.Y), new Vector2(xPos + off + wall.Length / (float)tex.Width, (yPos + div)));
                        verts[verti++] = new VertexPositionTexture(new Vector3(pt1.X, height1, pt1.Y), new Vector2(xPos + off, (yPos + div)));

                        indices[indi++] = verti - 2;
                        indices[indi++] = verti - 3;
                        indices[indi++] = verti - 4;

                        indices[indi++] = verti - 4;
                        indices[indi++] = verti - 1;
                        indices[indi++] = verti - 2;

                        xInt += wall.EffectiveLength;
                        xPos += wall.EffectiveLength / (float)tex.Width;
                    }
                }
                bini++;
            }

            //using (var fs = new FileStream(@"C:\Users\Rhys\Desktop\walls.png", FileMode.Create, FileAccess.Write))
            //    tex.SaveAsPng(fs, tex.Width, tex.Height);

            tex.SetData(data);

            if (!justTexture)
            {
                WallTarget = tex;
                WallVerts = verts;
                WallIndices = indices;
            }
        }

        public void GenerateRoof(GraphicsDevice gd, WorldRC world, Blueprint bp)
        {
            RoofTexture = bp.RoofComp.Texture;
            int baseIndex = 0;
            var verts = new List<VertexPositionTexture>();
            var inds = new List<int>();
            bp.RoofComp.RegenRoof(gd);

            var basepos = new Vector2(bp.Width - FLOOR_TILES, bp.Height - FLOOR_TILES) * 1.5f;

            for (int i = 1; i < bp.Stories + 1; i++)
            {
                var basetc = new Vector2((1 / 3f) * ((Math.Min(i, 4) % 3)+1), (1 / 2f) * ((Math.Min(i, 4) / 3) + 1));
                var data = bp.RoofComp.MeshRectData(i + 1);
                if (RoofOnFloor)
                    verts.AddRange(data.Item1.Select(x => new VertexPositionTexture(x.Position / 3f, basetc - new Vector2((x.Position.X-basepos.X) / (3f*FLOOR_TILES*3), (x.Position.Z- basepos.Y) / (3f * FLOOR_TILES * 2)))));
                else
                    verts.AddRange(data.Item1.Select(x => new VertexPositionTexture(x.Position / 3f, new Vector2(x.GrassInfo.Y, x.GrassInfo.Z))));
                inds.AddRange(data.Item2.Select(x => x + baseIndex));
                baseIndex += data.Item1.Length;
            }

            RoofVerts = verts.ToArray();
            RoofIndices = inds.ToArray();
        }

        public void GenerateFloor(GraphicsDevice gd, WorldRC world, Blueprint bp, bool justTexture)
        {
            var dim = FLOOR_RES_PER_TILE * FLOOR_TILES;
            var tex = new RenderTarget2D(gd, dim * 3, dim * 2, false, SurfaceFormat.Color, DepthFormat.Depth24);
            gd.SetRenderTarget(tex);
            gd.Clear(Color.TransparentBlack);
            var lookat = Matrix.CreateLookAt(new Vector3(bp.Width * 1.5f, 200, bp.Height * 1.5f), new Vector3(bp.Width * 1.5f, 0, bp.Height * 1.5f), new Vector3(0, 0, 1));
            var baseO = Matrix.CreateOrthographic(FLOOR_TILES * 3f, FLOOR_TILES * 3f, 0, 400);

            var oldLevel = world.State.SilentLevel;
            for (int i = 0; i < bp.Stories + 1; i++) {
                world.State.SilentLevel = (sbyte)(i + 1);
                var x = i % 3;
                var y = i / 3;
                var offMat = Matrix.CreateScale(1 / 3f, 1 / 2f, 1f) * Matrix.CreateTranslation(-1 + ((x + 0.5f) * 2 / 3f), 1 - ((y + 0.5f) * 2 / 2f), 0);

                gd.RasterizerState = Scissor;
                gd.ScissorRectangle = new Rectangle(dim * x + 1, dim * y + 1, dim - 2, dim - 2);

                if (i == bp.Stories)
                {
                    var effect = WorldContent.RCObject;
                    gd.BlendState = BlendState.NonPremultiplied;
                    var vp = lookat * baseO * offMat;
                    effect.Parameters["ViewProjection"].SetValue(vp);
                    var frustrum = new BoundingFrustum(lookat * baseO);

                    effect.CurrentTechnique = effect.Techniques["Draw"];

                    var objs = bp.Objects.Where(o => o.Level == 1 && frustrum.Intersects(((ObjectComponentRC)o).GetBounds())).OrderBy(o => ((ObjectComponentRC)o).SortDepth(vp));
                    foreach (var obj in objs)
                    {
                        obj.Draw(gd, world.State);
                    }
                }
                else
                {
                    var floors = new HashSet<sbyte>();
                    floors.Add((sbyte)i);
                    var mat = baseO * offMat;
                    bp.Terrain.DrawCustom(gd, world.State, lookat, mat, 1, floors);
                    if (i == 0) bp.Terrain.DrawMask(gd, world.State, lookat, mat);

                    var effect = WorldContent.RCObject;
                    gd.BlendState = BlendState.NonPremultiplied;
                    var vp = lookat * baseO * offMat;
                    effect.Parameters["ViewProjection"].SetValue(vp);
                    var frustrum = new BoundingFrustum(lookat * baseO);

                    effect.CurrentTechnique = effect.Techniques["Draw"];

                    var objs = bp.Objects.Where(o => o.Level == i+1 && frustrum.Intersects(((ObjectComponentRC)o).GetBounds())).OrderBy(o => ((ObjectComponentRC)o).SortDepth(vp));
                    foreach (var obj in objs)
                    {
                        obj.Draw(gd, world.State);
                    }

                    if (RoofOnFloor)
                    {
                        //gd.DepthStencilState = DepthStencilState.None;
                        gd.DepthStencilState = DepthStencilState.Default;
                        if (i > 0) bp.RoofComp.DrawOne(gd, lookat, mat, world.State, i - 1);
                        if (i == bp.Stories - 1)
                        {
                            bp.RoofComp.DrawOne(gd, lookat, mat, world.State, i);
                        }
                        gd.DepthStencilState = DepthStencilState.Default;
                    }
                }
            }
            world.State.SilentLevel = oldLevel;

            /*
            using (var fs = new FileStream(@"C:\Users\Rhys\Desktop\floor.png", FileMode.Create, FileAccess.Write))
                tex.SaveAsPng(fs, tex.Width, tex.Height);
                */

            if (!justTexture)
            {
                var vertDiv = GROUND_SUBDIV + 1;
                var inc = FLOOR_TILES / (float)GROUND_SUBDIV;
                var invDiv = 1 / (float)GROUND_SUBDIV;
                var basepos = new Vector2(bp.Width - FLOOR_TILES, bp.Height - FLOOR_TILES) / 2;
                var basetc = new Vector2(1 / 3f, 1 / 2f);
                FloorVerts = new VertexPositionTexture[vertDiv * vertDiv];

                //output vertices
                int verti = 0;
                for (int y = 0; y < vertDiv; y++)
                {
                    for (int x = 0; x < vertDiv; x++)
                    {
                        var pos = basepos + new Vector2(x * inc, y * inc);
                        var height = bp.InterpAltitude(new Vector3(pos, 0));
                        FloorVerts[verti++] = new VertexPositionTexture(new Vector3(pos.X, height, pos.Y), basetc - new Vector2(x * invDiv / 3, y * invDiv / 2));
                    }
                }

                //output indices
                int indi = 0;

                FloorIndices = new int[GROUND_SUBDIV * GROUND_SUBDIV * 6];
                for (int y = 0; y < GROUND_SUBDIV; y++)
                {
                    for (int x = 0; x < GROUND_SUBDIV; x++)
                    {
                        var baseVert = x + y * vertDiv;
                        FloorIndices[indi++] = baseVert + 1 + vertDiv;
                        FloorIndices[indi++] = baseVert + 1;
                        FloorIndices[indi++] = baseVert;

                        FloorIndices[indi++] = baseVert;
                        FloorIndices[indi++] = baseVert + vertDiv;
                        FloorIndices[indi++] = baseVert + 1 + vertDiv;
                    }
                }

            }
            FloorTexture = tex;
            gd.SetRenderTarget(null);
        }

        private byte[] TexToData(Texture2D tex, bool compressed)
        {
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            if (compressed)
            {
                //let's assume the width and height didn't change. the default settings should always result in textures divisible by 4.
                return TextureUtils.DXT5Compress(data, tex.Width, tex.Height).Item1; 
            }
            return ToByteArray(data.Select(x => x.PackedValue).ToArray());
        }

        private static byte[] ToByteArray<T>(T[] input)
        {
            var result = new byte[input.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(input, 0, result, 0, result.Length);
            return result;
        }

        public void SimplifyFloor()
        {
            var simple = new Simplify();
            simple.vertices = FloorVerts.Select(x => new MSVertex() { p = x.Position, t = x.TextureCoordinate }).ToList();
            for (int t = 0; t < FloorIndices.Length; t += 3)
            {
                simple.triangles.Add(new MSTriangle()
                {
                    v = new int[] { FloorIndices[t], FloorIndices[t + 1], FloorIndices[t + 2] }
                });
            }
            simple.simplify_mesh(2, agressiveness: 3, iterations: 300);

            FloorVerts = simple.vertices.Select(x =>
            {
                //DGRP3DVert
                return new VertexPositionTexture(x.p, x.t);
            }).ToArray();
            var indices = new List<int>();
            foreach (var t in simple.triangles)
            {
                indices.Add(t.v[0]);
                indices.Add(t.v[1]);
                indices.Add(t.v[2]);
            }
            FloorIndices = indices.ToArray();
        }

        public FSOF GetFSOF(GraphicsDevice gd, WorldRC world, Blueprint bp, Action onNight, bool compressed)
        {
            var result = new FSOF();
            RoofOnFloor = true;
            GenerateWalls(gd, world, bp, false);
            //GROUND_SUBDIV = 64;
            GenerateFloor(gd, world, bp, false);
            //SimplifyFloor();
            GenerateRoof(gd, world, bp);

            result.TexCompressionType = (compressed) ? 1 : 0;
            result.FloorWidth = FloorTexture.Width;
            result.FloorHeight = FloorTexture.Height;
            result.WallWidth = WallTarget.Width;
            result.WallHeight = WallTarget.Height;

            result.FloorTextureData = TexToData(FloorTexture, compressed);
            result.WallTextureData = TexToData(WallTarget, compressed);

            var tVerts = new List<DGRP3DVert>();
            var tInd = new List<int>();
            var indOff = 0;
            for (int i = 0; i < 5; i++)
            {
                var tcOffset = new Vector2((i % 3) / 3f, (i / 3) / 2f);
                //save each floor. offset the floor for each level
                var posOffset = i * 2.95f;
                var fVerts = FloorVerts.Select(x => new DGRP3DVert(new Vector3(x.Position.X, x.Position.Y+posOffset, x.Position.Z), Vector3.Zero, x.TextureCoordinate + tcOffset));
                tVerts.AddRange(fVerts);
                tInd.AddRange(FloorIndices.Select(x => x + indOff));

                indOff = tVerts.Count;

                if (i == 0)
                {
                    tcOffset = new Vector2((5 % 3) / 3f, (5 / 3) / 2f);
                    //save each floor. offset the floor for each level
                    posOffset = 0.5f * 2.95f;
                    fVerts = FloorVerts.Select(x => new DGRP3DVert(new Vector3(x.Position.X, x.Position.Y + posOffset, x.Position.Z), Vector3.Zero, x.TextureCoordinate + tcOffset));
                    tVerts.AddRange(fVerts);
                    tInd.AddRange(FloorIndices.Select(x => x + indOff));
                    indOff = tVerts.Count;
                }
            }

            tVerts.AddRange(RoofVerts.Select(x => new DGRP3DVert(x.Position, Vector3.Zero, x.TextureCoordinate)));
            tInd.AddRange(RoofIndices.Select(x => x + indOff));

            DGRP3DVert.GenerateNormals(false, tVerts, FloorIndices);
            result.FloorVertices = tVerts.ToArray();
            result.FloorIndices = tInd.ToArray();

            var tempVerts = WallVerts.Select(x => new DGRP3DVert(x.Position, Vector3.Zero, x.TextureCoordinate)).ToList();
            DGRP3DVert.GenerateNormals(false, tempVerts, WallIndices);
            result.WallVertices = tempVerts.ToArray();
            result.WallIndices = WallIndices;

            onNight();
            GenerateWalls(gd, world, bp, true);
            GenerateFloor(gd, world, bp, true);

            result.NightFloorTextureData = TexToData(FloorTexture, compressed);
            result.NightWallTextureData = TexToData(WallTarget, compressed);
            result.NightLightColor = world.State.OutsideColor;

            return result;
        }

        public void Generate(GraphicsDevice gd, WorldRC world, Blueprint bp)
        {
            GenerateWalls(gd, world, bp, false);
            //GROUND_SUBDIV = 64;
            GenerateFloor(gd, world, bp, false);
            //SimplifyFloor();
            GenerateRoof(gd, world, bp);
        }

        private void BleedRect(Color[] img, Rectangle rect, int width, int height)
        {
            //let's assume the rectangle itself will be in bounds.
            //bleed the sides first
            var h = rect.Height;
            var lo = 0;
            var w = rect.Width;
            if (rect.X > 0) {
                int i = rect.Y*width + rect.X;
                for (int y = 0; y < h; y++)
                {
                    img[i - 1] = img[i];
                    i += width;
                }
                lo = -1;
                w++;
            }
            if (rect.Right < width)
            {
                int i = rect.Y * width + rect.Right;
                for (int y = 0; y < h; y++)
                {
                    img[i] = img[i - 1];
                    i += width;
                }
                w++;
            }
            if (rect.Y > 0)
            {
                int i = (rect.Y - 1) * width + rect.X + lo;
                for (int x = 0; x < w; x++)
                {
                    img[i] = img[i + width];
                    i++;
                }
            }

            if (rect.Bottom < height)
            {
                int i = rect.Bottom * width + rect.X + lo;
                for (int x = 0; x < w; x++)
                {
                    img[i] = img[i - width];
                    i++;
                }
            }
        }

        public void SaveToPath(string dirname)
        {
            var filename = Path.Combine(dirname, "lot.obj");
            using (var io = File.Open(filename, FileMode.Create, FileAccess.Write))
                SaveOBJ(io, Path.GetFileNameWithoutExtension(filename));
            var ext = Path.GetExtension(filename);
            using (var io = File.Open(filename.Substring(0, filename.Length - ext.Length) + ".mtl", FileMode.Create))
                SaveMTL(io, dirname);
        }

        public void SaveOBJData(StreamWriter io, VertexPositionTexture[] verts, int[] indices, ref int baseInd, string o_name)
        {
            if (indices.Length == 0) return;
            io.WriteLine("usemtl " + o_name);
            io.WriteLine("o " + o_name);
            foreach (var vert in verts)
            {
                io.WriteLine("v " + vert.Position.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Z.ToString(CultureInfo.InvariantCulture));
            }
            foreach (var vert in verts)
            {
                io.WriteLine("vt " + vert.TextureCoordinate.X.ToString(CultureInfo.InvariantCulture) + " " + (1 - vert.TextureCoordinate.Y).ToString(CultureInfo.InvariantCulture));
            }
            io.Write("f ");
            var ticker = 0;
            var j = 0;
            foreach (var ind in indices)
            {
                var i = ind + baseInd;
                io.Write(i + "/" + i + " ");
                if (++ticker == 3)
                {
                    io.WriteLine("");
                    if (j < indices.Length - 1) io.Write("f ");
                    ticker = 0;
                }
                j++;
            }
            baseInd += verts.Length;
        }
        public int? TexBase;

        public void SaveOBJ(Stream stream, string filename)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("mtllib " + filename + ".mtl");
                io.WriteLine("s 1");

                AppendOBJ(io, filename, 1, null);
            }
        }

        public void AppendOBJ(StreamWriter io, string filename, int indCount, Vector3? off)
        {
            if (off != null)
            {
                var o = off.Value;
                SaveOBJData(io, WallVerts.Select(x => new VertexPositionTexture(x.Position + o, x.TextureCoordinate)).ToArray(), WallIndices, ref indCount, LotName + "_walls");
                SaveOBJData(io, RoofVerts.Select(x => new VertexPositionTexture(x.Position + o, x.TextureCoordinate)).ToArray(), RoofIndices, ref indCount, LotName + "_roof");
            }
            else
            {
                if (TexBase == null)
                {
                    SaveOBJData(io, WallVerts, WallIndices, ref indCount, LotName + "_walls");
                    SaveOBJData(io, RoofVerts, RoofIndices, ref indCount, LotName + ((RoofOnFloor) ? "_floor":"_roof"));
                } else
                {
                    SaveOBJData(io, WallVerts, WallIndices, ref indCount, "TEX_"+TexBase);
                    SaveOBJData(io, RoofVerts, RoofIndices, ref indCount, "TEX_"+(TexBase+((RoofOnFloor)?1:2)));
                }
            }
            for (int i = 0; i < 5; i++)
            {
                //save each floor. offset the floor for each level
                var floorName = (TexBase == null)?(LotName + "_floor"): "TEX_" + (TexBase+1);
                var posOffset = i * 2.95f;
                var tcOffset = new Vector2((i % 3) / 3f, (i / 3) / 2f);
                var o = off ?? Vector3.Zero;
                SaveOBJData(io, FloorVerts.Select(x =>
                    new VertexPositionTexture(new Vector3(x.Position.X, x.Position.Y + posOffset, x.Position.Z) + o, x.TextureCoordinate + tcOffset)).ToArray(),
                    FloorIndices, ref indCount, floorName);

                if (i == 0)
                {
                    SaveOBJData(io, FloorVerts.Select(x =>
                        new VertexPositionTexture(new Vector3(x.Position.X, x.Position.Y + 2.95f / 3f, x.Position.Z) + o, x.TextureCoordinate + new Vector2(2 / 3f, 1 / 2f))).ToArray(),
                        FloorIndices, ref indCount, floorName);
                }
            }
            LastIndex = indCount;
        }

        public void SaveMTL(Stream stream, string path)
        {
            using (var io = new StreamWriter(stream))
            {
                AppendMTL(io, path);
            }
        }

        public void AppendMTL(StreamWriter io, string path)
        {
            var tex = TexBase != null;
            SaveMTLData(io, path, tex?("TEX_" + (TexBase)):(LotName + "_walls"), WallTarget);
            if (!RoofOnFloor) SaveMTLData(io, path, tex ? ("TEX_" + (TexBase + 2)) : (LotName + "_roof"), RoofTexture);
            SaveMTLData(io, path, tex ? ("TEX_" + (TexBase + 1)) : (LotName + "_floor"), FloorTexture);
        }

        public void SaveMTLData(StreamWriter io, string path, string oname, Texture2D tex)
        {
            if (tex != null)
            {
                Common.Utils.GameThread.NextUpdate(x =>
                {
                    try
                    {
                        using (var io2 = File.Open(Path.Combine(path, oname + ".png"), FileMode.Create))
                            tex.SaveAsPng(io2, tex.Width, tex.Height);
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


        private int CeilToFour(int size)
        {
            return ((size + 3) / 4) * 4;
        }

        private int AddToBin(LotFacadeWall wall)
        {
            int i = 0;
            while (true)
            {
                if (i >= WallBins.Count) WallBins.Add(new LotFacadeWallBin());
                if (WallBins[i].TryAdd(wall)) return i;
                i++;
            }
        }

        private class LotFacadeWallBin
        {
            //simple first fit algorithm for walls.

            public List<LotFacadeWall> Walls = new List<LotFacadeWall>();
            public int Space = MAX_WALL_WIDTH * WALL_WIDTH;
            public bool TryAdd(LotFacadeWall wall)
            {
                var effectiveLength = wall.Length;
                if (Walls.Count > 0) effectiveLength += GAP; //we need space behind us
                if (effectiveLength > Space) return false; //try the next bin
                else
                {
                    //space after us is not necessary when we hit the edge, so this gap is added after the space check.
                    effectiveLength += GAP;
                    Space -= effectiveLength;
                    if (Space < 0) Space = 0;
                    wall.EffectiveLength = effectiveLength;
                    Walls.Add(wall);
                    return true;
                }   
            }
        }

        private class LotFacadeWall
        {
            public Vector2[] Points;
            public Room Room;

            public float PhysicalLength;
            public int Length;
            public int EffectiveLength;

            public LotFacadeWall(Vector2[] points, Room room)
            {
                Room = room;
                Points = points;
                PhysicalLength = (Points[0] - Points[1]).Length()/16f;
                Length = Math.Min(MAX_WALL_WIDTH * WALL_WIDTH, (int)Math.Round(PhysicalLength * WALL_WIDTH));
            }
        }
    }


}
