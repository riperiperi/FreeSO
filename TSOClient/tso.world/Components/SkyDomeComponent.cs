using FSO.Common;
using FSO.Common.Utils;
using FSO.Files;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    public class SkyDomeComponent : IDisposable
    {
        private VertexBuffer Verts;
        private IndexBuffer Indices;
        private Texture2D GradTex;
        private int PrimCount;
        private Blueprint BP;
        private float LastSkyPos;

        public SkyDomeComponent(GraphicsDevice GD, Blueprint bp)
        {
            using (var file = File.OpenRead(Path.Combine(FSOEnvironment.ContentDir, "Textures/skycol.png")))
            {
                GradTex = ImageLoader.FromStream(GD, file);
            };

            BP = bp;
            BuildSkyDome(GD);
        }

        public void BuildSkyDome(GraphicsDevice GD)
        {
            //generate sky dome geometry
            var subdivs = 65;
            List<VertexPositionTexture> verts = new List<VertexPositionTexture>();
            var indices = new List<int>();
            var skyCol = new Color(0x00, 0x80, 0xFF, 0xFF);

            float skyPos = BP.OutsideSkyP;
            verts.Add(new VertexPositionTexture(new Vector3(0, 1, 0), new Vector2(skyPos, 0f)));
            LastSkyPos = skyPos;
            skyPos += 1 / 16f;
            int vertLastStart = 0;
            int vertLastLength = 1;
            for (int y = 1; y < subdivs; y++)
            {
                int start = verts.Count;
                var angley = (float)Math.PI * y / ((float)subdivs - 1);
                var radius = (float)Math.Sin(angley);
                var height = Math.Cos(angley);
                //var aheight = (height < -0.6f)?((-0.12f) - height):height;
                //var tpos = (0.9f - (float)Math.Sqrt(Math.Abs(aheight)) * 0.9f);
                var tpos = (float)((height > 0) ? (0.9f - Math.Sqrt(height) * 0.9f) : 0.9f + Math.Sqrt(-height) * 0.1f);

                for (int x = 0; x < subdivs + 1; x++)
                {
                    var anglex = (float)Math.PI * x * 2 / (float)subdivs;
                    var colLerp = Math.Min(1, Math.Abs(((y - 2) / (float)subdivs) - 0.60f) * 4);
                    verts.Add(new VertexPositionTexture(new Vector3((float)Math.Sin(anglex) * radius, (float)height, (float)Math.Cos(anglex) * radius), new Vector2(skyPos, tpos)));
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
                vertLastLength = subdivs + 1;
            }

            if (Verts == null) Verts = new VertexBuffer(GD, typeof(VertexPositionTexture), verts.Count, BufferUsage.None);
            Verts.SetData(verts.ToArray());
            if (Indices == null)
            {
                Indices = new IndexBuffer(GD, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
                Indices.SetData(indices.ToArray());
                PrimCount = indices.Count / 3;
            }
        }

        public Vector4 FogColor;

        public void Draw(GraphicsDevice gd, WorldState state)
        {

            gd.Clear(state.OutsideColor);
            var ocolor = state.OutsideColor.ToVector4();
            var effect = WorldContent.GetBE(gd);

            if (LastSkyPos != BP.OutsideSkyP) BuildSkyDome(gd);

            var color = ocolor - new Vector4(0.35f) * 1.5f + new Vector4(0.35f);
            color.W = 1;
            var wint = BP.Weather.WeatherIntensity;

            effect.LightingEnabled = false;
            effect.Texture = GradTex;
            effect.Alpha = (1 - (float)Math.Sqrt(wint) * 0.75f);
            effect.DiffuseColor = Vector3.One;
            effect.AmbientLightColor = Vector3.One;
            //effect.DiffuseColor = new Vector3(Math.Min(1, color.X), Math.Min(1, color.Y), Math.Min(1, color.Z));
            //effect.AmbientLightColor = new Vector3(color.X, color.Y, color.Z);
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = true;

            var view = state.Camera.View;
            view.M41 = 0; view.M42 = 0; view.M43 = 0;
            effect.View = view;
            effect.Projection = (state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection;
            effect.World = Matrix.CreateScale(5f);
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.AlphaBlend;
            gd.SamplerStates[0] = SamplerState.LinearWrap;

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
            effect.DiffuseColor = new Vector3(color.X, color.Y, color.Z) * ((night) ? 2f : 0.6f);
            gd.BlendState = (night)? BlendState.NonPremultiplied:BlendState.Additive;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.SetVertexBuffer(geom);
                gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            gd.BlendState = BlendState.NonPremultiplied;
            gd.DepthStencilState = DepthStencilState.Default;
            effect.Alpha = 1f;
        }

        public void Dispose()
        {
            Verts?.Dispose();
            Indices?.Dispose();
            GradTex?.Dispose();
        }
    }
}
