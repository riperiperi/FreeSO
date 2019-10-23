/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;
using System.Diagnostics;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Utils;
using FSO.Common;
using FSO.Content;
using FSO.SimAntics.Engine;
using FSO.SimAntics;

namespace FSO.Client.UI
{
    public class UILayer : IGraphicsLayer
    {
        private Microsoft.Xna.Framework.Game m_G;
        private List<UIScreen> m_Screens = new List<UIScreen>();
        private List<UIExternalContainer> m_ExtContainers = new List<UIExternalContainer>();
        private List<IUIProcess> m_UIProcess = new List<IUIProcess>();

        public UITooltipProperties TooltipProperties = new UITooltipProperties();
        public string Tooltip;

        private SpriteFont m_SprFontBig;
        private SpriteFont m_SprFontSmall;

        //For displaying 3D objects (sims).
        private Matrix m_WorldMatrix, m_ViewMatrix, m_ProjectionMatrix;
        private Dictionary<int, string> m_TextDict;

        //for fps counter
        private Stopwatch fpsStopwatch;

        /// <summary>
        /// Top most UI container
        /// </summary>
        private UIContainer mainUI;
        private UIContainer dialogContainer;

        public InputManager inputManager;
        private UIScreen currentScreen;

        /** Animation utility **/
        public UITween Tween;

        public Microsoft.Xna.Framework.Game GameComponent
        {
            get { return m_G; }
        }

        public UIContainer Root
        {
            get { return mainUI; }
        }

        /// <summary>
        /// A worldmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix WorldMatrix
        {
            get { return m_WorldMatrix; }
            set { m_WorldMatrix = value; }
        }

        /// <summary>
        /// A viewmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return m_ViewMatrix; }
            set { m_WorldMatrix = value; }
        }

        /// <summary>
        /// A projectionmatrix, used to display 3D objects (sims).
        /// Initialized in the ScreenManager's constructor.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return m_ProjectionMatrix; }
            set { m_ProjectionMatrix = value; }
        }

        /// <summary>
        /// The graphicsdevice that is part of the game instance.
        /// Used when calling XNA's graphic functions.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return m_G.GraphicsDevice; }
        }

        /// <summary>
        /// The UIScreen instance that is currently being 
        /// updated and rendered by this ScreenManager instance.
        /// </summary>
        public UIScreen CurrentUIScreen
        {
            get
            {
                return currentScreen;
            }
        }

        /// <summary>
        /// Gets or sets the internal dictionary containing all the strings for the game.
        /// </summary>
        public Dictionary<int, string> TextDict
        {
            get { return m_TextDict; }
            set { m_TextDict = value; }
        }

        public UILayer(Microsoft.Xna.Framework.Game G)
        {
            fpsStopwatch = new Stopwatch();
            fpsStopwatch.Start();

            m_G = G;

            m_WorldMatrix = Matrix.Identity;
            m_ViewMatrix = Matrix.CreateLookAt(Vector3.Right * 5, Vector3.Zero, Vector3.Forward);
            m_ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)GraphicsDevice.PresentationParameters.BackBufferWidth / 
                    (float)GraphicsDevice.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);

            TextStyle.DefaultTitle = new TextStyle {
                Font = GameFacade.MainFont,
                VFont = GameFacade.VectorFont,
                Size = 10,
                Color = new Color(255,249,157),
                SelectedColor = new Color(0x00, 0x38, 0x7B),
                SelectionBoxColor = new Color(255, 249, 157)
            };

            TextStyle.DefaultButton = new TextStyle
            {
                Font = GameFacade.MainFont,
                VFont = GameFacade.VectorFont,
                Size = 10,
                Color = new Color(255, 249, 157),
                SelectedColor = new Color(0x00, 0x38, 0x7B),
                SelectionBoxColor = new Color(255, 249, 157)
            };

            TextStyle.DefaultLabel = new TextStyle
            {
                Font = GameFacade.MainFont,
                VFont = GameFacade.VectorFont,
                Size = 10,
                Color = new Color(255, 249, 157),
                SelectedColor = new Color(0x00, 0x38, 0x7B),
                SelectionBoxColor = new Color(255, 249, 157)
            };

            Tween = new UITween();
            this.AddProcess(Tween);

            inputManager = new InputManager();
            inputManager.RequireWindowFocus = true;
            mainUI = new UIContainer();
            dialogContainer = new UIContainer();
            mainUI.Add(dialogContainer);

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new UISpriteBatch(GraphicsDevice, 0);
            //GameFacade.OnContentLoaderReady += new BasicEventHandler(GameFacade_OnContentLoaderReady);
            m_G.GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(GraphicsDevice_DeviceReset);
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            for (int i = 0; i < m_Screens.Count; i++)
                m_Screens[i].DeviceReset(m_G.GraphicsDevice);
        }

        public void AddProcess(IUIProcess Proc)
        {
            m_UIProcess.Add(Proc);
        }

        public void RemoveProcess(IUIProcess Proc)
        {
            m_UIProcess.Remove(Proc);
        }

        /// <summary>
        /// Adds a UIScreen instance to this ScreenManager's list of screens.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="Screen">The UIScreen instance to be added.</param>
        public void AddScreen(UIScreen Screen)
        {
            /*if (currentScreen != null)
            {
                mainUI.Remove(currentScreen);
            }*/
            /** Add screen on top **/
            mainUI.Add(Screen);
            /** Bring dialogs to top **/
            mainUI.Add(dialogContainer);
            /** Bring debug to the top **/
            //mainUI.Add(debugButton);

            Screen.OnShow();

            m_Screens.Add(Screen);
            currentScreen = Screen;
        }

        public void RemoveScreen(UIScreen Screen)
        {
            if (Screen == currentScreen)
            {
                currentScreen = null;
            }
            Screen.OnHide();
            mainUI.Remove(Screen);
            m_Screens.Remove(Screen);

            /** Put the previous screen back into the UI **/
            if (m_Screens.Count > 0)
            {
                currentScreen = m_Screens.Last();
                mainUI.AddAt(0, currentScreen);
            }
        }

        public void AddExternal(UIExternalContainer cont)
        {
            //todo: init?
            lock (m_ExtContainers)
            {
                m_ExtContainers.Add(cont);
            }
        }

        public void RemoveExternal(UIExternalContainer cont)
        {
            lock (m_ExtContainers)
            {
                //todo: release resources?
                GameThread.NextUpdate(x =>
                {
                    cont.CleanupFocus(x);
                    cont.Removed();
                });
                m_ExtContainers.Remove(cont);
            }
        }

        public void RemoveCurrent()
        {
            /** Remove all dialogs **/
            while (Dialogs.Count > 0)
            {
                RemoveDialog(Dialogs[0]);
            }

            var currentScreen = mainUI.GetChildren().OfType<UIScreen>().FirstOrDefault();
            if (currentScreen != null)
            {
                ((UIScreen)currentScreen).OnHide();
                mainUI.Remove(currentScreen);
                m_Screens.Remove(currentScreen);
            }
        }

        public void Update(UpdateState state)
        {
            GameThread.DigestUpdate(state);

            if (GameFacade.Game.Window == null) return;

            var mousePosition = state.MouseState.Position;
            var bounds = GameFacade.Game.Window.ClientBounds;
            state.MouseOverWindow = mousePosition.X > 0 && mousePosition.Y > 0 &&
                                    mousePosition.X < bounds.Width && mousePosition.Y < bounds.Height;
            if (FSOEnvironment.SoftwareKeyboard) state.MouseOverWindow = true;
            state.WindowFocused = GameFacade.Game.IsActive;

            /** 
             * Handle the mouse events from the previous frame
             * It's important to do this before the update calls because
             * a lot of mouse events will make changes to the UI. If they do,
             * we want the matrices to be recalculated before the draw
             * method and that is done in the update method.
             */
            if (state.ProcessMouseEvents){
                inputManager.HandleMouseEvents(state);
            }
            state.MouseEvents.Clear();

            state.InputManager = inputManager;
            Content.Content.Get()?.Changes.RunResModifications();
            mainUI.Update(state);

            if (state.AltDown && state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Enter))
            {
                GameFacade.GraphicsDeviceManager.ToggleFullScreen();
            }

            lock (m_ExtContainers)
            {
                var extCopy = new List<UIExternalContainer>(m_ExtContainers);
                foreach (var ext in extCopy)
                {
                    lock (ext)
                    {
                        ext.Update(state);
                    }
                }
            }

            /** Process external update handlers **/
            foreach (var item in m_UIProcess)
            {
                item.Update(state);
            }

            Tooltip = state.UIState.Tooltip;
            TooltipProperties = state.UIState.TooltipProperties;
        }

        public void PreDraw(UISpriteBatch SBatch)
        {
            mainUI.PreDraw(SBatch);
        }

        public void Draw(UISpriteBatch SBatch)
        {
            mainUI.Draw(SBatch);

            if (TooltipProperties.UpdateDead) TooltipProperties.Show = false;
            if (Tooltip != null && TooltipProperties.Show) DrawTooltip(SBatch, TooltipProperties.Position, TooltipProperties.Opacity, TooltipProperties.Color);
            TooltipProperties.UpdateDead = true;
        }

        public void DrawTooltip(SpriteBatch batch, Vector2 position, float opacity, Color color)
        {
            TextStyle style = TextStyle.DefaultLabel.Clone();
            var toolScale = FSOEnvironment.DPIScaleFactor; //*zoom scale?
            style.Color = color;
            style.Size = (int)(8 * toolScale);

            var scale = new Vector2(1, 1);
            if (style.Scale != 1.0f)
            {
                scale = new Vector2(scale.X * style.Scale, scale.Y * style.Scale);
            }

            var wrapped = UIUtils.WordWrap(Tooltip, (int)(290*toolScale), style); //tooltip max width should be 300. There is a 5px margin on each side.

            int width = (int)(wrapped.MaxWidth + 10*toolScale);
            int height = (int)(toolScale * 13 * wrapped.Lines.Count + 4); //13 per line + 4.

            position.X = Math.Min(position.X, GlobalSettings.Default.GraphicsWidth*FSOEnvironment.DPIScaleFactor - width);
            position.Y = Math.Max(position.Y, height);

            var whiteRectangle = TextureGenerator.GetPxWhite(batch.GraphicsDevice);

            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, width, height), Color.White*opacity); //note: in XNA4 colours need to be premultiplied

            //border
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, 1, height), color * opacity);
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, width, 1), color * opacity);
            batch.Draw(whiteRectangle, new Rectangle((int)position.X + width, (int)position.Y - height, 1, height), color * opacity);
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y, width, 1), color * opacity);

            position.Y -= height;

            for (int i = 0; i < wrapped.Lines.Count; i++)
            {
                int thisWidth = (int)(style.MeasureString(wrapped.Lines[i]).X);
                var pos = position + new Vector2((width - thisWidth) / 2, 0);
                if (style.VFont != null)
                {
                    batch.End();
                    style.VFont.Draw(batch.GraphicsDevice, wrapped.Lines[i], pos, color * opacity, scale, null);
                    batch.Begin();
                }
                else
                    batch.DrawString(style.SpriteFont, wrapped.Lines[i], pos, color * opacity, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                position.Y += 13 * FSOEnvironment.DPIScaleFactor;
            }
        }

        private List<DialogReference> Dialogs = new List<DialogReference>();
        public void AddDialog(DialogReference dialog)
        {
            //dialogContainer.Add(dialog.Dialog);
            CurrentUIScreen.Add(dialog.Dialog);
            if(dialog.Controller != null){
                dialog.Dialog.Controller = dialog.Controller;
            }
            if(dialog.LogicalParent != null){
                dialog.Dialog.LogicalParent = dialog.LogicalParent;
            }

            Dialogs.Add(dialog);
            AdjustModal();
        }

        public void RemoveDialog(DialogReference dialog)
        {
            //dialogContainer.Remove(dialog.Dialog);
            if (dialog.Dialog.Parent != null)
            {
                dialog.Dialog.Parent.Remove(dialog.Dialog);
            }
            Dialogs.Remove(dialog);
            AdjustModal();
        }

        public void RemoveDialog(UIElement dialog)
        {
            var reference = Dialogs.FirstOrDefault(x => x.Dialog == dialog);
            if (reference != null)
            {
                Dialogs.Remove(reference);
                dialog.Parent.Remove(reference.Dialog);
                AdjustModal();
            }
        }

        private UIBlocker ModalBlocker = new UIBlocker();
        private void AdjustModal()
        {
            var topMostModal = Dialogs.LastOrDefault(x => x.Modal);
            /** Remove modal blocker **/
            if (ModalBlocker.Parent != null)
            {
                ModalBlocker.Parent.Remove(ModalBlocker);
            }

            if (topMostModal == null)
            {
                
            }
            else
            {
                CurrentUIScreen.AddBefore(ModalBlocker, topMostModal.Dialog);
            }
        }


        #region IGraphicsLayer Members

        public UISpriteBatch SpriteBatch;

        public void PreDraw(GraphicsDevice device)
        {
            lock (m_ExtContainers)
            {
                foreach (var ext in m_ExtContainers)
                {
                    lock (ext)
                    {
                        if (!ext.HasUpdated) ext.Update(null);
                        ext.PreDraw(SpriteBatch);
                        ext.Draw(SpriteBatch);
                    }
                }
            }

            SpriteBatch.UIBegin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
            this.PreDraw(SpriteBatch);
            try { SpriteBatch.End(); } catch { }
        }

        public void Draw(GraphicsDevice device)
        {

            SpriteBatch.UIBegin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
            this.Draw(SpriteBatch);
            SpriteBatch.End();
        }

        #endregion

        #region IGraphicsLayer Members

        public void Initialize(GraphicsDevice device)
        {
        }

        #endregion
    }

    public delegate void UpdateHookDelegate(UpdateState state);

    public class DialogReference
    {
        public UIElement Dialog;
        public bool Modal;
        public object Controller;
        public UIContainer LogicalParent;
    }
}
