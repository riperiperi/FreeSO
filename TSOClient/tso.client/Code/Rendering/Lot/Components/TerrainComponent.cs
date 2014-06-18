using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using TSOClient.Code.Rendering.Lot.Model;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public class TerrainComponent : House3DComponent
    {
        private VertexPositionTexture[] Geom;
        private Texture2D Texture;
        private BasicEffect Effect;

        public TerrainComponent(HouseRenderState state)
        {
            var textureBase = GameFacade.GameFilePath("gamedata/terrain/newformat/");
            var grass = Texture2D.FromFile(GameFacade.GraphicsDevice, Path.Combine(textureBase, "gr.tga"));
            Texture = grass;

            Effect = new BasicEffect(GameFacade.GraphicsDevice, null);
            Effect.TextureEnabled = true;
            Effect.Texture = Texture;

            Geom = new VertexPositionTexture[4];

            var repeatX = state.Size / 2.5f;
            var repeatY = repeatX;

            var tl = state.GetWorldFromTile(new Vector2(1, 1));
            var tr = state.GetWorldFromTile(new Vector2(state.Size-1, 1));
            var bl = state.GetWorldFromTile(new Vector2(1, state.Size-1));
            var br = state.GetWorldFromTile(new Vector2(state.Size-1, state.Size-1));

            Geom[0] = new VertexPositionTexture(tl, new Vector2(0, 0));
            Geom[1] = new VertexPositionTexture(tr, new Vector2(repeatX, 0));
            Geom[2] = new VertexPositionTexture(br, new Vector2(repeatX, repeatY));
            Geom[3] = new VertexPositionTexture(bl, new Vector2(0, repeatY));
        }



        public override void Draw(GraphicsDevice device, HouseRenderState state)
        {
            Effect.World = state.World;
            Effect.View = state.Camera.View;
            Effect.Projection = state.Camera.Projection;
            
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            device.VertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            Effect.Begin();
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleFan, Geom, 0, 2);
                pass.End();
            }
            Effect.End();
        }
    }
}
