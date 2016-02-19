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
    public class UIExternalContainer : UIContainer
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

        public int Width
        {
            get
            {
                return _Width;
            }
            set
            {
                _Width = value;
                BatchDirty = true;
            }
        }

        public int Height
        {
            get
            {
                return _Height;
            }
            set
            {
                _Height = value;
                BatchDirty = true;
            }
        }

        public UIExternalContainer(int width, int height)
        {
            _Width = width;
            _Height = height;
            inputManager = new InputManager();
            State = new UpdateState();
            mouse = new MouseState();

            WidthHeightChange(width, height);
        }

        public void WidthHeightChange(int width, int height)
        {
            //this should be called on the UI thread, otherwise monogame will lose it.
            if (Batch != null) Batch.Dispose();
            Batch = new UISpriteBatch(GameFacade.GraphicsDevice, 1, width, height, GlobalSettings.Default.AntiAlias?4:0);
            RawImage = new byte[width * height * 4];
            BatchDirty = false;

            State.UIState.Width = width;
            State.UIState.Height = height;
            DoRedraw = true;
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            if (BatchDirty) WidthHeightChange(Width, Height);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="mtx"></param>
        public override void Draw(UISpriteBatch batch)
        {
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

            
            tex.GetData(RawImage, 0, RawImage.Length/4);

            for (int i=0; i<RawImage.Length; i+=4)
            {
                var swap = RawImage[i];
                RawImage[i] = RawImage[i + 2];
                RawImage[i + 2] = swap;
            }
            if (OnFrame != null) OnFrame();
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

            State.SharedData.Clear();
            State.Update();

            State.SharedData.Add("ExternalDraw", false);
            State.SharedData["ExternalDraw"] = false;

            inputManager.HandleMouseEvents(State);
            State.MouseEvents.Clear();
            base.Update(State);

            if ((bool)State.SharedData["ExternalDraw"]) DoRedraw = true;
        }
    }

    public delegate void UIExternalFrameDone();
}
