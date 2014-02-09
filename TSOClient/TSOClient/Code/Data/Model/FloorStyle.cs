using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.IFF;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using tso.common.utils;

namespace TSOClient.Code.Data.Model
{
    public class FloorStyle
    {
        public int ID;

        public SpriteFrame CloseTexture;
        public SpriteFrame MediumTexture;
        public SpriteFrame FarTexture;

        private Dictionary<HouseRotation, FloorStyle> AltViews;

        public void AddAltView(HouseRotation rotation, FloorStyle alt)
        {
            if (AltViews == null) { AltViews = new Dictionary<HouseRotation, FloorStyle>(); }
            AltViews.Add(rotation, alt);
        }


        private Texture2D _CloseTexture;
        private Texture2D _MediumTexture;
        private Texture2D _FarTexture;

        /// <summary>
        /// Some tiles, (e.g. 9 & 10) have different states depending on the rotation
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public Texture2D GetTexture(HouseZoom zoom, HouseRotation rotation)
        {
            if (AltViews != null && AltViews.ContainsKey(rotation))
            {
                return AltViews[rotation].GetTexture(zoom, rotation);
            }

            switch (zoom)
            {
                case HouseZoom.CloseZoom:
                    if (_CloseTexture == null)
                    {
                        _CloseTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, CloseTexture);
                    }
                    return _CloseTexture;

                case HouseZoom.MediumZoom:
                    if (_MediumTexture == null)
                    {
                        _MediumTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, MediumTexture);
                    }
                    return _MediumTexture;

                case HouseZoom.FarZoom:
                    if (_FarTexture == null)
                    {
                        _FarTexture = TextureUtils.FromSpriteFrame(GameFacade.GraphicsDevice, FarTexture);
                    }
                    return _FarTexture;
            }

            return null;
        }

        public string Name;
        public string Description;
        public string Price;
    }
}
