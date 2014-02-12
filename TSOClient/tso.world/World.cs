using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework;
using tso.common.rendering.framework.camera;
using Microsoft.Xna.Framework;
using tso.world.model;
using Microsoft.Xna.Framework.Graphics;
using tso.common.rendering.framework.model;

namespace tso.world
{
    public class World : _3DScene
    {
        public World(GraphicsDevice Device)
            : base(Device)
        {
        }

        /** How many pixels from each edge of the screen before we start scrolling the view **/
        public int ScrollBounds = 20;

        public WorldState State;
        private bool HasInitGPU;
        private bool HasInitBlueprint;
        private bool HasInit;
        private bool _OrderDirty = false;

        private World2D _2DWorld = new World2D();
        private World3D _3DWorld = new World3D();
        private Blueprint Blueprint;


        /// <summary>
        /// Setup anything that needs a GraphicsDevice
        /// </summary>
        /// <param name="layer"></param>
        public override void Initialize(_3DLayer layer)
        {
            base.Initialize(layer);

            /**
             * Setup world state, this object acts as a facade
             * to world objects as well as providing various
             * state settings for the world and helper functions
             */
            State = new WorldState(layer.Device, layer.Device.Viewport.Width, layer.Device.Viewport.Height, this);
            State._3D = new tso.world.utils._3DWorldBatch(State);
            State._2D = new tso.world.utils._2DWorldBatch(layer.Device, World2D.NUM_2D_BUFFERS, World2D.BUFFER_SURFACE_FORMATS);
            base.Camera = State.Camera;

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;

            _2DWorld.Initialize(layer);
        }

        public void InitBlueprint(Blueprint blueprint)
        {
            this.Blueprint = blueprint;
            _2DWorld.Init(blueprint);
            _3DWorld.Init(blueprint);

            HasInitBlueprint = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }

        /// <summary>
        /// Sorts the components by their preffered draw order. This is important so that
        /// the components are drawn on top of each other in the right order
        /// to ensure correct alpha blending.
        /// </summary>
        public void SortDrawOrder(){
            //Components.Sort(new WorldComponentSorter());
            _OrderDirty = false;
        }

        public void InvalidateZoom(){
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.All){
                item.OnZoomChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ZOOM));
        }

        public void InvalidateScroll(){
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.All){
                item.OnScrollChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.SCROLL));
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            /** Check for mouse scrolling **/
            var mouse = state.MouseState;

            if (State == null) { return; }

            var screenWidth = State.WorldSpace.WorldPxWidth;
            var screenHeight = State.WorldSpace.WorldPxHeight;

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
                switch (State.Rotation)
                {
                    case WorldRotation.TopLeft:
                        switch (cursor)
                        {
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


                    case WorldRotation.TopRight:
                        switch (cursor){
                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(1, -1);
                                break;
                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(-1, 1);
                                break;
                            case CursorType.ArrowLeft:
                                scrollVector = new Vector2(1, 1);
                                break;
                            case CursorType.ArrowRight:
                                scrollVector = new Vector2(-1, -1);
                                break;
                        }
                        break;

                    case WorldRotation.BottomRight:
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

                    case WorldRotation.BottomLeft:
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
                if (scrollVector != Vector2.Zero)
                {
                    State.CenterTile += scrollVector * new Vector2(0.0625f, 0.0625f);
                }
            }

            CursorManager.INSTANCE.SetCursor(cursor);
            //GameFacade.Cursor.SetCursor(cursor);
        }


        /// <summary>
        /// Pre-Draw
        /// </summary>
        /// <param name="device"></param>
        public override void PreDraw(GraphicsDevice device)
        {
            base.PreDraw(device);
            if (HasInit == false) { return; }

            //For all the tiles in the dirty list, re-render them
            State._2D.Begin(this.State.Camera);
            _2DWorld.PreDraw(device, State);
            State._2D.End();

            State._3D.Begin(device);
            _3DWorld.PreDraw(device, State);
            State._3D.End();
        }

        /// <summary>
        /// We will just take over the whole rendering of this scene :)
        /// </summary>
        /// <param name="device"></param>
        public override void Draw(GraphicsDevice device){
            if (HasInit == false) { return; }

            State._3D.Begin(device);
            State._2D.Begin(this.State.Camera);
            _3DWorld.DrawBefore2D(device, State);
            _2DWorld.Draw(device, State);
            State._2D.End();
            _3DWorld.DrawAfter2D(device, State);
            State._3D.End();
        }

    }
}
