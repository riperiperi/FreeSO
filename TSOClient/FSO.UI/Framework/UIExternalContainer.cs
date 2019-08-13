using FSO.Common;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Framework
{
    public class UIExternalContainer : UICachedContainer
    {
        UISpriteBatch Batch;

        public event UIExternalFrameDone OnFrame;

        private int _Width;
        private int _Height;
        private bool BatchDirty;
        private InputManager inputManager;

        public byte[] RawImage; //last rendered raw data from render target

        private UpdateState State;
        private bool DoRedraw = true;

        public MouseState mouse;
        public bool HasFocus;
        public bool HasUpdated;
        public int NeedFrames = 5;

        public Dictionary<char, Keys> NonPrintingKeys = new Dictionary<char, Keys>()
        {
            { '\b', Keys.Back },
        };

        public List<char> KeysPressed = new List<char>();

        public int Width
        {
            get
            {
                return (int)Size.X;
            }
            set
            {
                Size = new Vector2(value, Size.Y);
                BatchDirty = true;
            }
        }

        public int Height
        {
            get
            {
                return (int)Size.Y;
            }
            set
            {
                Size = new Vector2(Size.X, value);
                BatchDirty = true;
            }
        }

        public UIExternalContainer(int width, int height) : base()
        {
            Size = new Vector2(width, height);
            inputManager = new InputManager();
            State = new UpdateState();
            State.InputManager = inputManager;
            mouse = new MouseState();
            ClearColor = Color.White;

            //WidthHeightChange(width, height);
        }

        public void WidthHeightChange(int width, int height)
        {
            //this should be called on the UI thread, otherwise monogame will lose it.
            if (Batch != null) Batch.Dispose();
            Batch = new UISpriteBatch(GameFacade.GraphicsDevice, 1, width, height, (GlobalSettings.Default.AntiAlias > 0 && !FSOEnvironment.DirectX)? 4:0);
            RawImage = new byte[width * height * 4];
            BatchDirty = false;

            State.UIState.Width = width;
            State.UIState.Height = height;
            DoRedraw = true;
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            var invalid = Invalidated;
            base.PreDraw(batch);
            if (invalid && Target != null && NeedFrames-- > 0)
            {
                var expectedSize = Target.Width * Target.Height * 4;
                if (RawImage == null || RawImage.Length != expectedSize)
                {
                    RawImage = new byte[expectedSize];
                }
                Target.GetData(RawImage, 0, (GameFacade.DirectX) ? RawImage.Length : RawImage.Length);

                
                for (int i = 0; i < RawImage.Length; i += 4)
                {
                    var swap = RawImage[i];
                    RawImage[i] = RawImage[i + 2];
                    RawImage[i + 2] = swap;
                }

                if (OnFrame != null) OnFrame();
            }
            //    if (BatchDirty) WidthHeightChange(Width, Height);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="mtx"></param>
        public override void Draw(UISpriteBatch batch)
        {
            //base.Draw(batch);
            /*
            if (Width == 0 || Height == 0 || !DoRedraw) return;
            DoRedraw = false;
            batch = Batch;
            batch.UIBegin(BlendState.AlphaBlend, SpriteSortMode.Deferred);
            Promise<Texture2D> bufferTexture = null;
            using (batch.WithBuffer(ref bufferTexture))
            {
                lock (Children)
                {
                    foreach (var child in Children)
                    {
                        child.PreDraw(batch);
                    }
                    foreach (var child in Children)
                    {
                        child.Draw(batch);
                    }
                }
                batch.Pause();
                batch.Resume();
            }

            var tex = bufferTexture.Get();
            batch.End();
            */
            //batch.Draw(tex, Vector2.Zero, _BlendColor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public override void Update(UpdateState state)
        {
            HasUpdated = true;
            State.Time = (state != null)?state.Time:new GameTime();
            State.PreviousKeyboardState = State.KeyboardState;

            if (!HasFocus) mouse = new MouseState();
            State.MouseState = mouse;
            State.KeyboardState = Keyboard.GetState();

            if (State.MouseStates.Count == 0)
            {
                State.MouseStates.Add(new MultiMouse() { ID = 1 });
            }
            State.MouseStates[0].MouseState = mouse;

            State.SharedData.Clear();
            State.Update();

            State.SharedData.Add("ExternalDraw", false);
            State.SharedData["ExternalDraw"] = false;
            lock (KeysPressed)
            {
                State.FrameTextInput = KeysPressed.ToList();
                State.NewKeys = State.FrameTextInput.Where(x => NonPrintingKeys.ContainsKey(x)).Select(x => NonPrintingKeys[x]).ToList();
                KeysPressed.Clear();
            }
            inputManager.HandleMouseEvents(State);
            State.MouseEvents.Clear();
            base.Update(State);

            if ((bool)State.SharedData["ExternalDraw"]) Invalidate();// DoRedraw = true;
        }

        public void SubmitKey(char c)
        {
            lock (KeysPressed)
            {
                KeysPressed.Add(c);
            }
        }

        public void CleanupFocus(UpdateState state)
        {
            inputManager.SetFocus(null);
            Update(state);
        }
    }

    public delegate void UIExternalFrameDone();
}
