using FSO.LotView.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering;
using FSO.Common.Utils;
using FSO.LotView.Effects;
using FSO.LotView.Model;

namespace FSO.LotView.RC
{
    public class DGRPRendererRC : DGRPRenderer
    {
        private DGRP3DMesh Mesh;

        public DGRPRendererRC(DGRP group, OBJD source) : base(group, source)
        {

        }

        public override void Draw(WorldState world)
        {
            if (DrawGroup == null) return;
            if (_Dirty.HasFlag(ComponentRenderMode._3D))
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty &= ~ComponentRenderMode._3D;
            }

            //immedately draw the mesh.
            var device = world.Device;
            var effect = WorldContent.RCObject;

            effect.World = World;
            effect.Level = (float)(Level-0.999f);
            var advDir = WorldConfig.Current.Directional && WorldConfig.Current.AdvancedLighting;

            if (Mesh.DepthMask != null)
            {
                var geom = Mesh.DepthMask;
                //depth mask for drawing into a surface or wall
                if (geom.Verts != null)
                {
                    effect.SetTechnique(RCObjectTechniques.DepthClear);
                    effect.CurrentTechnique.Passes[0].Apply();

                    device.DepthStencilState = DepthClear1;
                    device.Indices = geom.Indices;
                    device.SetVertexBuffer(geom.Verts);
                    
                    device.BlendState = NoColor;
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);

                    device.DepthStencilState = (Mesh.MaskType == DGRP3DMaskType.Portal) ? DepthClear2Strict : DepthClear2;
                    effect.CurrentTechnique.Passes[1].Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);

                    device.DepthStencilState = DepthStencilState.Default;
                    device.BlendState = BlendState.NonPremultiplied;
                    effect.SetTechnique(RCObjectTechniques.Draw);
                }
            }

            if (Room == 65533) effect.SetTechnique(RCObjectTechniques.DisabledDraw);

            int i = 0;
            foreach (var spr in Mesh.Geoms)
            {
                if (i == 0 || (((i-1) > 63) ? ((DynamicSpriteFlags2 & ((ulong)0x1 << ((i-1) - 64))) > 0) :
                    ((DynamicSpriteFlags & ((ulong)0x1 << (i-1))) > 0)) || (Mesh.MaskType == DGRP3DMaskType.Portal && i == Mesh.Geoms.Count - 1)) { 
                    foreach (var geom in spr.Values)
                    {
                        if (geom.PrimCount == 0) continue;
                        if (Mesh.MaskType == DGRP3DMaskType.Portal && i == Mesh.Geoms.Count - 1)
                            device.DepthStencilState = Portal;
                        effect.MeshTex = geom.Pixel;
                        var info = geom.Pixel?.Tag as TextureInfo;
                        effect.UVScale = info?.UVScale ?? Vector2.One;
                        var pass = effect.CurrentTechnique.Passes[(advDir && Room < 65533) ? 1:0];
                        pass.Apply();
                        if (geom.Rendered)
                        {
                            device.Indices = geom.Indices;
                            device.SetVertexBuffer(geom.Verts);

                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);
                        }
                        if (Mesh.MaskType == DGRP3DMaskType.Portal && i == Mesh.Geoms.Count - 1)
                            device.DepthStencilState = DepthStencilState.Default;
                    }
                }
                i++;
            }

            if (Mesh.MaskType == DGRP3DMaskType.Portal)
            {
                var geom = Mesh.DepthMask;
                //clear the stencil, so it doesn't interfere with future portals.
                if (geom.Verts != null)
                {
                    effect.SetTechnique(RCObjectTechniques.DepthClear);
                    effect.CurrentTechnique.Passes[1].Apply();

                    device.DepthStencilState = StencilClearOnly;
                    device.Indices = geom.Indices;
                    device.SetVertexBuffer(geom.Verts);

                    device.BlendState = NoColor;
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);
                    device.BlendState = BlendState.NonPremultiplied;
                }
                device.DepthStencilState = DepthStencilState.Default;
                effect.SetTechnique(RCObjectTechniques.Draw);
            }
            if (Room == 65533) effect.SetTechnique(RCObjectTechniques.Draw);
        }
    }
}
