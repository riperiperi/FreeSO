/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace FSO.Common.Rendering.Framework
{
    /// <summary>
    /// A screen used for drawing.
    /// </summary>
    public class GameScreen
    {
        public List<IGraphicsLayer> Layers = new List<IGraphicsLayer>();
        public GraphicsDevice Device;
        public UpdateState State;

        private int touchedFrames;

        private const int TOUCH_ACCEPT_TIME = 5;

        public GameScreen(GraphicsDevice device)
        {
            this.Device = device;

            State = new UpdateState();
        }

        /// <summary>
        /// Adds a graphical element to this scene.
        /// </summary>
        /// <param name="layer">Element inheriting from IGraphicsLayer.</param>
        public void Add(IGraphicsLayer layer)
        {
            layer.Initialize(Device);
            Layers.Add(layer);
        }

        public void Update(GameTime time)
        {

            State.Time = time;
            State.MouseState = Mouse.GetState();
            TouchStub(State);

            State.PreviousKeyboardState = State.KeyboardState;
            State.KeyboardState = Keyboard.GetState();
            State.SharedData.Clear();
            State.Update();

            foreach (var layer in Layers){
                layer.Update(State);
            }
        }

        private void TouchStub(UpdateState state)
        {
            var test = TouchPanel.EnableMouseTouchPoint;
            TouchCollection touches = TouchPanel.GetState();
            if (touches.Count > 0)
            {
                if (touchedFrames < TOUCH_ACCEPT_TIME)
                {
                    touchedFrames++;
                }
                else
                {
                    //right click, take center
                    Vector2 avg = new Vector2();
                    for (int i = 0; i < touches.Count; i++)
                    {
                        avg += touches[i].Position;
                    }
                    avg /= touches.Count;

                    state.MouseState = new MouseState(
                        (int)avg.X, (int)avg.Y, state.MouseState.ScrollWheelValue,
                        (touches.Count > 1) ? ButtonState.Released : ButtonState.Pressed,
                        (touches.Count > 1) ? ButtonState.Pressed : ButtonState.Released,
                        ButtonState.Released,
                        ButtonState.Released,
                        ButtonState.Released
                        );

                    state.TouchMode = true;
                }
            }
            else
            {
                touchedFrames = 0;
                state.TouchMode = false;
            }
        }

        public void Draw(GameTime time)
        {
            lock (Device)
            {
                foreach (var layer in Layers)
                {
                    layer.PreDraw(Device);
                }
            }

            Device.Clear(new Color(0x72, 0x72, 0x72));
            //Device.RasterizerState.AlphaBlendEnable = true;
            //Device.DepthStencilState.DepthBufferEnable = true;

            lock (Device)
            {
                foreach (var layer in Layers)
                {
                    layer.Draw(Device);
                }
            }
        }
    }
}
