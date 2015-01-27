using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Data;
using TSOClient.Code.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Data.Model;
using TSOClient.Code.Rendering.Lot.Model;
using TSOClient.Code.Rendering.Lot.Framework;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public class FloorComponent : House2DComponent
    {
        public int Level;
        public int FloorStyle;

        private Texture2D Texture;
        private Rectangle PaintCoords;

        private bool m_Dirty = true;
        private bool m_Active = true;

        public FloorComponent()
        {
        }

        public override void OnRotationChanged(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
            m_Dirty = true;
        }

        public override void OnZoomChanged(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
            m_Dirty = true;
        }

        public override void OnScrollChange(HouseRenderState state)
        {
            m_Dirty = true;
        }


        /// <summary>
        /// Floors only occupy their own tile
        /// </summary>
        public override int Height
        {
            get { return 0; }
        }

        public override void Draw(HouseRenderState state, HouseBatch batch)
        {
            if (!m_Active) { return; }

            if (m_Dirty)
            {
                if (FloorStyle == 0) { m_Active = false; return; }

                var floorStyle = ArchitectureCatalog.GetFloor(FloorStyle);
                if (floorStyle == null) { m_Active = false; return; }


                var position = state.TileToScreen(Position);
                PaintCoords = new Rectangle((int)position.X, (int)position.Y, state.CellWidth, state.CellHeight);
                Texture = floorStyle.GetTexture(state.Zoom, state.Rotation);
                m_Dirty = false;
            }



            batch.Draw(new HouseBatchSprite {
                Pixel = Texture,
                DestRect = PaintCoords,
                SrcRect = new Rectangle(0, 0, Texture.Width, Texture.Height),
                RenderMode = HouseBatchRenderMode.NO_DEPTH
            });
            //batch.Draw(Texture, PaintCoords, Color.White);
        }
    }
}
