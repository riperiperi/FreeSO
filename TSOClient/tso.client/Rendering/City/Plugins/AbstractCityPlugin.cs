using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;

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
