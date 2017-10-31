using FSO.Common.Utils;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    public class SkyDomeComponent
    {
        private VertexBuffer Verts;
        private IndexBuffer Indices;
        private int PrimCount;

        public SkyDomeComponent(GraphicsDevice GD)
        {
            //generate sky dome geometry
            var subdivs = 64;

            List<VertexPositionColor> verts = new List<VertexPositionColor>();
            var indices = new List<int>();
            var skyCol = new Color(0x00, 0x80, 0xFF, 0xFF);

            verts.Add(new VertexPositionColor(new Vector3(0, 1, 0), skyCol));

            int vertLastStart = 0;
            int vertLastLength = 1;
            for (int y = 1; y < subdivs; y++)
            {
                int start = verts.Count;
                var angley = (float)Math.PI * y/((float)subdivs);
                var radius = (float)Math.Sin(angley);
                var height = Math.Cos(angley);
                for (int x = 0; x < subdivs+1; x++)
                {
                    var anglex = (float)Math.PI * x*2/(float)subdivs;
                    var colLerp = Math.Min(1, Math.Abs(((y-2) / (float)subdivs) - 0.5f) * 4);
                    verts.Add(new VertexPositionColor(new Vector3((float)Math.Sin(anglex)*radius, (float)height, (float)Math.Cos(anglex)*radius), Color.Lerp(Color.White, skyCol, colLerp)));
                    if (x < subdivs)
                    {
                        if (y != 1)
                        {
                            indices.Add(vertLastStart + x % vertLastLength);
                            indices.Add(vertLastStart + (x + 1) % vertLastLength);
                            indices.Add(verts.Count - 1);
                        }

                        indices.Add(vertLastStart + (x + 1) % vertLastLength);
                        indices.Add(verts.Count);
                        indices.Add(verts.Count - 1);
                    }
                }
                vertLastStart = start;
                vertLastLength = subdivs+1;
            }

            Verts = new VertexBuffer(GD, typeof(VertexPositionColor), verts.Count, BufferUsage.None);
            Verts.SetData(verts.ToArray());
            Indices = new IndexBuffer(GD, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            Indices.SetData(indices.ToArray());
            PrimCount = indices.Count / 3;
        }

        public Vector4 FogColor;

        public void Draw(GraphicsDevice gd, WorldState state)
        {
            var ocolor = state.OutsideColor.ToVector4();
            var effect = WorldContent.GetBE(gd);

            var color = ocolor - new Vector4(0.35f) * 1.5f + new Vector4(0.35f);
            color.W = 1;
            FogColor = color * new Color(0x80, 0xC0, 0xFF, 0xFF).ToVector4();
            effect.LightingEnabled = false;
            effect.DiffuseColor = new Vector3(Math.Min(1, color.X), Math.Min(1, color.Y), Math.Min(1, color.Z));
            //effect.AmbientLightColor = new Vector3(color.X, color.Y, color.Z);
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;

            var view = state.Camera.View;
            view.M41 = 0; view.M42 = 0; view.M43 = 0;
            effect.View = view;
            effect.Projection = (state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection;
            effect.World = Matrix.CreateScale(5f);
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.Opaque;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.Indices = Indices;
                gd.SetVertexBuffer(Verts);

                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, PrimCount);
            }

            gd.BlendState = BlendState.NonPremultiplied;
            var night = state.Light?.Night ?? false;
            //draw the sun or moon
            var pos = state.Light?.SunVector ?? new Vector3(0, 1, 0);
            var z = -pos.X;
            pos.X = pos.Z;
            pos.Z = z;
            var dist = 0.5f + pos.Y * 2;
            dist *= dist;
            dist += 0.5f;
            if (night) dist = 35;
            var sunMat = Matrix.CreateTranslation(0, 0, dist) * Matrix.CreateBillboard(pos, new Vector3(0, 0.4f, 0), Vector3.Up, null);

            var geom = WorldContent.GetTextureVerts(gd);
            effect.World = sunMat;
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = true;
            effect.Texture = (night)? TextureGenerator.GetMoon(gd) : TextureGenerator.GetSun(gd);
            effect.DiffuseColor = new Vector3(color.X, color.Y, color.Z) * 2f;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.SetVertexBuffer(geom);
                gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            gd.DepthStencilState = DepthStencilState.Default;
        }
    }
}
