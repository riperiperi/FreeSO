using FSO.Common.Rendering.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Utils;
using FSO.Files.RC;

namespace FSO.LotView.Debug
{
    public class Debug3DDGRPComponent : _3DComponent
    {
        public DGRP3DMesh Mesh;
        public BasicEffect Effect;
        public bool Wireframe;

        public override void DeviceReset(GraphicsDevice Device)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            Effect = new BasicEffect(Device);
        }

        public override void Draw(GraphicsDevice device)
        {
            if (Mesh == null) return;
            Effect.World = World;
            Effect.View = View;
            Effect.Projection = Projection;
            Effect.TextureEnabled = true;
            Effect.LightingEnabled = false;

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.FillMode = Wireframe?FillMode.WireFrame:FillMode.Solid;
            rasterizerState.CullMode = CullMode.None;
            device.RasterizerState = rasterizerState;

            var gs = Mesh.Geoms.FirstOrDefault();
            if (gs == null) return;
            foreach (var geom in gs.Values)
            {
                if (geom.PrimCount == 0) continue;
                Effect.Texture = geom.Pixel;
                foreach (var pass in Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    if (!geom.Rendered) continue;
                    device.Indices = geom.Indices;
                    device.SetVertexBuffer(geom.Verts);

                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);
                }
            }
        }

        public override void Update(UpdateState state)
        {
        }

        public void Dispose()
        {
            //since these are loaded from the cache we can't dispose them
            //they could be being used ingame.
            /*
            if (Mesh != null)
            {
                foreach (var geom in Mesh.Geoms)
                {
                    foreach (var e in geom.Values) e.Dispose();
                }
            }
            */
        }
    }
}
