/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Debug.content.preview
{
    public class MeshPreviewComponent : _3DComponent
    {
        public Mesh Mesh;
        public Texture2D Texture;

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
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
