using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.ThreeD;

using tso.common.rendering.framework.camera;

namespace TSOClient.Code.Rendering.Lot.Model
{
    public class HouseRenderState
    {
        public const float WorldUnitsPerTile = 3.0f;


        public GraphicsDevice Device;
        public ICamera Camera;
        public Matrix World = Matrix.Identity;


        private HouseRotation _Rotation;
        public HouseRotation Rotation
        {
            get
            {
                return _Rotation;
            }
            set
            {
                _Rotation = value;
                InvalidateMetrics();
            }
        }


        private HouseZoom _Zoom;
        public HouseZoom Zoom
        {
            get
            {
                return _Zoom;
            }
            set
            {
                _Zoom = value;

                switch (_Zoom)
                {
                    case HouseZoom.CloseZoom:
                        CellWidth = 128;
                        CellHeight = 64;
                        CellHalfWidth = 64;
                        CellHalfHeight = 32;
                        break;

                    case HouseZoom.MediumZoom:
                        CellWidth = 63;
                        CellHeight = 32;
                        CellHalfWidth = 31;
                        CellHalfHeight = 16;
                        break;

                    case HouseZoom.FarZoom:
                        CellWidth = 32;
                        CellHeight = 16;
                        CellHalfWidth = 16;
                        CellHalfHeight = 8;
                        break;
                }
                CellPxSize = new Rectangle(0, 0, CellWidth, CellHeight);
                InvalidateMetrics();
            }
        }


        private Vector2 m_FocusTile;
        /// <summary>
        /// Which tile in the view is currently the center focus
        /// </summary>
        public Vector2 FocusTile
        {
            get
            {
                return m_FocusTile;
            }
            set
            {
                m_FocusTile = value;
                InvalidateMetrics();
            }
        }



        private void InvalidateMetrics()
        {
            /*
            CenterX = Size * CellHalfWidth;
            RightX = Size * CellWidth;
            MiddleY = Size * CellHalfHeight;
            BottomY = Size * CellHeight;
            */

            /**
             * We want to center the focus tile in the middle of the screen
             */
            var screenWidth = GlobalSettings.Default.GraphicsWidth;
            var screenHeight = GlobalSettings.Default.GraphicsHeight;

            var pxPoint = TileToScreenNoScroll(FocusTile);
            //CellOffset = new Vector2(
            //    -(pxPoint.X) + (screenWidth/2.0f),
            //    -(pxPoint.Y) + (screenHeight/2.0f));
            //var cx = CenterX - (FocusTile.Y * CellHalfWidth) + (FocusTile.X * CellHalfWidth);
            //var cy = (FocusTile.Y * CellHalfHeight) + (FocusTile.X * CellHalfHeight);

            //cy -= (FocusTile.Y / (float)Size) * CellHalfHeight;
            //cx += (FocusTile.X / (float)Size) * CellHalfWidth;

            //cy += (FocusTile.Y / Size) * CellHeight;
            //cx /= 1.03f;
            //cy /= 1.03f;

            //CellOffset = new Vector2(-cx + (screenWidth/2), -cy + (screenHeight/2));
            var offset = this.TileToScreenNoScroll(FocusTile);
            var screenx = offset.X;
            var screeny = offset.Y;

            screenx += (screenWidth / 2.0f);
            screeny += (screenHeight / 2.0f);

            CellOffset = new Vector2((float)screenx, (float)screeny);

            //TODO: I think we should fix this in the tile position calculation rather than
            //by offseting scroll coords

            //switch (Rotation)
            //{
            //    case HouseRotation.Angle90:
            //        CellOffset -= new Vector2(CellHalfWidth, 0.0f);
            //        break;

            //    case HouseRotation.Angle180:
            //        CellOffset -= new Vector2(0.0f, CellHalfHeight);
            //        break;

            //    case HouseRotation.Angle270:
            //        CellOffset -= new Vector2(CellHalfWidth, CellHeight);
            //        break;

            //    case HouseRotation.Angle360:
            //        CellOffset -= new Vector2(CellWidth, CellHalfHeight);
            //        break;
            //}


            //var scrollX = CenterX - (ScrollOffset.Y * CellHalfWidth) + (ScrollOffset.X * CellHalfWidth);
            //var scrollY = (ScrollOffset.Y * CellHalfHeight) + (ScrollOffset.X * CellHalfHeight);

            //CellOffset = new Vector2(-(scrollX/2), -(scrollY/2));
        }


        /** Numbers for internal calculation **/
        //private int CenterX;
        //private int RightX;
        //private int MiddleY;
        //private int BottomY;

        public int CellWidth { get; internal set; }
        public int CellHeight { get; internal set; }
        public int CellHalfWidth { get; internal set; }
        public int CellHalfHeight { get; internal set; }

        public Rectangle CellPxSize { get; internal set; }
        private Vector2 CellOffset;
        public int Size;






        //private static Vector3 WorldOffset = new Vector3(-(32.0f * WorldUnitsPerTile), 0.0f, -(32.0f * WorldUnitsPerTile));

        public Vector3 GetWorldFromTile(Vector2 tile)
        {
            //3 feet per tile
            return new Vector3(tile.X * WorldUnitsPerTile, 0.0f, tile.Y * WorldUnitsPerTile);// +WorldOffset;
        }

        public float TileToWorld(float tileCoord)
        {
            return WorldUnitsPerTile * tileCoord;
        }

        public Vector2 TileToScreen(Point point)
        {
            return TileToScreen(new Vector2(point.X, point.Y));
        }

        public Vector2 TileToScreen(Vector2 point)
        {
            point.X -= FocusTile.X;
            point.Y -= FocusTile.Y;
            var position = TileToScreenNoScroll(point);
            //return position + CellOffset;

            var screenWidth = GlobalSettings.Default.GraphicsWidth;
            var screenHeight = GlobalSettings.Default.GraphicsHeight;

            position.X -= (CellHalfWidth);

            return position + new Vector2((float)screenWidth / 2.0f, (float)screenHeight / 2.0f);
        }

        public Vector2 TileToScreenNoScroll(Vector2 point)
        {
            var tilex = point.X;
            var tiley = point.Y;

            var sin60 = CellWidth / Math.Sqrt(5.0); // sin(arctan(2)) or cos(arctan(1/2))
            var sin30 = CellHeight / Math.Sqrt(5.0); // sin(arctan(1/2)) or cos(arctan(2))

            var screenx = Math.Round((tilex - tiley) * sin60);
            var screeny = Math.Round((tilex + tiley) * sin30);

            return new Vector2((float)screenx, (float)screeny);

            //screenx -= (screenWidth / 2.0f);
            //screeny += (screenHeight / 2.0f);
            //CellOffset = new Vector2((float)screenx, (float)screeny);









            //float x = 0.0f;
            //float y = 0.0f;

            //switch (Rotation)
            //{
            //    case HouseRotation.Angle90:
            //        x = CenterX - (point.Y * CellHalfWidth) + (point.X * CellHalfWidth);
            //        y = (point.Y * CellHalfHeight) + (point.X * CellHalfHeight);
            //        break;

            //    case HouseRotation.Angle180:
            //        x = (point.Y * CellHalfWidth) + (point.X * CellHalfWidth);
            //        y = MiddleY + (point.Y * CellHalfHeight) - (point.X * CellHalfHeight);
            //        break;

            //    case HouseRotation.Angle270:
            //        x = CenterX + (point.Y * CellHalfWidth) - (point.X * CellHalfWidth);
            //        y = BottomY - (point.Y * CellHalfHeight) - (point.X * CellHalfHeight);
            //        break;

            //    case HouseRotation.Angle360:
            //        x = RightX - (point.X * CellHalfWidth) - (point.Y * CellHalfWidth);
            //        y = MiddleY + (point.X * CellHalfHeight) - (point.Y * CellHalfHeight);
            //        break;
            //}

            //return new Vector2(x, y);
        }
    }


    public enum HouseZoom
    {
        CloseZoom,
        MediumZoom,
        FarZoom
    }


    public enum HouseRotation
    {
        Angle90 = 0,
        Angle180 = 1,
        Angle270 = 2,
        Angle360 = 3
    }
}
