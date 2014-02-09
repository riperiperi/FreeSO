using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework;
using tso.vitaboy;
using Microsoft.Xna.Framework.Graphics;

namespace tso.debug.content.preview
{
    public class MeshPreviewComponent : _3DComponent
    {
        public Mesh Mesh;
        public Texture2D Texture;

        public override void Update(tso.common.rendering.framework.model.UpdateState state)
        {
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            if (Mesh == null) { return; }

            var effect = new BasicEffect(device, null);
            effect.World = World;
            effect.View = View;
            effect.Projection = Projection;
            effect.Begin();
            if (Texture != null)
            {
                effect.TextureEnabled = true;
                effect.Texture = Texture;
            }

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                Mesh.Draw(device);
                pass.End();
            }
            effect.End();

        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            throw new NotImplementedException();
        }
    }
}
