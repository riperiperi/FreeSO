using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Common.Rendering.Framework
{
    public interface IGraphicsLayer
    {
        void Initialize(GraphicsDevice device);
        void Update(UpdateState state);
        void PreDraw(GraphicsDevice device);
        void Draw(GraphicsDevice device);
    }
}
