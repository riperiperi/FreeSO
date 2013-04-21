using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    public class UISpriteBatch : SpriteBatch
    {
        public UISpriteBatch(GraphicsDevice gd)
            : base(gd)
        {
        }

        private SpriteBlendMode _BlendMode;
        private SpriteSortMode _SortMode;
        private SaveStateMode _SaveStateMode;


        public void UIBegin(SpriteBlendMode blendMode, SpriteSortMode sortMode, SaveStateMode stateMode)
        {
            this._BlendMode = blendMode;
            this._SortMode = sortMode;
            this._SaveStateMode = stateMode;

            this.Begin(blendMode, sortMode, stateMode);
        }


        public void Pause()
        {
            this.End();
        }

        public void Resume()
        {
            this.Begin(_BlendMode, _SortMode, _SaveStateMode);
        }



    }
}
