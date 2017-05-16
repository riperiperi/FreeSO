using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Framework;

namespace FSO.Client.Rendering.City.Plugins
{
    public abstract class AbstractCityPlugin
    {
        protected Terrain City;
        public AbstractCityPlugin(Terrain city)
        {
            City = city;
        }

        public abstract void TileHover(Vector2? tile);

        public abstract void TileMouseDown(Vector2 tile);

        public abstract void TileMouseUp(Vector2? tile);

        public abstract void Update(UpdateState state);

        public abstract void Draw(SpriteBatch sb);
    }
}
