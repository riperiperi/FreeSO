using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using tso.common.utils;

namespace TSOClient.Code.Data.Model
{
    public class WallStyle
    {
        public int ID;

        public SPRParser SpriteClose;
        public SPRParser SpriteMedium;
        public SPRParser SpriteFar;

        private WallStyleGroup _Close;
        public WallStyleGroup Close
        {
            get
            {
                if (_Close == null)
                {
                    _Close = new WallStyleGroup(SpriteClose);
                }
                return _Close;
            }
        }


        private WallStyleGroup _Far;
        public WallStyleGroup Far
        {
            get
            {
                if (_Far == null)
                {
                    _Far = new WallStyleGroup(SpriteFar);
                }
                return _Far;
            }
        }


    }

    public class WallStyleGroup
    {
        public SpriteFrame LeftFrame;
        public SpriteFrame RightFrame;
        public SpriteFrame DiagFrame;
        public SpriteFrame EndFrame;

        private Texture2D _LeftTexture;
        private Texture2D _RightTexture;
        private Texture2D _DiagTexture;
        private Texture2D _EndTexture;


        public WallStyleGroup(SPRParser sprite)
        {
            LeftFrame = sprite.GetFrame(0);
            RightFrame = sprite.GetFrame(1);
            DiagFrame = sprite.GetFrame(2);
            EndFrame = sprite.GetFrame(3);
        }

        public Texture2D LeftTexture
        {
            get {
                if (_LeftTexture == null)
                {
                    _LeftTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, LeftFrame);
                }
                return _LeftTexture; 
            }
        }

        public Texture2D RightTexture
        {
            get
            {
                if (_RightTexture == null)
                {
                    _RightTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, RightFrame);
                }
                return _RightTexture;
            }
        }

        public Texture2D DiagTexture
        {
            get
            {
                if (_DiagTexture == null)
                {
                    _DiagTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, DiagFrame);
                }
                return _DiagTexture;
            }
        }

        public Texture2D EndTexture
        {
            get
            {
                if (_EndTexture == null)
                {
                    _EndTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, EndFrame);
                }
                return _EndTexture;
            }
        }
    }

    public enum WallSegment
    {
        Left = 1,
        Right = 2,
        Diag = 3,
        End = 4
    }
}
