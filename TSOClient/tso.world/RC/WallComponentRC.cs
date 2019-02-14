using FSO.Content.Model;
using FSO.LotView.LMap;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.RC
{
    /// <summary>
    /// This component allows FreeSO to draw walls in 3D. 
    /// This is not enabled in 2D mode due to it misrepresenting certain wall directions.
    /// 
    /// Basically, wall geometry is generated as if the wall were thick around the tile edge. 
    /// At runtime, this is offset back.
    /// </summary>
    public class WallComponentRC
    {
        public Blueprint blueprint;

        public List<Dictionary<Tuple<Texture2D, Texture2D>, WallGroupRC>> GroupsByTexture = new List<Dictionary<Tuple<Texture2D, Texture2D>, WallGroupRC>>();

        public Dictionary<ushort, Wall> WallCache = new Dictionary<ushort, Wall>();
        public Dictionary<ushort, WallStyle> WallStyleCache = new Dictionary<ushort, WallStyle>();

        private Wall GetPattern(ushort id)
        {
            if (!WallCache.ContainsKey(id)) WallCache.Add(id, Content.Content.Get().WorldWalls.Get(id));
            return WallCache[id];
        }

        private WallStyle GetStyle(ushort id)
        {
            if (!WallStyleCache.ContainsKey(id)) WallStyleCache.Add(id, Content.Content.Get().WorldWalls.GetWallStyle(id));
            return WallStyleCache[id];
        }

        public static Vector2[] CutCheckDir =
        {
            new Vector2(-1,-1),
            new Vector2(-1,1),
            new Vector2(1,1),
            new Vector2(1,-1)
        };

        private bool HasRoof(int x, int y, int level)
        {
            return !TileIndoors(x, y, level) && TileIndoors(x, y, level - 1);
        }

        private bool TileIndoors(int x, int y, int level)
        {
            var room = blueprint.RoomMap[level - 1][x + y * blueprint.Width];
            var room1 = room & 0xFFFF;
            var room2 = (room >> 16) & 0x7FFF;
            if (room1 < blueprint.Rooms.Count && !blueprint.Rooms[(int)room1].IsOutside) return true;
            if (room2 > 0 && room2 < blueprint.Rooms.Count && !blueprint.Rooms[(int)room2].IsOutside) return true;
            return false;
        }

        public void Generate(GraphicsDevice device, WorldState world, bool cutaway)
        {
            var wallContent = Content.Content.Get().WorldWalls;
            var floorContent = Content.Content.Get().WorldFloors;
            if (!cutaway) Dispose();
            var white = Color.White.ToVector4();
            var whitepx = Common.Utils.TextureGenerator.GetPxWhite(device);
            var darker = new Vector4(0.85f, 0.85f, 0.85f, 1);
            var wallTop = new Color(151, 120, 76, 255).ToVector4();
            var wallHeight = new Vector3(0, 0, 2.95f);
            var thickness = 0.15f/2;
            var thickDiag = thickness;
            var xz = CutCheckDir[(int)world.CutRotation] * 0.66f;
            if (float.IsNaN(xz.X)) xz = new Vector2();

            //draw walls
            for (sbyte level = 1; level <= blueprint.Floors.Length; level++)
            {
                bool canCut = (level == world.Level);
                if (cutaway && !canCut) continue;
                if (GroupsByTexture.Count < level) GroupsByTexture.Add(new Dictionary<Tuple<Texture2D, Texture2D>, WallGroupRC>());
                else
                {
                    //clear previous data for cutaway
                    foreach (var grp2 in GroupsByTexture[level - 1].Values)
                    {
                        grp2.Dispose();
                    }
                    GroupsByTexture[level - 1].Clear();
                }
                var grp = GroupsByTexture[level - 1];

                var rMap = blueprint.RoomMap[level - 1];

                Func<Vector3, float> GetWallHeight = (Vector3 pos) =>
                {
                    if (level != world.Level) return 1;
                    pos.X += xz.X; pos.Y += xz.Y;
                    var index = (int)pos.Y * blueprint.Width + (int)pos.X;
                    if (index < 0 || index > blueprint.Cutaway.Length) return 1;
                    return blueprint.Cutaway[index] ? 0.12f : 1;
                };

                for (short y = 0; y < blueprint.Height; y++)
                {
                    for (short x = 0; x < blueprint.Height; x++)
                    {


                        Action<Vector2, Vector2, ushort, ushort, int, float, float, float> addLineGeom = (Vector2 from, Vector2 to, ushort pattern, ushort style, int topMode, float starttc, float endtc, float aboveFloor) => {
                            var tex = world._2D.GetTexture(GetPattern(pattern)?.Near?.Frames[2]);
                            var mask = world._2D.GetTexture(GetStyle(style)?.WallsUpNear?.Frames[(topMode != 4)?0:2]);

                            var g = Fetch(tex, mask, grp);
                            g.UseOffset = (topMode != 4);

                            var p1 = new Vector3(from.X + x, from.Y + y, 0);
                            var a1 = blueprint.InterpAltitude(p1) + (level-1)*2.95f;
                            p1.Z = a1;
                            var p2 = new Vector3(to.X + x, to.Y + y, 0);
                            var a2 = blueprint.InterpAltitude(p2) + (level-1) * 2.95f;
                            p2.Z = a2;

                            //generate the geometry for this line
                            var l = level - 1;
                            var baseI = g.Verts.Count;
                            var h1 = (topMode == 4) ? 1: GetWallHeight(p1);
                            var h2 = (topMode == 4) ? 1 : GetWallHeight(p2);
                            var col = (from.X == to.X) ? darker : white;
                            g.Verts.Add(new WallVertexRC(p1, col, new Vector3(starttc, l, aboveFloor)));
                            g.Verts.Add(new WallVertexRC(p2, col, new Vector3(endtc, l, aboveFloor)));
                            g.Verts.Add(new WallVertexRC(p1+ wallHeight*h1, col, new Vector3(starttc, h1 + l, aboveFloor)));
                            g.Verts.Add(new WallVertexRC(p2+ wallHeight*h2, col, new Vector3(endtc, h2 + l, aboveFloor)));

                            g.Indices.Add(baseI); g.Indices.Add(baseI + 2); g.Indices.Add(baseI + 1);
                            g.Indices.Add(baseI + 2); g.Indices.Add(baseI+3); g.Indices.Add(baseI + 1);

                            if (topMode < 4)
                            {
                                var g2 = Fetch(whitepx, whitepx, grp);

                                baseI = g2.Verts.Count;
                                Vector3 toBack = Vector3.Zero;

                                switch (topMode)
                                {
                                    case 0:
                                        toBack = new Vector3(-thickness*2, 0, 0);
                                        break;
                                    case 1:
                                        toBack = new Vector3(0, -thickness * 2, 0);
                                        break;
                                    case 2:
                                        toBack = new Vector3(thickness * -2, thickness * -2, 0);
                                        break;
                                    case 3:
                                        toBack = new Vector3(thickness * 2, thickness * -2, 0);
                                        break;
                                }

                                var vec = new Vector2(0, 1 + l);
                                h1 -= 0.001f; h2 -= 0.001f;
                                g2.Verts.Add(new WallVertexRC(p1 + wallHeight*h1, wallTop, new Vector3(0, h1 + l, aboveFloor)));
                                g2.Verts.Add(new WallVertexRC(p2 + wallHeight*h2, wallTop, new Vector3(0, h2 + l, aboveFloor)));
                                g2.Verts.Add(new WallVertexRC(p1 + wallHeight*h1 + toBack, wallTop, new Vector3(0, h1 + l, aboveFloor)));
                                g2.Verts.Add(new WallVertexRC(p2 + wallHeight*h2 + toBack, wallTop, new Vector3(0, h2 + l, aboveFloor)));

                                g2.Indices.Add(baseI); g2.Indices.Add(baseI + 2); g2.Indices.Add(baseI + 1);
                                g2.Indices.Add(baseI + 2); g2.Indices.Add(baseI + 3); g2.Indices.Add(baseI + 1);
                            }
                        };

                        var comp = blueprint.GetWall(x, y, level);
                        if (comp.Segments != 0)
                        {
                            float bleedLight = (level == 1 || blueprint.GetFloor(x, y, level).Pattern != 0 || HasRoof(x, y, level)) ? 0 : 1;
                            if ((comp.Segments & WallSegments.TopLeft) > 0)
                            {
                                if (comp.TopLeftThick)
                                {
                                    float extentBack = 0;
                                    float extentFront = 0;
                                    float bleedLight2 = (level == 1 || blueprint.GetFloor((short)(x-1), y, level).Pattern != 0 || HasRoof(x-1, y, level)) ? 0 : 1;
                                    if (y > 0 && !blueprint.GetWall(x, (short)(y - 1), level).TopLeftThick)
                                    {
                                        extentBack = -thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(thickness, extentBack + 0.005f), new Vector2(-thickness, extentBack + 0.005f), comp.TopLeftPattern, 1, 5, 0, thickness * 2, bleedLight);
                                    }
                                    if (y < blueprint.Height - 1 && !blueprint.GetWall(x, (short)(y + 1), level).TopLeftThick)
                                    {
                                        extentFront = thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(-thickness, 1 + extentFront - 0.005f), new Vector2(thickness, 1 + extentFront - 0.005f), comp.TopLeftPattern, 1, 5, 0, thickness * 2, bleedLight);
                                    }
                                    if (x > 0)
                                        addLineGeom(new Vector2(-thickness, extentBack), new Vector2(-thickness, 1 + extentFront), blueprint.GetWall((short)(x - 1), y, level).BottomRightPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 5, -(0 + extentBack), -(1 + extentFront), bleedLight2);
                                    addLineGeom(new Vector2(thickness, 1+extentFront), new Vector2(thickness, extentBack), comp.TopLeftPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 0, 0-extentFront, 1-extentBack, bleedLight);
                                }
                                else
                                {
                                    //fence tl
                                    addLineGeom(new Vector2(0, 1), new Vector2(0, 0), comp.TopLeftPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 4, 0, 1, bleedLight);
                                }
                            }
                            if ((comp.Segments & WallSegments.TopRight) > 0)
                            {
                                if (comp.TopRightThick)
                                {
                                    float extentBack = 0;
                                    float extentFront = 0;
                                    float bleedLight2 = (level == 1 || blueprint.GetFloor(x, (short)(y-1), level).Pattern != 0 || HasRoof(x, y-1, level)) ? 0 : 1;
                                    if (x > 0 && !blueprint.GetWall((short)(x - 1), y, level).TopRightThick)
                                    {
                                        extentBack = -thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(extentBack+0.005f, -thickness), new Vector2(extentBack + 0.005f, thickness), comp.TopRightPattern, 1, 5, 0, thickness*2, bleedLight);
                                    }
                                    if (x < blueprint.Width - 1 && !blueprint.GetWall((short)(x + 1), y, level).TopRightThick)
                                    {
                                        extentFront = thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(1 + extentFront - 0.005f, thickness), new Vector2(1 + extentFront - 0.005f, -thickness), comp.TopRightPattern, 1, 5, 0, thickness * 2, bleedLight);
                                    }
                                    if (y > 0)
                                        addLineGeom(new Vector2(1 + extentFront, -thickness), new Vector2(extentBack, -thickness), blueprint.GetWall(x, (short)(y - 1), level).BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, extentFront, -(1 - extentBack), bleedLight2);
                                    addLineGeom(new Vector2(extentBack, thickness), new Vector2(1+extentFront, thickness), comp.TopRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 1, extentBack, 1+extentFront, bleedLight);
                                }
                                else
                                {
                                    //fence tr
                                    addLineGeom(new Vector2(0, 0), new Vector2(1, 0), comp.TopRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bleedLight);
                                }
                            }

                            if ((comp.Segments & WallSegments.BottomLeft) > 0 && y < blueprint.Height)
                            {
                                //fence bl
                                var comp2 = blueprint.GetWall(x, (short)(y + 1), level);
                                if (!comp2.TopRightThick)
                                    addLineGeom(new Vector2(1, 1), new Vector2(0, 1), comp.BottomLeftPattern, (comp2.ObjSetTRStyle == 0) ? comp2.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bleedLight);
                            }
                            if ((comp.Segments & WallSegments.BottomRight) > 0 && x < blueprint.Width)
                            {
                                //fence br

                                var comp2 = blueprint.GetWall((short)(x + 1), y, level);
                                if (!comp2.TopLeftThick)
                                    addLineGeom(new Vector2(1, 0), new Vector2(1, 1), comp.BottomRightPattern, (comp2.ObjSetTLStyle == 0) ? comp2.TopLeftStyle : comp2.ObjSetTLStyle, 4, 0, 1, bleedLight);
                            }

                            if ((comp.Segments & WallSegments.HorizontalDiag) > 0)
                            {
                                var bl2 = (level == 1 || comp.TopLeftStyle != 0) ? 0 : 1;
                                var bl1 = (level == 1 || comp.TopLeftPattern != 0) ? 0 : 1;
                                if (comp.TopRightStyle == 1 || comp.TopRightStyle == 255)
                                {
                                    addLineGeom(new Vector2(thickDiag, 1+thickDiag), new Vector2(1+thickDiag, thickDiag), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 2, 0, 1, bl1);
                                    addLineGeom(new Vector2(1-thickDiag, -thickDiag), new Vector2(-thickDiag, 1 - thickDiag), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, 0, 1, bl2);

                                    //caps
                                    addLineGeom(new Vector2(-thickDiag, 1 - thickDiag), new Vector2(thickDiag, 1 + thickDiag), comp.BottomRightPattern, 1, 5, 0, thickDiag*2, Math.Min(bl1, bl2));
                                    addLineGeom(new Vector2(1 + thickDiag, thickDiag), new Vector2(1 - thickDiag, -thickDiag), comp.BottomLeftPattern, 1, 5, 0, thickDiag * 2, Math.Min(bl1, bl2));
                                }
                                else
                                {
                                    addLineGeom(new Vector2(0, 1), new Vector2(1, 0), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bl1);
                                    addLineGeom(new Vector2(1, 0), new Vector2(0, 1), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bl2);
                                }
                            }
                            if ((comp.Segments & WallSegments.VerticalDiag) > 0)
                            {
                                var bl1 = (level == 1 || comp.TopLeftStyle != 0) ? 0 : 1;
                                var bl2 = (level == 1 || comp.TopLeftPattern != 0) ? 0 : 1;
                                if (comp.TopRightStyle == 1 || comp.TopRightStyle == 255)
                                {
                                    addLineGeom(new Vector2(-thickDiag, thickDiag), new Vector2(1- thickDiag, 1+thickDiag), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 3, 0, 1, bl1);
                                    addLineGeom(new Vector2(1+ thickDiag, 1- thickDiag), new Vector2(0 + thickDiag, -thickDiag), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, 0, 1, bl2);

                                    //caps 
                                    addLineGeom(new Vector2(0 + thickDiag, -thickDiag), new Vector2(-thickDiag, thickDiag), comp.BottomLeftPattern, 1, 5, 0, thickDiag*2, Math.Min(bl1, bl2));
                                    addLineGeom(new Vector2(1 - thickDiag, 1 + thickDiag), new Vector2(1 + thickDiag, 1 - thickDiag), comp.BottomRightPattern, 1, 5, 0, thickDiag * 2, Math.Min(bl1, bl2));

                                }
                                else
                                {
                                    addLineGeom(new Vector2(0, 0), new Vector2(1, 1), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bl1);
                                    addLineGeom(new Vector2(1, 1), new Vector2(0, 0), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1, bl2);
                                }

                            }
                        }
                    }
                }
                foreach (var g in grp.Values) g.Complete(device);
            }
        }

        public void Draw(GraphicsDevice gd, WorldState state)
        {
            var effect = WorldContent.RCObject;

            effect.CurrentTechnique = effect.Techniques["WallDraw"];

            var lastSideMask = false;

            var xz = ((WorldStateRC)state).GetWallOffset() * 0.7f;

            gd.BlendState = BlendState.Opaque;
            if (!gd.RasterizerState.ScissorTestEnable) gd.RasterizerState = RasterizerState.CullCounterClockwise;
            var baseWorld = Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateScale(3f, -3f, 3f);
            effect.Parameters["World"].SetValue(baseWorld);
            effect.Parameters["SideMask"].SetValue(0f);
            effect.Parameters["Level"].SetValue((float)(state.Level - 0.999f));
            //effect.Parameters["CutawayTex"].SetValue(Cutaway);
            //effect.Parameters["CurrentLevel"].SetValue(state.Level - 1);
            if (GroupsByTexture.Count < state.Level) return;

            for (int i = 0; i < state.Level; i++)
            {
                var grp = GroupsByTexture[i];
                foreach (var g in grp.Values)
                {
                    if (g.PrimCount == 0) continue;
                    if (effect.Parameters["AnisoTex"] != null)
                        effect.Parameters["AnisoTex"].SetValue(g.Pixel);
                    else
                        effect.Parameters["MeshTex"].SetValue(g.Pixel);
                    effect.Parameters["MaskTex"].SetValue(g.Mask);

                    if (lastSideMask != g.UseOffset)
                    {
                        effect.Parameters["SideMask"].SetValue(g.UseOffset ? 1f : 0f);
                        effect.Parameters["World"].SetValue((g.UseOffset) ? (Matrix.CreateTranslation(new Vector3(xz, 0)) * baseWorld) : baseWorld);
                        lastSideMask = g.UseOffset;
                    }
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        gd.SetVertexBuffer(g.GVerts);
                        gd.Indices = g.GIndices;
                        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, g.PrimCount);
                    }
                }
            }
            if (!gd.RasterizerState.ScissorTestEnable) gd.RasterizerState = RasterizerState.CullNone;
        }

        public BlendState MaxBlendRed = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Alpha
        };

        public BlendState MaxBlendGreen = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Green | ColorWriteChannels.Alpha
        };

        public void DrawLMap(GraphicsDevice gd, LightData light, Matrix projection, Matrix lightTransform)
        {
            var effect = WorldContent.RCObject;

            effect.CurrentTechnique = effect.Techniques["WallLMap"];

            var lastSideMask = false;

            //gd.DepthStencilState = DepthStencilState.None;
            //gd.RasterizerState = RasterizerState.CullNone;
            //var baseWorld = Matrix.CreateRotationX((float)Math.PI / 2) * Matrix.CreateScale(3f, -3f, 3f);

            //mat.M31 = 0; mat.M32 = 0; mat.M33 = 0; mat.M34 = 0f; //z is unimportant, so it is zero.

            effect.Parameters["World"].SetValue(Matrix.CreateTranslation(0, 0, -light.Level+(0.07f / light.FalloffMultiplier)) * lightTransform);
            effect.Parameters["Level"].SetValue((float)(light.Level));
            effect.Parameters["SideMask"].SetValue(0f);
            effect.Parameters["ViewProjection"].SetValue(projection);

            //dont care about base texture or offset here.
            for (int i = light.Level; i < GroupsByTexture.Count; i++)
            {
                var grp = GroupsByTexture[i];
                foreach (var g in grp.Values)
                {
                    if (g.PrimCount == 0) continue;
                    effect.Parameters["MaskTex"].SetValue(g.Mask);

                    if (lastSideMask != g.UseOffset)
                    {
                        gd.BlendState = (g.UseOffset) ? MaxBlendRed : MaxBlendGreen;
                        effect.Parameters["SideMask"].SetValue(g.UseOffset ? 1f : 0f);
                        lastSideMask = g.UseOffset;
                    }
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        gd.SetVertexBuffer(g.GVerts);
                        gd.Indices = g.GIndices;
                        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, g.PrimCount);
                    }
                }
            }
            //gd.DepthStencilState = DepthStencilState.None;
            //gd.BlendState = BlendState.AlphaBlend;
        }

        public WallGroupRC Fetch(Texture2D pixel, Texture2D mask, Dictionary<Tuple<Texture2D, Texture2D>, WallGroupRC> grp)
        {
            WallGroupRC result;
            var t = new Tuple<Texture2D, Texture2D>(pixel, mask);
            if (!grp.TryGetValue(t, out result))
            {
                result = new WallGroupRC() { Pixel = pixel, Mask = mask };
                grp[t] = result;
            }
            return result;
        }

        public void Dispose()
        {
            foreach (var grp in GroupsByTexture)
            {
                foreach (var g in grp.Values)
                {
                    g.Dispose();
                }
            }
            GroupsByTexture.Clear();
        }
    }

    public class WallGroupRC : IDisposable
    {
        public Texture2D Pixel;
        public Texture2D Mask;
        public bool UseOffset = true;

        public List<WallVertexRC> Verts = new List<WallVertexRC>();
        public List<int> Indices = new List<int>();

        public VertexBuffer GVerts;
        public IndexBuffer GIndices;
        public int PrimCount;

        public void Complete(GraphicsDevice gd)
        {
            GVerts = new VertexBuffer(gd, typeof(WallVertexRC), Verts.Count, BufferUsage.None);
            GVerts.SetData(Verts.ToArray());
            GIndices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Indices.Count, BufferUsage.None);
            GIndices.SetData(Indices.ToArray());

            PrimCount = Indices.Count / 3;
            Verts = null;
            Indices = null;
        }

        public void Dispose()
        {
            GVerts.Dispose();
            GIndices.Dispose();
        }
    }
}
