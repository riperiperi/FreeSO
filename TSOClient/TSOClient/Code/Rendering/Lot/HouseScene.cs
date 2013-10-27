using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.ThreeD;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;

namespace TSOClient.Code.Rendering.Lot
{
    public class HouseScene : ThreeDScene
    {
        /** How many pixels from each edge of the screen before we start scrolling the view **/
        public int ScrollBounds = 6;


        public HouseRenderState RenderState { get; internal set; }
        private House2DScene Scene2D;
        private House3DScene Scene3D;

        private HouseModel Model;


        public HouseScene()
        {
            Init();
        }


        /// <summary>
        /// Setup the rendering scene
        /// </summary>
        public void Init()
        {
            //Default render state
            RenderState = new HouseRenderState();
            RenderState.Rotation = HouseRotation.Angle90;
            RenderState.Zoom = HouseZoom.FarZoom;
            RenderState.Device = GameFacade.GraphicsDevice;
            

            Init2DWorld();
            Init3DWorld();
        }

        /// <summary>
        /// Setup the 2D world
        /// </summary>
        protected void Init2DWorld()
        {
            Scene2D = new House2DScene();
        }

        /// <summary>
        /// Setup the 3D world
        /// </summary>
        protected void Init3DWorld()
        {
            //Camera, default to a random position, when we load the lot it will change
            var cameraPosition = new Vector3(0,0,0);
            var cameraTarget = new Vector3(0,0,0);
            Camera = new OrthographicCamera(cameraPosition, cameraTarget, Vector3.Up);
            //We have to squish the output vertically a bit so that tiles are twice as wide as they are tall.
            Camera.AspectRatioMultiplier = 1.03f;//0.95567f;
            RenderState.Camera = Camera;

            Scene3D = new House3DScene(RenderState);
        }


        /// <summary>
        /// Load a house from its definition
        /// </summary>
        /// <param name="house"></param>
        public void LoadHouse(HouseData house)
        {
            RenderState.Size = house.Size;

            /**
             * Camera should be at the edge of the screen looking onto the
             * center point of the lot at a 30 degree angle
             */
            var radius = RenderState.TileToWorld( (house.Size / 2.0f) + 0.5f  );
            var opposite = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * radius;

            Camera.Position = new Vector3(radius * 2, opposite, radius * 2);
            Camera.Target = new Vector3(radius, 0.0f, radius);
            //Camera.Translation = new Vector3(-radius, 0.0f, -radius);
            Camera.Zoom = 152;

            /**
             * Setup the 2D space
             */
            Model = new HouseModel();
            Model.LoadHouse(house);

            Scene2D.LoadHouse(Model);
            Scene3D.LoadHouse(Model);

            //Center point of the center most tile
            ViewCenter = new Vector2((house.Size / 2) + 0.5f, (house.Size / 2) + 0.5f);
        }



        public override void Update(UpdateState state)
        {
            base.Update(state);

            /** Check for mouse scrolling **/
            var mouse = state.MouseState;

            var screenWidth = GlobalSettings.Default.GraphicsWidth;
            var screenHeight = GlobalSettings.Default.GraphicsHeight;

            /** Corners **/
            var xBound = screenWidth - ScrollBounds;
            var yBound = screenHeight - ScrollBounds;

            var cursor = CursorType.Normal;
            var scrollVector = new Vector2(0, 0);

            if (mouse.X > 0 && mouse.Y > 0 && mouse.X < screenWidth && mouse.Y < screenHeight)
            {
                if (mouse.Y <= ScrollBounds)
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Scroll top left **/
                        cursor = CursorType.ArrowUpLeft;
                        scrollVector = new Vector2(-1, -1);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Scroll top right **/
                        cursor = CursorType.ArrowUpRight;
                        scrollVector = new Vector2(1, -1);
                    }
                    else
                    {
                        /** Scroll up **/
                        cursor = CursorType.ArrowUp;
                        scrollVector = new Vector2(0, -1);
                    }
                }
                else if (mouse.Y <= yBound)
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Left **/
                        cursor = CursorType.ArrowLeft;
                        scrollVector = new Vector2(-1, 0);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Right **/
                        cursor = CursorType.ArrowRight;
                        scrollVector = new Vector2(1, -1);
                    }
                }
                else
                {
                    if (mouse.X <= ScrollBounds)
                    {
                        /** Scroll bottom left **/
                        cursor = CursorType.ArrowDownLeft;
                        scrollVector = new Vector2(-1, 1);
                    }
                    else if (mouse.X >= xBound)
                    {
                        /** Scroll bottom right **/
                        cursor = CursorType.ArrowDownRight;
                        scrollVector = new Vector2(1, 1);
                    }
                    else
                    {
                        /** Scroll down **/
                        cursor = CursorType.ArrowDown;
                        scrollVector = new Vector2(0, 1);
                    }
                }
            }

            if (cursor != CursorType.Normal)
            {

                /**
                 * Calculate scroll vector based on rotation & scroll type
                 */
                scrollVector = new Vector2();
                switch (Rotation){
                    case HouseRotation.Angle90:
                        switch (cursor){
                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(1, 1);
                                break;

                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(-1, -1);
                                break;

                            case CursorType.ArrowLeft:
                                scrollVector = new Vector2(-1, 1);
                                break;

                            case CursorType.ArrowRight:
                                scrollVector = new Vector2(1, -1);
                                break;
                        }
                        break;


                    case HouseRotation.Angle180:
                        switch (cursor)
                        {
                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(-1, 1);
                                break;

                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(1, -1);
                                break;
                        }
                        break;

                    case HouseRotation.Angle270:
                        switch (cursor)
                        {
                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(-1, -1);
                                break;

                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(1, 1);
                                break;
                        }
                        break;

                    case HouseRotation.Angle360:
                        switch (cursor)
                        {
                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(1, -1);
                                break;

                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(-1, 1);
                                break;
                        }
                        break;
                }

                /** We need to scroll **/
                ViewCenter += scrollVector * new Vector2(0.0625f, 0.0625f);
            }

            GameFacade.Cursor.SetCursor(cursor);
        }


        /// <summary>
        /// Render the house scene
        /// </summary>
        /// <param name="device"></param>
        public override void Draw(GraphicsDevice device)
        {
            Scene3D.Draw(device, RenderState);
            Scene2D.Draw(device, RenderState);
        }










        #region View Modification

        public HouseZoom Zoom
        {
            get
            {
                return RenderState.Zoom;
            }
            set
            {
                RenderState.Zoom = value;
                Scene2D.OnZoomChange(RenderState);
                Scene3D.OnZoomChange(RenderState);
            }
        }

        /// <summary>
        /// Set the camera rotation
        /// </summary>
        public HouseRotation Rotation
        {
            get
            {
                return RenderState.Rotation;
            }
            set
            {
                RenderState.Rotation = value;
                Scene2D.OnRotationChange(RenderState);
            }
        }

        public Vector2 ViewCenter
        {
            get { return RenderState.FocusTile; }
            set
            {
                RenderState.FocusTile = value;
                Scene2D.OnScrollChange(RenderState);
                Scene3D.OnScrollChange(RenderState);
            }
        }

        #endregion

    }
}
