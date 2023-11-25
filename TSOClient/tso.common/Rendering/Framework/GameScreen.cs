using System.Collections.Generic;
using System.Linq;
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

        public static Color ClearColor = new Color(0x72, 0x72, 0x72);

        private int touchedFrames;
		private int lastTouchCount;
		private MouseState lastMouseState;
        private Vector2? prevTouchAvg;
        private const int TOUCH_ACCEPT_TIME = 5;

        public GameScreen(GraphicsDevice device)
        {
            this.Device = device;

            State = new UpdateState();
        }

        private static List<char> TextCharacters = new List<char>();
        public static void TextInput(object sender, TextInputEventArgs e)
        {
            TextCharacters.Add(e.Character);
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

        public void Update(GameTime time, bool hasFocus)
        {
            State.Time = time;
            State.PreviousKeyboardState = State.KeyboardState;
            State.FrameTextInput = TextCharacters;

            var touchMode = FSOEnvironment.SoftwareKeyboard;

            if (touchMode)
            {
                if (FSOEnvironment.SoftwareDepth) State.KeyboardState = new KeyboardState();
                TouchCollection touches = TouchPanel.GetState();

                var missing = new HashSet<MultiMouse>(State.MouseStates);
                //relate touches to their last virtual mouse
                foreach (var touch in touches)
                {
                    var mouse = State.MouseStates.FirstOrDefault(x => x.ID == touch.Id);
                    if (mouse == null)
                    {
                        mouse = new MultiMouse { ID = touch.Id };
                        State.MouseStates.Add(mouse);
                    }
                    missing.Remove(mouse);

                    mouse.MouseState = new MouseState(
                        (int)touch.Position.X, (int)touch.Position.Y, 0,
                        ButtonState.Pressed,
                        ButtonState.Released,
                        ButtonState.Released,
                        ButtonState.Released,
                        ButtonState.Released
                        );
                }
                
                //if virtual mouses no longer have their touch, they are performing a "mouse up"
                //if the state has mouseovers, we should record the mouse state as being lifted.
                foreach (var miss in missing)
                {
                    if (miss.LastMouseOver == null && miss.LastMouseDown == null)
                    {
                        State.MouseStates.Remove(miss);
                    } else
                    {
                        miss.MouseState = new MouseState(miss.MouseState.X, miss.MouseState.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                        miss.Dead = true;
                    }
                }
            }
            else
            {
                //single mouse state
                if (hasFocus)
                {
                    State.MouseState = Mouse.GetState();
                    State.KeyboardState = Keyboard.GetState();
                }
                else
                {
                    State.MouseState = new MouseState();
                    State.KeyboardState = new KeyboardState();
                }

                if (State.KeyboardState.IsKeyDown(Keys.LeftAlt) && State.MouseState.LeftButton == ButtonState.Pressed)
                {
                    //emulated middle click with alt
                    var ms = State.MouseState;
                    State.MouseState = new MouseState(ms.X, ms.Y, ms.ScrollWheelValue, ButtonState.Released, ButtonState.Pressed, ms.RightButton, ms.XButton1, ms.XButton2);
                }

                if (State.MouseStates.Count == 0)
                {
                    State.MouseStates.Add(new MultiMouse { ID = 1 });
                }

                State.MouseStates[0].MouseState = State.MouseState;
            }


            State.SharedData.Clear();
            State.Update();

            foreach (var layer in Layers){
                layer.Update(State);
            }

            TextCharacters.Clear();
        }

        private void TouchStub(UpdateState state)
        {
            var test = TouchPanel.EnableMouseTouchPoint;
            TouchCollection touches = TouchPanel.GetState();
			if (touches.Count != lastTouchCount) touchedFrames = 0;
			lastTouchCount = touches.Count;
            if (touches.Count > 0)
            {
				Vector2 avg = new Vector2();
				for (int i = 0; i < touches.Count; i++)
				{
					avg += touches[i].Position;
				}
				avg /= touches.Count;

                if (touchedFrames < TOUCH_ACCEPT_TIME)
                {
                    avg = prevTouchAvg ?? avg;
					state.MouseState = new MouseState(
						(int)avg.X, (int)avg.Y, state.MouseState.ScrollWheelValue,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released
					);
                    touchedFrames++;
                }
                else
                {
                    state.MouseState = new MouseState(
                        (int)avg.X, (int)avg.Y, state.MouseState.ScrollWheelValue,
						(touches.Count > 1) ? ButtonState.Released : ButtonState.Pressed,
                        (touches.Count > 1) ? ButtonState.Pressed : ButtonState.Released,
                        (touches.Count > 1) ? ButtonState.Pressed : ButtonState.Released,
                        ButtonState.Released,
                        ButtonState.Released
                        );
                    prevTouchAvg = avg;

                    state.TouchMode = true;
                }
            }
            else
            {
                prevTouchAvg = null;
                touchedFrames = 0;
				if (state.TouchMode) state.MouseState = new MouseState(
						lastMouseState.X, lastMouseState.Y, state.MouseState.ScrollWheelValue,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released,
						ButtonState.Released
					);
                //state.TouchMode = false;
            }
			lastMouseState = state.MouseState;
        }

        public void Draw(GameTime time)
        {
            lock (Device)
            {
                foreach (var layer in Layers.Reverse<IGraphicsLayer>())
                {
                    layer.PreDraw(Device);
                }
            }

            Device.SetRenderTarget(null);
            Device.BlendState = BlendState.AlphaBlend;
            Device.Clear(ClearColor);
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
