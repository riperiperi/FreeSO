using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework.camera;
using Microsoft.Xna.Framework;
using tso.world.utils;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world
{
    public class WorldState {

        private World World;
        public GraphicsDevice Device;

        public WorldState(GraphicsDevice device, float worldPxWidth, float worldPxHeight, World world)
        {
            this.Device = device;
            this.World = world;
            //this.OrthographicCamera = new OrthographicCamera(device, Vector3.Zero, Vector3.Zero, Vector3.Up);
            this.WorldCamera = new WorldCamera(device);
            //OrthographicCamera.AspectRatioMultiplier = 0.96f;
            //OrthographicCamera.AspectRatioMultiplier = 1.03f;

            WorldSpace = new WorldSpace(worldPxWidth, worldPxHeight, this);
            Zoom = WorldZoom.Near;
            Rotation = WorldRotation.TopRight;
        }

        protected WorldCamera WorldCamera;
        //protected OrthographicCamera OrthographicCamera;
        public ICamera Camera {
            get { return WorldCamera; }
        }

        public WorldSpace WorldSpace;
        public _2DWorldBatch _2D;
        public _3DWorldBatch _3D;

        private int _WorldSize;
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

        /// <summary>
        /// What zoom level is being displayed
        /// </summary>
        private WorldZoom _Zoom;
        public WorldZoom Zoom {
            get{ return _Zoom; }
            set{ _Zoom = value; InvalidateZoom(); }
        }

        /// <summary>
        /// What rotation is being displayed
        /// </summary>
        private WorldRotation _Rotation;
        public WorldRotation Rotation {
            get { return _Rotation; }
            set { _Rotation = value; InvalidateRotation();  }
        }

        private Vector2 _CenterTile = Vector2.Zero;
        public Vector2 CenterTile
        {
            get { return _CenterTile; }
            set { _CenterTile = value; InvalidateScroll(); }
        }

        protected void InvalidateZoom(){
            WorldSpace.Invalidate();
            InvalidateCamera();
            World.InvalidateZoom();
        }

        protected void InvalidateRotation(){
            WorldSpace.Invalidate();
            InvalidateCamera();
        }

        protected void InvalidateScroll(){
            WorldSpace.Invalidate();
            InvalidateCamera();
            World.InvalidateScroll();
        }

        protected void InvalidateWorldSize(){
            var edge = _WorldSize + 1.0f;
            var radius = WorldSpace.WorldUnitsPerTile * (edge / 2.0f);
            var opposite = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * radius;

            Camera.Position = new Vector3(radius * 2, opposite, radius * 2);
            Camera.Target = new Vector3(radius, 0.0f, radius);

            //Center point of the center most tile
            CenterTile = new Vector2((_WorldSize / 2.0f), (_WorldSize / 2.0f));
            InvalidateCamera();
        }

        protected void InvalidateCamera(){
            WorldCamera.CenterTile = CenterTile;
            WorldCamera.Zoom = Zoom;
            WorldCamera.Rotation = Rotation;
        }
    }


    /// <summary>
    /// Holds information about the world space and drawing numbers
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

        public float TileSin60;
        public float TileSin30;

        /// <summary>
        /// How big is the world view area in pixels
        /// </summary>
        public float WorldPxWidth;
        public float WorldPxHeight;


        private WorldState State;
        public WorldSpace(float worldPxWidth, float worldPxHeight, WorldState state){
            this.State = state;
            this.WorldPxWidth = worldPxWidth;
            this.WorldPxHeight = worldPxHeight;
        }

        /// <summary>
        /// Gets the offset for the screen based on the scroll position
        /// </summary>
        /// <returns></returns>
        public Vector2 GetScreenOffset()
        {
            var centerPos = GetScreenFromTile(State.CenterTile);
            var result = new Vector2(-centerPos.X, -centerPos.Y);
            result.X += (WorldPxWidth / 2.0f);
            result.Y += (WorldPxHeight / 2.0f);
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
            }

            result.X = (float)Math.Round(result.X);
            result.Y = (float)Math.Round(result.Y);

            return result;
        }


        /// <summary>
        /// Get screen coordinates from a tile coordinate without applying a scroll offset
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns
        public Vector2 GetScreenFromTile(Vector2 tile){
            return GetScreenFromTile(new Vector3(tile.X, tile.Y, 0.0f));
        }

        public Vector2 GetScreenFromTile(Vector3 tile)
        {
            var screenx = 0.0f;
            var screeny = 0.0f;

            switch (State.Rotation)
            {
                case WorldRotation.TopLeft:
                    screenx = ((tile.X - tile.Y) * TilePxWidthHalf);
                    screeny = ((tile.X + tile.Y) * TilePxHeightHalf);
                    break;
                case WorldRotation.TopRight:
                    /**as y gets bigger pxX gets smaller
                    as x gets bigger pxY gets bigger**/
                    screenx = -((tile.X * TilePxWidthHalf) + (tile.Y * TilePxWidthHalf));
                    screeny = -((tile.Y * TilePxHeightHalf) - (tile.X * TilePxHeightHalf));
                    break;
                case WorldRotation.BottomRight:
                    screenx = ((tile.X + tile.Y) * TilePxWidthHalf);
                    screeny = ((tile.X - tile.Y) * TilePxHeightHalf);
                    break;
                case WorldRotation.BottomLeft:
                    screenx = -((tile.X - tile.Y) * TilePxWidthHalf);
                    screeny = ((tile.X + tile.Y) * TilePxHeightHalf);
                    break;
            }

            //screenx = ((tile.X * TilePxWidthHalf) + (tile.Y * TilePxWidthHalf));
            //screeny = ((tile.Y * TilePxHeightHalf) - (tile.X * TilePxHeightHalf));

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

        public static Rectangle GetWorldFromTile(Rectangle tile)
        {
            tile.X = (int)(tile.X * WorldUnitsPerTile);
            tile.Y *= (int)(tile.Y * WorldUnitsPerTile);
            tile.Width = (int)(tile.Width * WorldUnitsPerTile);
            tile.Height *= (int)(tile.Height * WorldUnitsPerTile);
            return tile;
        }

        public void Invalidate(){
            switch (State.Zoom){
                case WorldZoom.Far:
                    TilePxWidth = 32;
                    TilePxHeight = 16;

                    TilePxWidthHalf = 16;
                    TilePxHeightHalf = 8;

                    CadgeWidth = 34;
                    CadgeHeight = 96;
                    CadgeBaseLine = 87;
                    break;

                case WorldZoom.Medium:
                    TilePxWidth = 64;
                    TilePxHeight = 32;

                    TilePxWidthHalf = 32;
                    TilePxHeightHalf = 16;


                    CadgeWidth = 68;
                    CadgeHeight = 192;
                    CadgeBaseLine = 174;
                    break;

                case WorldZoom.Near:
                    TilePxWidth = 128;
                    TilePxHeight = 64;

                    TilePxWidthHalf = 64;
                    TilePxHeightHalf = 32;

                    CadgeWidth = 136;
                    CadgeHeight = 384;
                    CadgeBaseLine = 348;
                    break;
            }
            //1.03
            //123
            TileSin60 = TilePxWidth / (float)Math.Sqrt(5.0);
            TileSin30 = TilePxHeight / (float)Math.Sqrt(5.0);
        }

    }
}
