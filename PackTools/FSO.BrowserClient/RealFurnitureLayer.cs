using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Common.Rendering;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Draws real DGRP furniture sprites in the browser lot: engine data (per
    /// zoom/rotation image selection, sprite/object offsets, engine textures via
    /// the 2D batch cache) through the engine's immediate sprite batch.
    ///
    /// Sprites are built and drawn here rather than through
    /// WorldEntities.Draw/ObjectComponent.Draw: with the current KNIF-built
    /// 2DWorldBatch effect, the DGRPRenderer-owned draw path produces no
    /// fragments under WebGL (same data drawn through this path does), a
    /// mystery earmarked for the WebGL-profile FX rebuild. Components are still
    /// placed into the Blueprint (Place) so picking/VM stages can use them.
    /// </summary>
    public class RealFurnitureLayer
    {
        class Entry
        {
            public string Id;
            public FSO.Content.GameObject Obj;
            public short X;
            public short Y;
            public sbyte Level;
            public int Dir;
            public List<_2DStandaloneSprite> Sprites = new List<_2DStandaloneSprite>();
        }

        readonly List<Entry> entries = new List<Entry>();
        WorldZoom builtZoom;
        WorldRotation builtRotation;
        bool built;
        public int Count => entries.Count;

        public static bool V2Diag;
        /// <summary>?v2diag=2 — draw every sprite with a solid magenta texture to
        /// locate where (or whether) these draws land.</summary>
        public static bool V2AllMagenta;
        Texture2D diagTex;
        bool diagLogged;

        /// <summary>Fullbright: ObjectComponent.Room passes >Rooms.Count through, and
        /// the sprite shader treats 65535 as luminous — matches the forced all-white
        /// outdoor lighting.</summary>
        const ushort RoomFullbright = 65535;

        /// <summary>Register real ObjectComponents with the blueprint (picking/VM use
        /// them later) and remember placement data for sprite building.</summary>
        public int Place(World world, Blueprint bp, PackObjectLoader packs, JObject furnish)
        {
            short nextId = 100;
            int placed = 0, skipped = 0;

            foreach (var f in (JArray)furnish["furniture"])
            {
                var id = (string)f["id"];
                var pack = packs.Get(id);
                if (pack == null) { skipped++; continue; }

                var entry = new Entry
                {
                    Id = id,
                    Obj = pack.Object,
                    X = (short)(int)f["x"],
                    Y = (short)(int)f["y"],
                    Level = (sbyte)((int?)f["level"] ?? 1),
                    Dir = (int?)f["dir"] ?? 0,
                };
                entries.Add(entry);

                var comp = world.MakeObjectComponent(pack.Object);
                bp.AddObject(comp);
                var pos = LotTilePos.FromBigTile(entry.X, entry.Y, entry.Level);
                bp.ChangeObjectLocation(comp, pos);
                comp.Position = new Vector3(pos.x / 16f - 0.5f, pos.y / 16f - 0.5f, (pos.Level - 1) * 2.95f);
                comp.Direction = DirFromXml(entry.Dir);
                comp.Room = RoomFullbright;
                comp.ObjectID = nextId++;
                placed++;
            }

            Console.WriteLine($"furniture real: {placed} components placed, {skipped} without pack objects");
            return placed;
        }

        /// <summary>Mirror of DGRPRenderer.ValidateSprite for our entries: pick the
        /// DGRP image for the current zoom/rotation and lay out its sprites.</summary>
        void BuildSprites(WorldState state)
        {
            var space = state.WorldSpace;
            foreach (var e in entries)
            {
                e.Sprites.Clear();
                var objd = e.Obj.OBJ;
                var dgrp = e.Obj.Resource.Get<DGRP>(objd.BaseGraphicID);
                var image = dgrp?.GetImage((uint)DirFromXml(e.Dir), (uint)state.Zoom, (uint)state.Rotation);
                if (image == null) continue;

                var radDir = RadFromXml(e.Dir);
                foreach (var dgrpSprite in image.Sprites)
                {
                    if (dgrpSprite == null) continue;
                    var texture = state._2D.GetWorldTexture(dgrpSprite);
                    if (texture.Pixel == null) continue;

                    var pt = ((TextureInfo)texture.Pixel.Tag).Size;
                    var sprite = new _2DStandaloneSprite
                    {
                        Pixel = texture.Pixel,
                        // NO_DEPTH: painter's order until the FX rebuild restores the
                        // software depth pipeline.
                        RenderMode = _2DBatchRenderMode.NO_DEPTH,
                        SrcRect = new Rectangle(0, 0, pt.X, pt.Y),
                        DestRect = new Rectangle(0, 0, pt.X, pt.Y),
                        FlipHorizontally = dgrpSprite.Flip,
                        Room = RoomFullbright,
                        Floor = e.Level,
                        ObjectID = 1,
                    };

                    var pxX = (space.CadgeWidth / 2.0f) + dgrpSprite.SpriteOffset.X;
                    var pxY = (space.CadgeBaseLine - sprite.DestRect.Height) + dgrpSprite.SpriteOffset.Y;
                    var centerRelative = dgrpSprite.ObjectOffset * new Vector3(1f / 16f, 1f / 16f, 1f / 5f);
                    centerRelative = Vector3.Transform(centerRelative, Matrix.CreateRotationZ(radDir));
                    var pxOff = space.GetScreenFromTile(centerRelative);
                    sprite.DestRect.X = (int)(pxX + pxOff.X);
                    sprite.DestRect.Y = (int)(pxY + pxOff.Y);
                    sprite.WorldPosition = centerRelative * 3f;
                    e.Sprites.Add(sprite);
                }
            }
            builtZoom = state.Zoom;
            builtRotation = state.Rotation;
            built = true;
            Console.WriteLine($"furniture real: sprites built for zoom={state.Zoom} rot={state.Rotation} " +
                $"({entries.Sum(e2 => e2.Sprites.Count)} sprites)");
        }

        /// <summary>Draw after the world pass. Positions/vertex buffers refresh per
        /// frame (camera scroll changes AbsoluteDestRect); painter-sorted by tile.</summary>
        public void Draw(GraphicsDevice gd, WorldState state)
        {
            if (entries.Count == 0) return;
            if (!built || state.Zoom != builtZoom || state.Rotation != builtRotation) BuildSprites(state);

            var _2d = state._2D;

            // Upload ALL vertex buffers BEFORE the prepare/draw bracket: a
            // VertexBuffer.SetData between PrepareImmediate and DrawImmediate desyncs
            // KNI BlazorGL's cached vertex binding and the draw emits no fragments
            // (the one structural difference between every failing path and the
            // probe that rendered).
            var ordered = entries.OrderBy(en => en.X + en.Y).ToList();
            foreach (var e in ordered)
            {
                var tilePos = new Vector3(e.X - 0.5f, e.Y - 0.5f, (e.Level - 1) * 2.95f);
                var basePx = state.WorldSpace.GetScreenFromTile(tilePos);
                foreach (var sprite in e.Sprites)
                {
                    sprite.AbsoluteDestRect = sprite.DestRect;
                    sprite.AbsoluteDestRect.Offset((int)basePx.X, (int)basePx.Y);
                    sprite.AbsoluteWorldPosition = sprite.WorldPosition + WorldSpace.GetWorldFromTile(tilePos);
                    sprite.PrepareVertices(gd);
                }
            }

            var restoreDS = gd.DepthStencilState;
            gd.DepthStencilState = DepthStencilState.None;
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            // Force a technique SWITCH: every draw that has ever produced fragments on
            // KNI BlazorGL followed a technique flip on this effect (uniform flush
            // appears to be keyed on program change).
            _2d.PrepareImmediate(FSO.LotView.Effects.WorldBatchTechniques.drawSimple);
            _2d.PrepareImmediate(FSO.LotView.Effects.WorldBatchTechniques.drawZSprite);

            if (V2AllMagenta && diagTex == null)
            {
                diagTex = new Texture2D(gd, 64, 64);
                var dp = new Color[64 * 64];
                for (int i = 0; i < dp.Length; i++) dp[i] = Color.Magenta;
                diagTex.SetData(dp);
            }

            bool firstSprite = true;
            foreach (var e in ordered)
            {
                foreach (var sprite in e.Sprites)
                {
                    if (V2AllMagenta)
                    {
                        var sp = sprite.Pixel;
                        sprite.Pixel = diagTex;
                        _2d.DrawImmediate(sprite);
                        sprite.Pixel = sp;
                        continue;
                    }
                    _2d.DrawImmediate(sprite);

                    if (V2Diag && firstSprite)
                    {
                        firstSprite = false;
                        if (diagTex == null)
                        {
                            diagTex = new Texture2D(gd, 64, 64);
                            var dp = new Color[64 * 64];
                            for (int i = 0; i < dp.Length; i++) dp[i] = Color.Magenta;
                            diagTex.SetData(dp);
                        }
                        // A: magenta texture at the REAL sprite's rect (+y-80 so both visible)
                        var savedPix = sprite.Pixel;
                        var savedAbs = sprite.AbsoluteDestRect;
                        sprite.Pixel = diagTex;
                        sprite.AbsoluteDestRect.Offset(0, -80);
                        sprite.PrepareVertices(gd);
                        _2d.DrawImmediate(sprite);
                        // B: REAL texture at the screen-center rect
                        sprite.Pixel = savedPix;
                        sprite.AbsoluteDestRect = new Rectangle(
                            state.WorldRectangle.Center.X - 32, state.WorldRectangle.Center.Y + 120, 64, 64);
                        sprite.PrepareVertices(gd);
                        _2d.DrawImmediate(sprite);
                        sprite.AbsoluteDestRect = savedAbs;
                        if (!diagLogged)
                        {
                            diagLogged = true;
                            Console.WriteLine($"v2diag A: magenta at {savedAbs} B: realtex {savedPix?.Width}x{savedPix?.Height} at center+120");
                        }
                    }
                }
            }

            _2d.EndImmediate();
            gd.DepthStencilState = restoreDS;
        }

        /// <summary>Blueprint-XML dir convention (XmlHouseDataObject): 0=N 2=E 4=S 6=W.</summary>
        static Direction DirFromXml(int dir)
        {
            switch (dir)
            {
                case 2: return Direction.EAST;
                case 4: return Direction.SOUTH;
                case 6: return Direction.WEST;
                default: return Direction.NORTH;
            }
        }

        static float RadFromXml(int dir)
        {
            switch (dir)
            {
                case 2: return (float)Math.PI / 2;
                case 4: return (float)Math.PI;
                case 6: return (float)(Math.PI * 3) / 2;
                default: return 0;
            }
        }
    }
}
