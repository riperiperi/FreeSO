using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils
{
    public static class SSAADownsample
    {
        public static void Draw(GraphicsDevice gd, RenderTarget2D targ)
        {
            var effect = WorldContent.SSAA;
            gd.BlendState = BlendState.Opaque;
            effect.CurrentTechnique = effect.Techniques[0];
            effect.Parameters["SSAASize"].SetValue(new Vector2(1f / targ.Width, 1f / targ.Height));
            effect.Parameters["tex"].SetValue(targ);
            effect.CurrentTechnique.Passes[0].Apply();

            gd.SetVertexBuffer(WorldContent.GetTextureVerts(gd));
            gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }
    }
}
