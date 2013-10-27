using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using TSOClient.Code.Data;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public class WallComponent : House2DComponent
    {
        private HouseDataWall WallInfo;

        public WallComponent(HouseDataWall wallInfo)
        {
            this.WallInfo = wallInfo;
        }

        //public override void OnStateChanged(HouseRenderState state)
        //{
        //    base.OnStateChanged(state);

        //    /** Change texture pointers **/

        //}

        public override int Height{
            get { return 0;  }
        }

        public override void Draw(HouseRenderState state, HouseBatch batch)
        {
            var position = state.TileToScreen(Position);

            

            if ((WallInfo.Segments & WallSegments.BottomLeft) == WallSegments.BottomLeft)
            {
                var wall = ArchitectureCatalog.GetWallPattern(WallInfo.BottomRightPattern);
                if (wall != null)
                {
                    var tx = wall.Far.RightTexture;

                    batch.Draw(tx, new Rectangle((int)position.X, (int)position.Y - 49, 16, 67), Color.White);
                }
            }

            if ((WallInfo.Segments & WallSegments.BottomRight) == WallSegments.BottomRight)
            {
                var wall = ArchitectureCatalog.GetWallPattern(WallInfo.BottomLeftPattern);
                if (wall != null)
                {
                    var tx = wall.Far.LeftTexture;
                    batch.Draw(tx, new Rectangle((int)position.X, (int)position.Y - 58, 16, 67), Color.White);
                }
            }
        }
    }
}
