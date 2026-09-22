using FSO.Common;
using FSO.Common.Model;
using FSO.Common.Rendering;
using FSO.Common.Utils;
using FSO.Files;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace FSO.LotView.Components
{
    public class AbstractSkyDome : IDisposable
    {
        private const int StarCount = 14397;
        private const int StarSeed = 130654;
        private const float StarSize = 0.002f;
        private static string DefaultSkyCol = "Textures/skycol.png";
        private static string FinalSkyCol = "Textures/skycolfinal.png";

        private VertexBuffer Verts;
        private IndexBuffer Indices;
        private Texture2D GradTex;
        private int PrimCount;
        private float LastSkyPos;

        private VertexPositionTexture[] VertexData;
        private int[] IndexData;

        private static VertexBuffer StarVerts;
        private static IndexBuffer StarInds;
        private static float ActiveStarSpeed;
        private static float ActiveStarMultiplier;

        public float StarSpeed;

        private bool IsFinal;

        public AbstractSkyDome(GraphicsDevice GD, float time)
        {
            float? customSky = DynamicTuning.Global?.GetTuning("city", 0, 2);

            if (!customSky.HasValue || !TryLoadSkyColor(GD, $"Textures/skycol_alt{(int)customSky}.png"))
            {
                TryLoadSkyColor(GD, DefaultSkyCol);
            }

            LoadFinalIfNeeded(GD);

            InitArrays();

            LastSkyPos = float.PositiveInfinity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 GetStarVector(Random random)
        {
            // Transform from uniform uv onto a spherical surface with unit size.
            float u = random.NextSingle();
            float v = random.NextSingle();
            float theta = u * 2f * MathF.PI;
            float phi = MathF.Acos(2f * v - 1f);

            return new Vector3(
                MathF.Sin(phi) * MathF.Cos(theta),
                MathF.Sin(phi) * MathF.Sin(theta),
                MathF.Cos(phi)
                );
        }

        private static float ApplyStarMultiplier(float opacity, float starMul)
        {
            if (starMul >= 1f)
            {
                return opacity;
            }

            // Based on the current multiplier, only show the brightest stars. (with a super quick fade)

            float opacityDiff = starMul - (1f - opacity);

            if (opacityDiff < 0)
            {
                return 0;
            }

            return Math.Min(1, opacityDiff * 50) * opacity;
        }

        private static void GenerateStarGeo(GraphicsDevice gd, float speed = 0f, float starMul = 1f)
        {
            var verts = new VertexPositionColorTexture[StarCount * 4];
            var inds = new int[StarCount * 6];

            var random = new Random(StarSeed);

            int verti = 0;
            int indi = 0;
            for (int i = 0; i < StarCount; i++)
            {
                var vec = GetStarVector(random);
                var size = (0.85f + random.NextSingle() * 0.3f) * StarSize;
                var opacity = ApplyStarMultiplier(1f - random.NextSingle() * 0.9f, starMul);

                float mySpeed = speed * MathF.Abs(MathF.Sqrt(vec.Y * vec.Y + vec.Z * vec.Z));

                inds[indi++] = verti;
                inds[indi++] = verti + 1;
                inds[indi++] = verti + 2;

                inds[indi++] = verti + 2;
                inds[indi++] = verti + 3;
                inds[indi++] = verti + 0;

                // Need to build a bit of a basis to billboard the star towards the (biasing y towards the rotation direction)

                Vector3 starY = Vector3.Cross(vec, Vector3.Left);
                starY.Normalize();
                Vector3 starX = Vector3.Cross(vec, starY);
                starX.Normalize();

                var color = new Color(1, 1, 1, opacity);

                // With the basis, construct the billboarded star.

                verts[verti++] = new VertexPositionColorTexture(vec + starX * -size + starY * -(size + mySpeed), color, new Vector2(0, 0));
                verts[verti++] = new VertexPositionColorTexture(vec + starX * size + starY * -(size + mySpeed), color, new Vector2(1, 0));
                verts[verti++] = new VertexPositionColorTexture(vec + starX * size + starY * (size + mySpeed), color, new Vector2(1, 1));
                verts[verti++] = new VertexPositionColorTexture(vec + starX * -size + starY * (size + mySpeed), color, new Vector2(0, 1));
            }

            StarVerts?.Dispose();
            StarInds?.Dispose();

            StarVerts = new VertexBuffer(gd, typeof(VertexPositionColorTexture), verti, BufferUsage.None);
            StarInds = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indi, BufferUsage.None);

            StarVerts.SetData(verts);
            StarInds.SetData(inds);

            ActiveStarMultiplier = starMul;
            ActiveStarSpeed = speed;
        }

        private void EnsureStarGeo(GraphicsDevice gd, float starMul)
        {
            if (StarVerts == null || ActiveStarSpeed != StarSpeed || ActiveStarMultiplier != starMul)
            {
                GenerateStarGeo(gd, StarSpeed, starMul);
            }
        }

        public void LoadFinalIfNeeded(GraphicsDevice GD)
        {
            bool needsFinal = FinaleUtils.IsFinale();

            if (!IsFinal && needsFinal)
            {
                using (var file = File.OpenRead(Path.Combine(FSOEnvironment.ContentDir, FinalSkyCol)))
                {
                    GradTex = ImageLoader.FromStream(GD, file);
                };

                IsFinal = true;
            }
        }

        private bool TryLoadSkyColor(GraphicsDevice GD, string path)
        {
            try
            {
                using (var file = File.OpenRead(Path.Combine(FSOEnvironment.ContentDir, path)))
                {
                    GradTex = ImageLoader.FromStream(GD, file);
                };
            }
            catch
            {
                return false;
            }

            return true;
        }

        private float[] m_SkyColors = new float[]
        {
            4/8f,
            4/8f,
            4/8f,
            5/8f,
            6/8f, //sunrise
            7/8f,
            8/8f, //peak
            0/8f, //peak
            0/8f,
            0/8f,
            1/8f, //sunset
            2/8f,
            3/8f,
            4/8f,
        };

        public float OutsideSkyP(float time)
        {
            double Progress = (time * (m_SkyColors.Length - 1)) % 1; //interpolation progress (mod 1)
            var sky1 = m_SkyColors [(int)Math.Floor(time * (m_SkyColors.Length - 1))]; //first colour
            var sky2 = m_SkyColors [(int)Math.Floor(time * (m_SkyColors.Length - 1)) + 1]; //second colour
            if (sky1 == 1f && sky2 == 0f) Progress = 0;
            return (float)Progress* sky2 + (1 - (float)Progress) * sky1;
        }

        private float DayOffset = 0.25f;
        private float DayDuration = 0.60f;

        public bool Night(float tod)
        {
            bool night = false;
            if (tod < DayOffset)
            {
                night = true;
            }
            else if (tod > DayOffset + DayDuration)
            {
                night = true;
            }
            return night;
        }

        private void InitArrays()
        {
            var subdivs = 65;

            int vertCount = (subdivs - 1) * (subdivs + 1) + 1;
            int indexCount = ((subdivs - 1) * (subdivs * 6 - 3)) - 3;

            VertexData = new VertexPositionTexture[vertCount];
            IndexData = new int[indexCount];
        }

        public void BuildSkyDome(GraphicsDevice GD, float time, Vector3 sunVector)
        {
            LoadFinalIfNeeded(GD);

            //generate sky dome geometry
            var subdivs = 65;
            VertexPositionTexture[] verts = VertexData;
            int[] indices = IndexData;
            var skyCol = new Color(0x00, 0x80, 0xFF, 0xFF);

            float skyPos = OutsideSkyP(time);
            LastSkyPos = time;
            skyPos += 1 / 16f;
            int vertLastStart = 0;
            int vertLastLength = 1;
            var yGap = 1f / GradTex.Height;
            var range = 1 - yGap;
            var topRange = 0.9f * range;
            var btmRange = 0.1f * range;
            float skyEffect = 0.07f;

            var sunPos = new Vector2(-sunVector.Z, sunVector.X) * MathF.Sin(time * MathF.PI * 2);
            var sunOffset = sunPos.Length() * (time > 0.5f ? -1 : 1);

            int vi = 0;
            int ii = 0;

            verts[vi++] = new VertexPositionTexture(new Vector3(0, 1, 0), new Vector2(skyPos - sunOffset * skyEffect, yGap));

            for (int y = 1; y < subdivs; y++)
            {
                int start = vi;
                var angley = (float)Math.PI * y / ((float)subdivs - 1);
                var radius = (float)Math.Sin(angley);
                var height = Math.Cos(angley);

                var sunDotEffect = MathF.Sqrt(1f - MathF.Abs((float)height));
                //var aheight = (height < -0.6f)?((-0.12f) - height):height;
                //var tpos = (0.9f - (float)Math.Sqrt(Math.Abs(aheight)) * 0.9f);
                
                var tpos = (float)((height > 0) ? (0.9f - Math.Sqrt(height) * topRange) : 0.9f + Math.Sqrt(-height) * btmRange);

                for (int x = 0; x < subdivs + 1; x++)
                {
                    var anglex = (float)Math.PI * x * 2 / (float)subdivs;
                    var colLerp = Math.Min(1, Math.Abs(((y - 2) / (float)subdivs) - 0.60f) * 4);

                    var pos = new Vector2(MathF.Sin(anglex), MathF.Cos(anglex));

                    var sunDot = (Vector2.Dot(pos, sunPos)) * sunDotEffect + sunOffset;

                    verts[vi++] = new VertexPositionTexture(new Vector3(pos.X * radius, (float)height, pos.Y * radius), new Vector2(skyPos - sunDot * skyEffect, tpos));
                    if (x < subdivs)
                    {
                        if (y != 1)
                        {
                            indices[ii++] = vertLastStart + x % vertLastLength;
                            indices[ii++] = vertLastStart + (x + 1) % vertLastLength;
                            indices[ii++] = vi - 1;
                        }

                        indices[ii++] = vertLastStart + (x + 1) % vertLastLength;
                        indices[ii++] = vi;
                        indices[ii++] = vi - 1;
                    }
                }
                vertLastStart = start;
                vertLastLength = subdivs + 1;
            }

            if (Verts == null) Verts = new VertexBuffer(GD, typeof(VertexPositionTexture), vi, BufferUsage.None);
            Verts.SetData(verts);
            if (Indices == null)
            {
                Indices = new IndexBuffer(GD, IndexElementSize.ThirtyTwoBits, ii, BufferUsage.None);
                Indices.SetData(indices);
                PrimCount = ii / 3;
            }
        }

        public Vector4 FogColor;
        public RasterizerState ClipState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthClipEnable = false
        };

        private float GetStarOpacity(Color outsideColor)
        {
            var avg = (outsideColor.R + outsideColor.G + outsideColor.B) / (3 * 255f);

            return Math.Max(0, Math.Min(1.30f - avg * avg * 6.5f, 1f));
        }

        private Matrix StarBaseRotation = Matrix.CreateRotationZ(MathF.PI / 2f);
        private Matrix StarPostRotation = Matrix.CreateRotationZ(MathF.PI * (45f / 180f)) * //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Matrix.CreateRotationY(MathF.PI * 0.3f) * //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.
            Matrix.CreateRotationY(MathF.PI / 2f);

        private Matrix GetStarRotationAxis(double tod)
        {
            double modTime;
            var offStart = 1 - (DayOffset + DayDuration);
            if (tod < DayOffset)
            {
                modTime = (offStart + tod) * 0.5 / (1 - DayDuration);
            }
            else if (tod > DayOffset + DayDuration)
            {
                modTime = (tod - (1 - offStart)) * 0.5 / (1 - DayDuration);
            }
            else
            {
                modTime = ((tod - DayOffset) * 0.5 / DayDuration) + 0.5;
            }

            Matrix Transform = StarBaseRotation;

            Transform *= Matrix.CreateRotationY((float)((modTime + 0.5) * Math.PI * 2.0));
            Transform *= StarPostRotation;

            return Transform;
        }

        public void Draw(GraphicsDevice gd, Color outsideColor, Matrix view, Matrix projection, float time, WeatherController weather, Vector3 sunVector, float scale)
        {
            var ocolor = outsideColor.ToVector4();
            var effect = WorldContent.GetBE(gd);

            var tod = FinaleUtils.BiasSunTime(time);
            var night = Night((float)tod);
            if (LastSkyPos != time) BuildSkyDome(gd, time, night ? -sunVector : sunVector);

            var color = (ocolor - new Vector4(0.35f)) * 1.5f + new Vector4(0.35f);
            color.W = 1;
            var wint = Math.Min(1f, weather.WeatherIntensity);

            float skyboxOpacity = (1 - (float)Math.Sqrt(wint) * 0.75f);

            effect.LightingEnabled = false;
            effect.Texture = GradTex;
            effect.Alpha = skyboxOpacity;
            effect.DiffuseColor = Vector3.One;
            effect.AmbientLightColor = Vector3.One;
            //effect.DiffuseColor = new Vector3(Math.Min(1, color.X), Math.Min(1, color.Y), Math.Min(1, color.Z));
            //effect.AmbientLightColor = new Vector3(color.X, color.Y, color.Z);
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = true;

            //var view = view;state.Camera.View;
            view.M41 = 0; view.M42 = 0; view.M43 = 0;
            var scaleVec = Vector3.TransformNormal(new Vector3(1, 0, 0), view);
            view = Matrix.CreateScale(1 / scaleVec.Length()) * view;
            effect.View = view;
            effect.Projection = projection;// (state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection;
            effect.World = Matrix.CreateScale(5f * scale);
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

            // Draw the stars

            var pos = sunVector;
            var z = -pos.X;
            pos.X = pos.Z;
            pos.Z = z;

            float starAlpha = GetStarOpacity(outsideColor) * skyboxOpacity;

            if (starAlpha > 0)
            {
                EnsureStarGeo(gd, FinaleUtils.GetStarMultiplier(time));
                effect.Alpha = starAlpha;
                effect.VertexColorEnabled = true;
                effect.Texture = TextureGenerator.GetStar(gd);
                gd.BlendState = BlendState.Additive;

                var starMat = Matrix.CreateScale(5f * scale) * GetStarRotationAxis(tod);
                starMat.Translation = Vector3.Zero;
                effect.World = starMat;

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.Indices = StarInds;
                    gd.SetVertexBuffer(StarVerts);
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, StarInds.IndexCount / 3);
                }

                effect.VertexColorEnabled = false;
                effect.Alpha = skyboxOpacity;
            }

            //draw the sun or moon
            var dist = 0.5f + pos.Y * 2;
            dist *= dist;
            dist += 0.5f;
            if (night) dist = 65;
            var sunMat = Matrix.CreateTranslation(0, 0, dist) * Matrix.CreateBillboard(pos, new Vector3(0, 0.4f, 0), Vector3.Up, null);

            var geom = WorldContent.GetTextureVerts(gd);
            effect.World = sunMat;
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = true;
            effect.Texture = (night) ? TextureGenerator.GetMoon(gd) : TextureGenerator.GetSun(gd);
            gd.SamplerStates[0] = SamplerState.LinearClamp;

            if (night)
            {
                var tint = new Vector3(color.X, color.Y, color.Z) * 0.6f;
                var lightIntensity = new Vector3(color.X, color.Y, color.Z).Length() + 0.4f;
                effect.DiffuseColor = FinaleUtils.BiasSunIntensity(new Vector3(lightIntensity) + tint, time);
            }
            else
            {
                float colorBias = Math.Abs(color.Z - color.X);

                // when the colour is uniformly white, penalize the brightness a bit

                color *= 0.6f + Math.Min(1f, colorBias / 0.8f) * 0.4f;

                effect.DiffuseColor = FinaleUtils.BiasSunIntensity(new Vector3(color.X, color.Y, color.Z) * 0.6f, time);
            }

            gd.BlendState = (night) ? BlendState.NonPremultiplied : BlendState.Additive;

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
