/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework;
using TSO.Common.rendering.framework.camera;
using Microsoft.Xna.Framework;
using tso.world.model;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.rendering.framework.model;

namespace tso.world
{
    /// <summary>
    /// Represents world (I.E lots in the game.)
    /// </summary>
    public class World : _3DScene
    {
        /// <summary>
        /// Creates a new World instance.
        /// </summary>
        /// <param name="Device">A GraphicsDevice instance.</param>
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

        public void InvalidateZoom()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.All){
                item.OnZoomChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ZOOM));
        }

        public void InvalidateRotation()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.All)
            {
                item.OnRotationChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.ROTATE));
        }

        public void InvalidateScroll()
        {
            if (Blueprint == null) { return; }

            foreach (var item in Blueprint.All){
                item.OnScrollChanged(State);
            }
            Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.SCROLL));
        }

        public bool TestScroll(UpdateState state)
        {
            var mouse = state.MouseState;

            if (State == null) { return false; }

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
                        switch (cursor)
                        {
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

                            case CursorType.ArrowLeft:
                                scrollVector = new Vector2(1, -1);
                                break;

                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(1, 1);
                                break;

                            case CursorType.ArrowRight:
                                scrollVector = new Vector2(-1, 1);
                                break;
                        }
                        break;

                    case WorldRotation.BottomLeft:
                        switch (cursor)
                        {
                            case CursorType.ArrowUp:
                                scrollVector = new Vector2(1, -1);
                                break;

                            case CursorType.ArrowLeft:
                                scrollVector = new Vector2(-1, -1);
                                break;

                            case CursorType.ArrowDown:
                                scrollVector = new Vector2(-1, 1);
                                break;

                            case CursorType.ArrowRight:
                                scrollVector = new Vector2(1, 1);
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

            if (cursor != CursorType.Normal)
            {
                CursorManager.INSTANCE.SetCursor(cursor);
                return true; //we scrolled, return true and set cursor
            }
            return false;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            /** Check for mouse scrolling **/
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

        /// <summary>
        /// Gets the ID of the object at a given position.
        /// </summary>
        /// <param name="x">X position of object.</param>
        /// <param name="y">Y position of object.</param>
        /// <param name="gd">GraphicsDevice instance.</param>
        /// <returns>ID of object at position if found.</returns>
        public short GetObjectIDAtScreenPos(int x, int y, GraphicsDevice gd)
        {
            State._2D.Begin(this.State.Camera);
            return _2DWorld.GetObjectIDAtScreenPos(x, y, gd, State);
        }
    }
}
