/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Lot;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSOClient.Code;

namespace TSOClient
{
    public class ScreenManager
    {
        private Game m_G;
        private ArrayList m_Screens = new ArrayList();
        private SpriteFont m_SprFontBig;
        private SpriteFont m_SprFontSmall;

        //For displaying 3D objects (sims).
        private Matrix m_WorldMatrix, m_ViewMatrix, m_ProjectionMatrix;
        private Dictionary<int, string> m_TextDict;


        /// <summary>
        /// Top most UI container
        /// </summary>
        private UIContainer mainUI;
        private UIButton debugButton;
        private InputManager inputManager;


        public Game GameComponent
        {
            get { return m_G; }
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
        /// A spritefont used to display big text.
        /// </summary>
        public SpriteFont SprFontBig
        {
            get { return m_SprFontBig; }
        }

        /// <summary>
        /// A spritefont used to display small text.
        /// </summary>
        public SpriteFont SprFontSmall
        {
            get { return m_SprFontSmall; }
        }

        /// <summary>
        /// The UIScreen instance that is currently being 
        /// updated and rendered by this ScreenManager instance.
        /// </summary>
        public UIScreen CurrentUIScreen
        {
            get
            {
                return m_Screens.OfType<UIScreen>().First();
            }
        }

        /// <summary>
        /// The LotScreen instance that is currently being 
        /// updated and rendered by this ScreenManager instance.
        /// </summary>
        //public LotScreen CurrentLotScreen
        //{
        //    get
        //    {
        //        return m_Screens.OfType<LotScreen>().First();
        //    }
        //}

        /// <summary>
        /// Gets or sets the internal dictionary containing all the strings for the game.
        /// </summary>
        public Dictionary<int, string> TextDict
        {
            get { return m_TextDict; }
            set { m_TextDict = value; }
        }

        public ScreenManager(Game G, SpriteFont SprFontBig, SpriteFont SprFontSmall)
        {
            m_G = G;
            m_SprFontBig = SprFontBig;
            m_SprFontSmall = SprFontSmall;

            GraphicsDevice.VertexDeclaration = new VertexDeclaration(GraphicsDevice, 
                VertexPositionNormalTexture.VertexElements);
            GraphicsDevice.RenderState.CullMode = CullMode.None;

            m_WorldMatrix = Matrix.Identity;
            m_ViewMatrix = Matrix.CreateLookAt(Vector3.Right * 5, Vector3.Zero, Vector3.Forward);
            m_ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)GraphicsDevice.PresentationParameters.BackBufferWidth / 
                    (float)GraphicsDevice.PresentationParameters.BackBufferHeight,
                    1.0f, 100.0f);

            TextStyle.DefaultTitle = new TextStyle {
                Font = GameFacade.MainFont,
                Size = 10,
                Color = new Color(255,249,157)
            };

            TextStyle.DefaultButton = new TextStyle
            {
                Font = GameFacade.MainFont,
                Size = 10,
                Color = new Color(255, 249, 157)
            };

            TextStyle.DefaultLabel = new TextStyle
            {
                Font = GameFacade.MainFont,
                Size = 10,
                Color = new Color(255, 249, 157)
            };

            inputManager = new InputManager();
            mainUI = new UIContainer();
            GameFacade.OnContentLoaderReady += new BasicEventHandler(GameFacade_OnContentLoaderReady);
        }

        void GameFacade_OnContentLoaderReady()
        {
            /**
             * Add a debug button once the content loader is ready so we can load textures
             */
            debugButton = new UIButton()
            {
                Caption = "Debug",
                Y = 10,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            debugButton.OnButtonClick += new ButtonClickDelegate(debugButton_OnButtonClick);
            mainUI.Add(debugButton);
        }

        void debugButton_OnButtonClick(UIButton button)
        {
            GameFacade.Controller.StartDebugTools();
        }

        /// <summary>
        /// Adds a UIScreen instance to this ScreenManager's list of screens.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="Screen">The UIScreen instance to be added.</param>
        public void AddScreen(UIScreen Screen)
        {
            mainUI.AddAt(0, Screen);
            m_Screens.Add(Screen);
        }

        public void RemoveScreen(UIScreen Screen)
        {
            mainUI.Remove(Screen);
            m_Screens.Remove(Screen);
        }

        /// <summary>
        /// Runs the Lua function that creates the initial UIScreen.
        /// </summary>
        /// <param name="Path">The path to the Lua script containing the function.</param>
        public void LoadInitialScreen(string Path)
        {
            LuaInterfaceManager.RunFileInThread(Path);
        }

        public void Update(UpdateState state)
        {
            //IEnumerable<GameScreen> Screens = m_Screens.OfType<GameScreen>();
            //List<GameScreen> ScreenList = Screens.ToList<GameScreen>();
            state.InputManager = inputManager;
            state.MouseEvents.Clear();
            mainUI.Update(state);
            inputManager.HandleMouseEvents(state);

            //for (int i = 0; i < ScreenList.Count; i++)
            //    ScreenList[i].Update(state);
        }

        public void Draw(SpriteBatch SBatch, float FPS)
        {
            //IEnumerable<GameScreen> Screens = m_Screens.OfType<GameScreen>();
            //List<GameScreen> ScreenList = Screens.ToList<GameScreen>();

            mainUI.Draw(SBatch);

            //for (int i = 0; i < ScreenList.Count; i++)
            //    ScreenList[i].Draw(SBatch);

            SBatch.DrawString(m_SprFontBig, "FPS: " + FPS.ToString(), new Vector2(0, 0), Color.Red);
        }
    }
}
