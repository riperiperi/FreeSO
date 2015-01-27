using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework;
using TSO.Vitaboy;
using Microsoft.Xna.Framework.Graphics;

namespace tso.debug.content.preview
{
    public class MeshPreviewComponent : _3DComponent
    {
        public Mesh Mesh;
        public Texture2D Texture;

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            if (Mesh == null) { return; }

            var effect = new BasicEffect(device);
            effect.World = World;
            effect.View = View;
            effect.Projection = Projection;
            if (Texture != null)
            {
                effect.TextureEnabled = true;
                effect.Texture = Texture;
            }

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Mesh.Draw(device);
            }
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            throw new NotImplementedException();
        }
    }
}
