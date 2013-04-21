using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    public abstract class GameScreen : UIScreen
    {
        private bool m_Scale800x600;

        public bool Scale800x600
        {
            get { return m_Scale800x600; }
            set
            {
                m_Scale800x600 = value;
                if (value)
                {
                    ScaleX = ScaleY = ScreenWidth / 800.0f;
                }
                else
                {
                    ScaleX = ScaleY = 1.0f;
                }
            }
        }


        public override Rectangle GetBounds()
        {
            if (m_Scale800x600)
            {
                return new Rectangle(0, 0, 800, 600);
            }
            return base.GetBounds();
        }
    }
}
