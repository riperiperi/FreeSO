using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;

namespace TSOClient
{
    public abstract class GameScreen
    {
        protected ScreenManager m_ScreenMgr;
        private List<UIElement> m_UIElements = new List<UIElement>();

        /// <summary>
        /// The screenmanager that controls this screen.
        /// </summary>
        public ScreenManager ScreenMgr
        {
            get { return m_ScreenMgr; }
        }

        public GameScreen(ScreenManager ScreenMgr)
        {
            m_ScreenMgr = ScreenMgr;
        }

        /// <summary>
        /// Updates the logic for this screen.
        /// </summary>
        /// <param name="GTime">The current gametime.</param>
        public virtual void Update(GameTime GTime)
        {
        }

        /// <summary>
        /// Draws the screen.
        /// </summary>
        /// <param name="SBatch">The SpriteBatch to draw with.</param>
        public virtual void Draw(SpriteBatch SBatch)
        {
        }

        public virtual void RemoveElement(string ID)
        {

        }
    }
}
