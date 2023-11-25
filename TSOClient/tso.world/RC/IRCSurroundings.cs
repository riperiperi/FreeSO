using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.RC
{
    public interface IRCSurroundings
    {
        void DrawSurrounding(GraphicsDevice gfx, ICamera cam, Vector4 fogColor, int surroundNumber);
    };
}
