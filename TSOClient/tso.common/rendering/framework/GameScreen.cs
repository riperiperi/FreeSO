using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.rendering.framework.model;
using Microsoft.Xna.Framework.Input;

namespace TSO.Common.rendering.framework
{
    /// <summary>
    /// A screen used for drawing.
    /// </summary>
    public class GameScreen
    {
        public List<IGraphicsLayer> Layers = new List<IGraphicsLayer>();
        public GraphicsDevice Device;
        public UpdateState State;

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
            State.PreviousKeyboardState = State.KeyboardState;
            State.KeyboardState = Keyboard.GetState();
            State.SharedData.Clear();
            State.Update();

            foreach (var layer in Layers){
                layer.Update(State);
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
            Device.RenderState.AlphaBlendEnable = true;
            Device.RenderState.DepthBufferEnable = true;

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
