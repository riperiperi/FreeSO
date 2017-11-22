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
            var xz = world.GetWallOffset()*10;
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


                        Action<Vector2, Vector2, ushort, ushort, int, float, float> addLineGeom = (Vector2 from, Vector2 to, ushort pattern, ushort style, int topMode, float starttc, float endtc) => {
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
                            g.Verts.Add(new WallVertexRC(p1, col, new Vector2(starttc, l)));
                            g.Verts.Add(new WallVertexRC(p2, col, new Vector2(endtc, l)));
                            g.Verts.Add(new WallVertexRC(p1+ wallHeight*h1, col, new Vector2(starttc, h1 + l)));
                            g.Verts.Add(new WallVertexRC(p2+ wallHeight*h2, col, new Vector2(endtc, h2 + l)));

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
                                g2.Verts.Add(new WallVertexRC(p1 + wallHeight*h1, wallTop, new Vector2(0, h1 + l)));
                                g2.Verts.Add(new WallVertexRC(p2 + wallHeight*h2, wallTop, new Vector2(0, h2 + l)));
                                g2.Verts.Add(new WallVertexRC(p1 + wallHeight*h1 + toBack, wallTop, new Vector2(0, h1 + l)));
                                g2.Verts.Add(new WallVertexRC(p2 + wallHeight*h2 + toBack, wallTop, new Vector2(0, h2 + l)));

                                g2.Indices.Add(baseI); g2.Indices.Add(baseI + 2); g2.Indices.Add(baseI + 1);
                                g2.Indices.Add(baseI + 2); g2.Indices.Add(baseI + 3); g2.Indices.Add(baseI + 1);
                            }
                        };

                        var comp = blueprint.GetWall(x, y, level);
                        if (comp.Segments != 0)
                        {
                            if ((comp.Segments & WallSegments.TopLeft) > 0)
                            {
                                if (comp.TopLeftThick)
                                {
                                    float extentBack = 0;
                                    float extentFront = 0;
                                    if (y > 0 && !blueprint.GetWall(x, (short)(y - 1), level).TopLeftThick)
                                    {
                                        extentBack = -thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(thickness, extentBack + 0.005f), new Vector2(-thickness, extentBack + 0.005f), comp.TopLeftPattern, 1, 5, 0, thickness * 2);
                                    }
                                    if (y < blueprint.Height - 1 && !blueprint.GetWall(x, (short)(y + 1), level).TopLeftThick)
                                    {
                                        extentFront = thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(-thickness, 1 + extentFront - 0.005f), new Vector2(thickness, 1 + extentFront - 0.005f), comp.TopLeftPattern, 1, 5, 0, thickness * 2);
                                    }
                                    if (x > 0)
                                        addLineGeom(new Vector2(-thickness, extentBack), new Vector2(-thickness, 1 + extentFront), blueprint.GetWall((short)(x - 1), y, level).BottomRightPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 5, -(0 + extentBack), -(1 + extentFront));
                                    addLineGeom(new Vector2(thickness, 1+extentFront), new Vector2(thickness, extentBack), comp.TopLeftPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 0, 0-extentFront, 1-extentBack);
                                }
                                else
                                {
                                    //fence tl
                                    addLineGeom(new Vector2(0, 1), new Vector2(0, 0), comp.TopLeftPattern, (comp.ObjSetTLStyle == 0) ? comp.TopLeftStyle : comp.ObjSetTLStyle, 4, 0, 1);
                                }
                            }
                            if ((comp.Segments & WallSegments.TopRight) > 0)
                            {
                                if (comp.TopRightThick)
                                {
                                    float extentBack = 0;
                                    float extentFront = 0;
                                    if (x > 0 && !blueprint.GetWall((short)(x - 1), y, level).TopRightThick)
                                    {
                                        extentBack = -thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(extentBack+0.005f, -thickness), new Vector2(extentBack + 0.005f, thickness), comp.TopRightPattern, 1, 5, 0, thickness*2);
                                    }
                                    if (x < blueprint.Width - 1 && !blueprint.GetWall((short)(x + 1), y, level).TopRightThick)
                                    {
                                        extentFront = thickness;
                                        //cap this end
                                        addLineGeom(new Vector2(1 + extentFront - 0.005f, thickness), new Vector2(1 + extentFront - 0.005f, -thickness), comp.TopRightPattern, 1, 5, 0, thickness * 2);
                                    }
                                    if (y > 0)
                                        addLineGeom(new Vector2(1 + extentFront, -thickness), new Vector2(extentBack, -thickness), blueprint.GetWall(x, (short)(y - 1), level).BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, extentFront, -(1 - extentBack));
                                    addLineGeom(new Vector2(extentBack, thickness), new Vector2(1+extentFront, thickness), comp.TopRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 1, extentBack, 1+extentFront);
                                }
                                else
                                {
                                    //fence tr
                                    addLineGeom(new Vector2(0, 0), new Vector2(1, 0), comp.TopRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
                                }
                            }

                            if ((comp.Segments & WallSegments.BottomLeft) > 0 && y < blueprint.Height)
                            {
                                //fence bl
                                var comp2 = blueprint.GetWall(x, (short)(y + 1), level);
                                if (!comp2.TopRightThick)
                                    addLineGeom(new Vector2(1, 1), new Vector2(0, 1), comp.BottomLeftPattern, (comp2.ObjSetTRStyle == 0) ? comp2.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
                            }
                            if ((comp.Segments & WallSegments.BottomRight) > 0 && x < blueprint.Width)
                            {
                                //fence br

                                var comp2 = blueprint.GetWall((short)(x + 1), y, level);
                                if (!comp2.TopLeftThick)
                                    addLineGeom(new Vector2(1, 0), new Vector2(1, 1), comp.BottomRightPattern, (comp2.ObjSetTLStyle == 0) ? comp2.TopLeftStyle : comp2.ObjSetTLStyle, 4, 0, 1);
                            }

                            if ((comp.Segments & WallSegments.HorizontalDiag) > 0)
                            {
                                if (comp.TopRightStyle == 1 || comp.TopRightStyle == 255)
                                {
                                    addLineGeom(new Vector2(thickDiag, 1+thickDiag), new Vector2(1+thickDiag, thickDiag), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 2, 0, 1);
                                    addLineGeom(new Vector2(1-thickDiag, -thickDiag), new Vector2(-thickDiag, 1 - thickDiag), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, 0, 1);

                                    //caps
                                    addLineGeom(new Vector2(-thickDiag, 1 - thickDiag), new Vector2(thickDiag, 1 + thickDiag), comp.BottomRightPattern, 1, 5, 0, thickDiag*2);
                                    addLineGeom(new Vector2(1 + thickDiag, thickDiag), new Vector2(1 - thickDiag, -thickDiag), comp.BottomLeftPattern, 1, 5, 0, thickDiag * 2);
                                }
                                else
                                {
                                    addLineGeom(new Vector2(0, 1), new Vector2(1, 0), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
                                    addLineGeom(new Vector2(1, 0), new Vector2(0, 1), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
                                }
                            }
                            if ((comp.Segments & WallSegments.VerticalDiag) > 0)
                            {
                                if (comp.TopRightStyle == 1 || comp.TopRightStyle == 255)
                                {
                                    addLineGeom(new Vector2(-thickDiag, thickDiag), new Vector2(1- thickDiag, 1+thickDiag), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 3, 0, 1);
                                    addLineGeom(new Vector2(1+ thickDiag, 1- thickDiag), new Vector2(0 + thickDiag, -thickDiag), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 5, 0, 1);

                                    //caps 
                                    addLineGeom(new Vector2(0 + thickDiag, -thickDiag), new Vector2(-thickDiag, thickDiag), comp.BottomLeftPattern, 1, 5, 0, thickDiag*2);
                                    addLineGeom(new Vector2(1 - thickDiag, 1 + thickDiag), new Vector2(1 + thickDiag, 1 - thickDiag), comp.BottomRightPattern, 1, 5, 0, thickDiag * 2);

                                }
                                else
                                {
                                    addLineGeom(new Vector2(0, 0), new Vector2(1, 1), comp.BottomLeftPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
                                    addLineGeom(new Vector2(1, 1), new Vector2(0, 0), comp.BottomRightPattern, (comp.ObjSetTRStyle == 0) ? comp.TopRightStyle : comp.ObjSetTRStyle, 4, 0, 1);
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

            var xz = ((WorldStateRC)state).GetWallOffset();

            gd.BlendState = BlendState.Opaque;
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
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
            gd.RasterizerState = RasterizerState.CullNone;
        }

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
