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

namespace FSO.LotView.RC
{
    public class DGRPRendererRC : DGRPRenderer
    {
        private DGRP3DMesh Mesh;
        public OBJD Source;
        public Matrix World;

        public DGRPRendererRC(DGRP group, OBJD source) : base(group)
        {
            Source = source;
        }

        public BoundingBox? GetBounds()
        {
            if (_Dirty && DrawGroup != null)
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty = false;
            }
            return Mesh?.Bounds;
        }

        public override void Draw(WorldState world)
        {
            if (DrawGroup == null) return;
            if (_Dirty)
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty = false;
            }

            //immedately draw the mesh.
            var device = world.Device;
            var effect = WorldContent.RCObject;

            if (Room == 65533) effect.CurrentTechnique = effect.Techniques["DisabledDraw"];

            effect.Parameters["World"].SetValue(World);
            effect.Parameters["Level"].SetValue((float)(Level-0.999f));

            int i = 0;
            foreach (var spr in Mesh.Geoms)
            {
                if (i == 0 || (((i-1) > 63) ? ((DynamicSpriteFlags2 & ((ulong)0x1 << ((i-1) - 64))) > 0) :
                    ((DynamicSpriteFlags & ((ulong)0x1 << (i-1))) > 0))) { 
                    foreach (var geom in spr.Values)
                    {
                        if (geom.PrimCount == 0) continue;
                        effect.Parameters["MeshTex"].SetValue(geom.Pixel);
                        var info = geom.Pixel?.Tag as TextureInfo;
                        effect.Parameters["UVScale"].SetValue(info?.UVScale ?? Vector2.One);
                        var pass = effect.CurrentTechnique.Passes[(WorldConfig.Current.Directional && Room < 65533) ? 1:0];
                        pass.Apply();
                        if (!geom.Rendered) continue;
                        device.Indices = geom.Indices;
                        device.SetVertexBuffer(geom.Verts);

                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);
                    }
                }
                i++;
            }

            if (Room == 65533) effect.CurrentTechnique = effect.Techniques["Draw"];
        }

        public void DrawLMap(GraphicsDevice device, sbyte level)
        {
            if (DrawGroup == null) return;
            if (_Dirty)
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty = false;
            }

            //immedately draw the mesh.
            var effect = WorldContent.RCObject;

            var mat = World;
            mat.M42 = ((Level-level) -1)*2.95f; //set y translation to 0
            effect.Parameters["World"].SetValue(mat);

            int i = 0;
            foreach (var spr in Mesh.Geoms)
            {
                if (i == 0 || (((i - 1) > 63) ? ((DynamicSpriteFlags2 & ((ulong)0x1 << ((i - 1) - 64))) > 0) :
                    ((DynamicSpriteFlags & ((ulong)0x1 << (i - 1))) > 0)))
                {
                    foreach (var geom in spr.Values)
                    {
                        if (geom.PrimCount == 0) continue;
                        foreach (var pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            if (!geom.Rendered) continue;
                            device.Indices = geom.Indices;
                            device.SetVertexBuffer(geom.Verts);

                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geom.PrimCount);
                        }
                    }
                }
                i++;
            }
        }
    }
}
