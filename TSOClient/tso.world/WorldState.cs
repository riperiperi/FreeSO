/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Components;
using FSO.LotView.LMap;
using FSO.Common.Utils;
using FSO.Vitaboy;

namespace FSO.LotView
{
    /// <summary>
    /// Holds state information retaining to world.
    /// </summary>
    public class WorldState 
    {
        private World World;
        public GraphicsDevice Device;

        /// <summary>
        /// Creates a new WorldState instance.
        /// </summary>
        /// <param name="device">A GraphicsDevice used for rendering.</param>
        /// <param name="worldPxWidth">Width of world in pixels.</param>
        /// <param name="worldPxHeight">Height of world in pixels.</param>
        /// <param name="world">A World instance.</param>
        public WorldState(GraphicsDevice device, float worldPxWidth, float worldPxHeight, World world)
        {
            this.Device = device;
            this.World = world;
            this.WorldCamera = new WorldCamera(device);
            WorldCamera.ViewDimensions = new Vector2(worldPxWidth, worldPxHeight);

            WorldSpace = new WorldSpace(worldPxWidth, worldPxHeight, this);
            Zoom = WorldZoom.Near;
            Rotation = WorldRotation.TopLeft;
            Level = 1;
        }

        public void SetDimensions(Vector2 dim)
        {
            WorldCamera.ViewDimensions = dim;
            WorldSpace.SetDimensions(dim);
        }

        protected WorldCamera WorldCamera;

        /// <summary>
        /// Gets the camera used by this WorldState instance.
        /// </summary>
        public ICamera Camera 
        {
            get { return WorldCamera; }
        }

        public bool TempDraw; //set for OBJID mode and thumbs
        public WorldSpace WorldSpace;
        public _2DWorldBatch _2D;
        public _3DWorldBatch _3D;
        public LMapBatch Light;
        public Texture2D AmbientLight;
        public Color OutsideColor; //temporary to give this to terrain component. in future it will use ambient light texture
        public bool DynamicCutaway;

        public bool ThisFrameImmediate;

        public AvatarComponent ScrollAnchor;

        private int _WorldSize;

        /// <summary>
        /// Gets or sets size of world.
        /// </summary>
        public int WorldSize
        {
            get {
                return _WorldSize;
            }
            set {
                _WorldSize = value;
                WorldCamera.WorldSize = value;
                InvalidateWorldSize();
            }
        }

        public bool DrawRoofs;

        public int SilentBuildMode
        {
            get { return _BuildMode; }
            set { _BuildMode = value; }
        }

        private int _BuildMode;
        public int BuildMode
        {
            get
            {
                return _BuildMode;
            }
            set
            {
                if (_BuildMode != value) World.InvalidateFloor();
                _BuildMode = value;
            }
        }

        /// <summary>
        /// What level is being displayed
        /// </summary>
        private sbyte _Level;
        public sbyte Level
        {
            get { return _Level; }
            set { _Level = value; World.InvalidateFloor(); }
        }

        /// <summary>
        /// Set level without invalidating.
        /// </summary>
        public sbyte SilentLevel
        {
            get { return _Level; }
            set { _Level = value; }
        }

        private float _PreciseZoom = 1f;
        public float PreciseZoom
        {
            get { return _PreciseZoom; }
            set { _PreciseZoom = value; InvalidatePreciseZoom(); }
        }

        public float SilentPreciseZoom
        {
            get { return _PreciseZoom; }
            set { _PreciseZoom = value; InvalidateCamera(); }
        }

        /// <summary>
        /// What zoom level is being displayed
        /// </summary>
        private WorldZoom _Zoom;
        public WorldZoom Zoom {
            get{ return _Zoom; }
            set{ var old = _Zoom; _Zoom = value; if (value != old) InvalidateZoom(); }
        }

        /// <summary>
        /// Set zoom without invalidating.
        /// </summary>
        public WorldZoom SilentZoom
        {
            get { return _Zoom; }
            set { _Zoom = value;}
        }

        /// <summary>
        /// What rotation is being displayed
        /// </summary>
        private WorldRotation _Rotation;
        public WorldRotation Rotation {
            get { return _Rotation; }
            set { _Rotation = value; InvalidateRotation();  }
        }

        /// <summary>
        /// Set rotation without invalidating.
        /// </summary>
        public WorldRotation SilentRotation
        {
            get { return _Rotation; }
            set { _Rotation = value; }
        }

        /// <summary>
        /// Draw entities even if they are out of world. (for thumbnails)
        /// </summary>
        public bool DrawOOB;

        private Vector2 _CenterTile = Vector2.Zero;
        public Vector2 CenterTile
        {
            get { return _CenterTile; }
            set { _CenterTile = value; InvalidateScroll(); }
        }

        protected void InvalidateZoom()
        {
            WorldSpace.Invalidate();
            InvalidateCamera();
            World.InvalidateZoom();
        }

        protected void InvalidatePreciseZoom()
        {
            InvalidateCamera();
            World.InvalidatePreciseZoom();
        }

        protected void InvalidateRotation()
        {
            WorldSpace.Invalidate();
            InvalidateCamera();
            World.InvalidateRotation();
        }

        protected void InvalidateScroll()
        {
            WorldSpace.Invalidate();
            InvalidateCamera();
            World.InvalidateScroll();
        }

        protected void InvalidateWorldSize()
        {
            var edge = _WorldSize + 1.0f;
            var radius = WorldSpace.WorldUnitsPerTile * (edge / 2.0f);
            var opposite = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * radius;

            Camera.Position = new Vector3(radius * 2, opposite, radius * 2);
            Camera.Target = new Vector3(radius, 0.0f, radius);

            //Center point of the center most tile
            //CenterTile = new Vector2((_WorldSize / 2.0f), (_WorldSize / 2.0f));
            InvalidateCamera();
        }


        public void InvalidateCamera()
        {
            var ctr = WorldSpace.GetScreenFromTile(CenterTile);
            ctr.X = (float)Math.Round(ctr.X);
            ctr.Y = (float)Math.Round(ctr.Y);
            var test = new Vector2(-0.5f, 0);   
            test *= 1 << (3 - (int)Zoom);
            var back = WorldSpace.GetTileFromScreen(ctr + test);
            WorldCamera.CenterTile = new Vector3(back, 0);
            WorldCamera.Zoom = Zoom;
            WorldCamera.Rotation = Rotation;
            WorldCamera.PreciseZoom = PreciseZoom;
        }

        public void PrepareLighting()
        {
            var adv = (Light?.LightMap) ?? TextureGenerator.GetDefaultAdv(Device);
            var amb = AmbientLight ?? TextureGenerator.GetPxWhite(Device);

            WorldContent._2DWorldBatchEffect.Parameters["advancedLight"].SetValue(adv);
            WorldContent.GrassEffect.Parameters["advancedLight"].SetValue(adv);
            WorldContent._2DWorldBatchEffect.Parameters["ambientLight"].SetValue(amb);
            Avatar.Effect.Parameters["advancedLight"].SetValue(adv);

            var frontDir = WorldCamera.FrontDirection();
            var lightOffset = new Vector2(frontDir.X / (6 * 75), frontDir.Z / (6 * 75));
            if (Light != null) lightOffset *= Light.InvMapLayout;
            WorldContent._2DWorldBatchEffect.Parameters["LightOffset"].SetValue(lightOffset);
            WorldContent.GrassEffect.Parameters["LightOffset"].SetValue(lightOffset);
            Avatar.Effect.Parameters["LightOffset"].SetValue(lightOffset);

            WorldContent._2DWorldBatchEffect.Parameters["MaxFloor"].SetValue((float)Level-1);
        }

        public void ClearLighting(bool indoors)
        {
            var adv = TextureGenerator.GetDefaultAdv(Device);
            var amb = TextureGenerator.GetPxWhite(Device);
            if (indoors) adv = amb;

            WorldContent._2DWorldBatchEffect.Parameters["advancedLight"].SetValue(adv);
            WorldContent.GrassEffect.Parameters["advancedLight"].SetValue(adv);
            WorldContent._2DWorldBatchEffect.Parameters["ambientLight"].SetValue(amb);
            Avatar.Effect.Parameters["advancedLight"].SetValue(adv);
        }
    }

    /// <summary>
    /// Holds information about the world space and drawing numbers.
    /// </summary>
    public class WorldSpace
    {
        public const float WorldUnitsPerTile = 3f;

        /// <summary>
        /// How big is a tile in 2D space
        /// </summary>
        public float TilePxHeight = 0.0f;
        public float TilePxWidth = 0.0f;

        public float TilePxHeightHalf = 0.0f;
        public float TilePxWidthHalf = 0.0f;

        public float CadgeWidth;
        public float CadgeHeight;
        public float CadgeBaseLine;
        public float TerrainHeight;

        public float TileSin60;
        public float TileSin30;

        /// <summary>
        /// How big is the world view area in pixels
        /// </summary>
        public float WorldPxWidth;
        public float WorldPxHeight;
        public float OneUnitDistance;

        private WorldState State;

        /// <summary>
        /// Creates a new WorldSpace instance.
        /// </summary>
        /// <param name="worldPxWidth">Width of world in pixels.</param>
        /// <param name="worldPxHeight">Height of world in pixels.</param>
        /// <param name="state">WorldState instance.</param>
        public WorldSpace(float worldPxWidth, float worldPxHeight, WorldState state)
        {
            this.State = state;
            this.WorldPxWidth = worldPxWidth;
            this.WorldPxHeight = worldPxHeight;
        }

        public void SetDimensions(Vector2 dim)
        {
            WorldPxWidth = dim.X;
            WorldPxHeight = dim.Y;
        }

        public Vector2 GetPointScreenOffset()
        {
            var centerPos = GetScreenFromTile(State.CenterTile);
            var result = new Vector2(-centerPos.X, -centerPos.Y);
            result.X += (WorldPxWidth / 2.0f);
            result.Y += (WorldPxHeight / 2.0f);
            return result;
        }

        /// <summary>
        /// Gets the offset for the screen based on the scroll position
        /// </summary>
        /// <returns></returns>
        public Vector2 GetScreenOffset()
        {
            var result = GetPointScreenOffset();
            result.Y -= CadgeBaseLine;
            result.X -= (CadgeWidth / 2.0f);

            switch (State.Rotation)
            {
                case WorldRotation.TopLeft:
                    result.Y += TilePxHeightHalf;
                    break;
                case WorldRotation.TopRight:
                    result.X -= TilePxWidthHalf;
                    break;
                case WorldRotation.BottomRight:
                    result.Y -= TilePxHeightHalf;
                    break;
                case WorldRotation.BottomLeft:
                    result.X += TilePxWidthHalf;
                    break;
            }

            result.X = (float)Math.Round(result.X);
            result.Y = (float)Math.Round(result.Y);

            return result;
        }



        /// <summary>
        /// Gets indices of a tile given a position with a scroll offset.
        /// </summary>
        /// <param name="pos">The position of the tile.</param>
        /// <returns>Indices of tile at position.</returns>
        public Vector2 GetTileAtPosWithScroll(Vector2 pos)
        {
            return State.CenterTile + GetTileFromScreen(pos - new Vector2((WorldPxWidth / 2.0f), (WorldPxHeight / 2.0f)-TerrainHeight*(State.Level-1)));
        }

        /// <summary>
        /// Gets indices of a tile given a position without a scroll offset.
        /// </summary>
        /// <param name="pos">The position of the tile.</param>
        /// <returns>Indices of tile at position.</returns>
        public Vector2 GetTileFromScreen(Vector2 pos) //gets floor tile at a screen position w/o scroll
        {
            Vector2 result = new Vector2();
            switch (State.Rotation)
            {
                case WorldRotation.TopLeft:
                    result.Y = (pos.Y / TilePxHeightHalf - pos.X / TilePxWidthHalf) / 2;
                    result.X = result.Y + pos.X / TilePxWidthHalf;
                    break;
                case WorldRotation.TopRight:
                    result.Y = (- pos.Y / TilePxHeightHalf - pos.X / TilePxWidthHalf) / 2;
                    result.X = -result.Y - pos.X / TilePxWidthHalf;
                    break;
                case WorldRotation.BottomRight:
                    result.Y = (-pos.Y / TilePxHeightHalf + pos.X / TilePxWidthHalf) / 2;
                    result.X = result.Y - pos.X / TilePxWidthHalf;
                    break;
                case WorldRotation.BottomLeft:
                    result.Y = (pos.Y / TilePxHeightHalf + pos.X / TilePxWidthHalf) / 2;
                    result.X = pos.X / TilePxWidthHalf - result.Y;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Get screen coordinates from a tile coordinate without applying a scroll offset.
        /// </summary>
        /// <param name="tile">The tile to get screen coordinates from.</param>
        /// <returns>Tile's position in screen coordinates.</returns>
        public Vector2 GetScreenFromTile(Vector2 tile)
        {
            return GetScreenFromTile(new Vector3(tile.X, tile.Y, 0.0f));
        }

        /// <summary>
        /// Get screen coordinates from a tile coordinate with a scroll offset.
        /// </summary>
        /// <param name="tile">The tile to get screen coordinates from.</param>
        /// <returns>Tile's position in screen coordinates.</returns>
        public Vector2 GetScreenFromTile(Vector3 tile)
        {
            var screenx = 0.0f;
            var screeny = 0.0f;
            switch (State.Rotation)
            {
                case WorldRotation.TopLeft:
                    screenx = ((tile.X - tile.Y) * TilePxWidthHalf);
                    screeny = ((tile.X + tile.Y) * TilePxHeightHalf)-(tile.Z*OneUnitDistance*(float)Math.Cos(Math.PI/6));
                    break;
                case WorldRotation.TopRight:
                    /**as y gets bigger pxX gets smaller
                    as x gets bigger pxY gets bigger**/
                    screenx = ((- tile.X - tile.Y) * TilePxWidthHalf);
                    screeny = ((tile.X - tile.Y) * TilePxHeightHalf) - (tile.Z * OneUnitDistance * (float)Math.Cos(Math.PI / 6));
                    break;
                case WorldRotation.BottomRight:
                    screenx = ((- tile.X + tile.Y) * TilePxWidthHalf);
                    screeny = ((-tile.X - tile.Y) * TilePxHeightHalf) - (tile.Z * OneUnitDistance * (float)Math.Cos(Math.PI / 6));
                    break;
                case WorldRotation.BottomLeft:
                    screenx = ((tile.X + tile.Y) * TilePxWidthHalf);
                    screeny = ((-tile.X + tile.Y) * TilePxHeightHalf) - (tile.Z * OneUnitDistance * (float)Math.Cos(Math.PI / 6));
                    break;
            }

            return new Vector2(screenx, screeny);
        }

        public static float GetWorldFromTile(float tile)
        {
            return tile * WorldUnitsPerTile;
        }

        /// <summary>
        /// Get world units from tile units
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static Vector3 GetWorldFromTile(Vector2 tile)
        {
            return GetWorldFromTile(new Vector3(tile.X, tile.Y, 0.0f));
        }

        /// <summary>
        /// Get world units from tile units
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static Vector3 GetWorldFromTile(Vector3 tile)
        {
            return new Vector3(tile.X * WorldUnitsPerTile, tile.Z * WorldUnitsPerTile, tile.Y * WorldUnitsPerTile);
        }

        /// <summary>
        /// Gets a tile's world coordinates.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>Tile's world coordinates.</returns>
        public static Rectangle GetWorldFromTile(Rectangle tile)
        {
            tile.X = (int)(tile.X * WorldUnitsPerTile);
            tile.Y *= (int)(tile.Y * WorldUnitsPerTile);
            tile.Width = (int)(tile.Width * WorldUnitsPerTile);
            tile.Height *= (int)(tile.Height * WorldUnitsPerTile);
            return tile;
        }

        public void Invalidate()
        {
            switch (State.Zoom)
            {
                case WorldZoom.Far:
                    TilePxWidth = 32;
                    TilePxHeight = 16;

                    TilePxWidthHalf = 16;
                    TilePxHeightHalf = 8;

                    CadgeWidth = 34;
                    CadgeHeight = 96;
                    CadgeBaseLine = 87;
                    TerrainHeight = 59;
                    break;

                case WorldZoom.Medium:
                    TilePxWidth = 64;
                    TilePxHeight = 32;

                    TilePxWidthHalf = 32;
                    TilePxHeightHalf = 16;


                    CadgeWidth = 68;
                    CadgeHeight = 192;
                    CadgeBaseLine = 174;
                    TerrainHeight = 118;
                    break;

                case WorldZoom.Near:
                    TilePxWidth = 128;
                    TilePxHeight = 64;

                    TilePxWidthHalf = 64;
                    TilePxHeightHalf = 32;

                    CadgeWidth = 136;
                    CadgeHeight = 384;
                    CadgeBaseLine = 348;
                    TerrainHeight = 235;
                    break;
            }

            TerrainHeight *= -1;
            OneUnitDistance = (float)Math.Sqrt(Math.Pow(TilePxWidth, 2) / 2.0);
            TileSin60 = TilePxWidth / (float)Math.Sqrt(5.0);
            TileSin30 = TilePxHeight / (float)Math.Sqrt(5.0);
        }
    }
}
