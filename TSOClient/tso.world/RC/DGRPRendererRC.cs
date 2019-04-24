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

        //depth clear mask
        //how it works:
        //pass 1: draw mask to stencil 1 with normal depth rules. no depth write.
        //pass 2: draw mask where stencil 1 exists with max far depth. depth write override, stencil clear.
        //pass 3: draw object normally

        public static DepthStencilState DepthClear1 = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,

            TwoSidedStencilMode = true,

            CounterClockwiseStencilFail = StencilOperation.Keep,
            CounterClockwiseStencilPass = StencilOperation.Replace,
            CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep,

            StencilDepthBufferFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Zero,
            StencilFail = StencilOperation.Keep,

            ReferenceStencil = 1,
            DepthBufferWriteEnable = true
        };

        public static DepthStencilState DepthClear2 = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Zero,
            StencilDepthBufferFail = StencilOperation.Zero,
            ReferenceStencil = 1,
            DepthBufferWriteEnable = true,
            DepthBufferFunction = CompareFunction.Always
        };

        public static DepthStencilState DepthClear2Strict = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferWriteEnable = true,
            DepthBufferFunction = CompareFunction.Always
        };


        public static DepthStencilState Portal = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferWriteEnable = true,
        };

        public static DepthStencilState StencilClearOnly = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Zero,
            StencilDepthBufferFail = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.Always
        };

        public static BlendState NoColor = new BlendState()
        {
            ColorWriteChannels = ColorWriteChannels.None
        };

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

        public void DrawLMap(GraphicsDevice device, sbyte level, float yOff)
        {
            if (DrawGroup == null) return;
            if (_Dirty)
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty = false;
            }

            if (Mesh.MaskType == DGRP3DMaskType.Portal) return;
            //immedately draw the mesh.
            var effect = WorldContent.RCObject;

            var mat = World;
            mat.M42 = ((Level-level) -1)*2.95f + yOff; //set y translation to 0
            effect.World = mat;

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

        public override void Preload(WorldState world)
        {
            if (_Dirty)
            {
                Mesh = Content.Content.Get().RCMeshes.Get(DrawGroup, Source);
                _Dirty = false;
            }
        }
    }
}
