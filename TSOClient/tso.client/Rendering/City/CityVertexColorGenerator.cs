using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.LotView;
using FSO.LotView.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace FSO.Client.Rendering.City
{
    internal class CityVertexColorGenerator : IDisposable
    {
        private struct ColorStop
        {
            public readonly Color Color;
            public readonly float Stop;

            public ColorStop(Color color, float stop)
            {
                Color = color;
                Stop = stop;
            }
        }

        private Terrain Parent;
        private RenderTarget2D Normal;
        private RenderTarget2D VertexColor;
        private RenderTarget2D VertexColorTemp;
        private RenderTarget2D JumpFlood;
        private RenderTarget2D JumpFloodAlt;
        private RenderTarget2D GaussianWorking;
        private Texture2D TerrainType;
        private Texture2D Elevation;
        private RenderTarget2D ForestDensity;
        private RenderTarget2D TerrainEdge;
        private Texture2D WaterGradient;

        private MapGeneration Effect;

        private Color ForestColor = new Color(0xff3F7C49);
        private BlendState AdditiveRGB;

        public CityVertexColorGenerator(Terrain parent)
        {
            Parent = parent;
        }

        private void Init(GraphicsDevice gd)
        {
            Normal = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            VertexColor = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            VertexColorTemp = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            GaussianWorking = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            JumpFlood = new RenderTarget2D(gd, 512, 512);
            JumpFloodAlt = new RenderTarget2D(gd, 512, 512);
            TerrainType = new Texture2D(gd, 512, 512, false, SurfaceFormat.Alpha8);
            Elevation = new Texture2D(gd, 512, 512, false, SurfaceFormat.Alpha8);
            ForestDensity = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            TerrainEdge = new RenderTarget2D(gd, 512, 512, false, SurfaceFormat.Alpha8, DepthFormat.None);
            WaterGradient = GenerateGradient(gd, [
                new(new Color(0xffFFEA45), 0.0f),
                new(new Color(0xffFFEA45), 0.1f),
                new(new Color(0xffFF8646), 0.8f),
                new(new Color(0xffAB7448), 1)
                ], 2.5f);

            Effect = WorldContent.MapGenerationEffect;

            AdditiveRGB = new BlendState()
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
                ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
                AlphaDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
                AlphaSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.Zero,
            };
        }

        private void Blit(Texture2D src, RenderTarget2D target, BlendState blendState = null, SamplerState samplerState = null)
        {
            var gd = GameFacade.GraphicsDevice;
            gd.SetRenderTarget(target);

            var effect = Effect;

            effect.BaseTexture = src;
            effect.CurrentTechnique.Passes[0].Apply();

            gd.BlendState = blendState ?? BlendState.Opaque;
            gd.SetVertexBuffer(WorldContent.GetTextureVerts(gd));
            gd.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

            //effect.CurrentTechnique.Passes[0].Apply();
            /*
            Batch.Begin(blendState: blendState ?? BlendState.Opaque, effect: effect, samplerState: samplerState ?? SamplerState.PointClamp, sortMode: SpriteSortMode.Immediate);
            //effect.CurrentTechnique.Passes[0].Apply();
            Batch.Draw(src, new Vector2(), Color.White);
            Batch.End();
            */

            gd.SetRenderTarget(null);
        }

        private void Blur(RenderTarget2D toBlur, float blurSize, RenderTarget2D outTex = null)
        {
            outTex ??= toBlur;

            Effect.PrepareGaussianKernel(blurSize);
            Effect.SetTechnique(MapGenerationTechniques.Gaussian);

            Effect.GaussianStep = new Vector2(1f / toBlur.Width, 0);
            Blit(toBlur, GaussianWorking);

            Effect.GaussianStep = new Vector2(0, 1f / toBlur.Height);
            Blit(GaussianWorking, outTex);
        }

        private RenderTarget2D GetEdgeMap(TerrainType type)
        {
            Effect.ImageSize = new Vector2(TerrainType.Width, TerrainType.Height);
            Effect.EdgeValue = (int)type;

            Effect.SetTechnique(MapGenerationTechniques.CityEdgeDetect);

            Blit(TerrainType, TerrainEdge);

            /*
            WaterGradient = GenerateGradient(Batch.GraphicsDevice, [
            new(new Color(0xffFFEA45), 0),
                        new(new Color(0xffFF8646), 1)
            ], 2f);
            */

            return TerrainEdge;
        }

        private RenderTarget2D GetDistanceMap(TerrainType type)
        {
            // Builds a distance map from the edge of the specified terrain type.

            var gd = GameFacade.GraphicsDevice;
            var edge = GetEdgeMap(type);

            Effect.ImageSize = new Vector2(TerrainType.Width, TerrainType.Height);

            Effect.SetTechnique(MapGenerationTechniques.JumpFloodInit);

            Blit(edge, JumpFlood);

            int stepSize = 512;
            int i = 0;

            while (stepSize > 0)
            {
                var alt = (i % 2) == 1;
                var from = alt ? JumpFloodAlt : JumpFlood;
                var to = alt ? JumpFlood : JumpFloodAlt;

                Effect.StepSize = stepSize;

                Effect.SetTechnique(MapGenerationTechniques.JumpFloodStep);
                Blit(from, to);

                i++;
                stepSize >>= 1;
            }

            return (i % 2) == 1 ? JumpFloodAlt : JumpFlood;
        }

        private Texture2D GenerateGradient(GraphicsDevice gd, Span<ColorStop> colors, float power)
        {
            int width = 100;
            var grad = new Texture2D(gd, width, 1);
            var dat = new Color[width];
            var invPower = 1 / power;

            for (int i = 1; i < colors.Length; i++)
            {
                ColorStop from = colors[i - 1];
                ColorStop to = colors[i];

                int fromI = (int)(MathF.Pow(from.Stop, power) * width);
                int toI = (int)Math.Ceiling((MathF.Pow(to.Stop, power) * width));

                float fromBase = from.Stop;
                float range = to.Stop - from.Stop;

                for (int px = fromI; px <= Math.Min(width - 1, toI); px++)
                {
                    float stopPos = MathF.Pow(px / (float)width, invPower);

                    dat[px] = Color.Lerp(from.Color, to.Color, (stopPos - fromBase) / range);
                }
            }

            grad.SetData(dat);

            return grad;
        }

        private void DrawTerrain(TerrainType type, Texture2D color, float sdfFade = 0, float sdfExpand = 0)
        {
            // Calculate the SDF for this terrain type

            var sdf = GetDistanceMap(type);

            // Set shader parameters
            // Color (based off terrain type)
            // Expand sets how far past the edge in pixels the terrain type is filled
            // Fade sets how many pixels the edge is interpolated over. Starts at the expanded edge.

            Effect.ImageSize = new Vector2(TerrainType.Width, TerrainType.Height);

            Effect.SdfExpand = sdfExpand;
            Effect.SdfFade = sdfFade;
            Effect.GradientBase = 0;
            Effect.GradientScale = 120;

            Effect.TerrainType = TerrainType;
            Effect.DistToColor = color;
            Effect.EdgeValue = (int)type;

            Effect.SetTechnique(MapGenerationTechniques.JumpDistFill);

            Blit(sdf, VertexColorTemp, BlendState.AlphaBlend);

            // TODO
            // Forest effect scale
            // Noise
        }

        public int time = 0;
        public void Update(GraphicsDevice gd)
        {
            if (VertexColor == null)
            {
                Init(gd);
            }

            // Start by filling with white
            gd.SetRenderTarget(VertexColorTemp);
            gd.Clear(Color.White);

            // Upload the citymap terrain type to the texture
            CityMap map = Parent.MapData;
            var terrainType = map.GetRawTerrain();
            var why = MemoryMarshal.Cast<TerrainType, byte>(terrainType).ToArray();
            TerrainType.SetData(why);

            var elevation = map.GetRawElevation();
            Elevation.SetData(elevation);

            var forestDensity = map.GetRawForestDensity();
            var forestType = map.GetRawForestType();

            var filteredDensity = new Color[forestDensity.Length];
            for (int i = 0; i < filteredDensity.Length; i++)
            {
                var value = forestType[i] == ForestType.NULL ? (byte)0 : forestDensity[i];
                filteredDensity[i] = new Color(value, value, value, value);
            }

            ForestDensity.SetData(filteredDensity);
            Blur(ForestDensity, 7f);

            // Build a distance map for the shore

            // Draw the water with depth simulated by the shore distance

            DrawTerrain(Content.Model.TerrainType.WATER, WaterGradient, sdfFade: 1f, sdfExpand: 0.5f);

            // Draw the grass and rock (color altered by forest density, blurred, the rock more than the grass)
            // Draw the snow and sand (largely unaffected by anything)
            var grass = TextureUtils.TextureFromColor(gd, new Color(0xff60BFA5));
            var rock = TextureUtils.TextureFromColor(gd, new Color(0xff799DC0));
            var sand = TextureUtils.TextureFromColor(gd, new Color(255, 255, 233));
            var snow = TextureUtils.TextureFromColor(gd, Color.White);
            DrawTerrain(Content.Model.TerrainType.SAND, sand, sdfFade: 1.75f, sdfExpand: 0.25f);
            DrawTerrain(Content.Model.TerrainType.GRASS, grass, sdfFade: 1.4f, sdfExpand: 0.25f);
            DrawTerrain(Content.Model.TerrainType.ROCK, rock, sdfFade: 2.5f, sdfExpand: 0f);
            DrawTerrain(Content.Model.TerrainType.SNOW, snow, sdfFade: 2f, sdfExpand: 0.25f);

            // Draw forests

            Effect.SetTechnique(MapGenerationTechniques.ForestOverlay);

            Effect.TerrainType = TerrainType;
            ForestColor = new Color(0xff419e4d);
            var colorVec = ForestColor.ToVector4();
            colorVec.W = 1.25f;
            Effect.ColorVec = colorVec;
            Blit(ForestDensity, VertexColorTemp, blendState: BlendState.AlphaBlend);

            // Draw the default lighting

            Effect.SetTechnique(MapGenerationTechniques.TerrainNormal);

            Effect.SunDir = -Vector3.Normalize(new Vector3(0, -2, -1.7f));
            Effect.TerrainScale = 1/20f;
            Effect.TerrainType = TerrainType;

            Blit(Elevation, Normal);

            Blur(Normal, 5f);

            Effect.SetTechnique(MapGenerationTechniques.TerrainLighting);
            Effect.DistToColor = Normal;
            Blit(VertexColorTemp, VertexColor);

            Effect.SetTechnique(MapGenerationTechniques.TerrainSpecular);

            Effect.SunDir = -Vector3.Normalize(new Vector3(0, -2, -1f));
            Effect.SpecularPower = 6f;
            Effect.SpecularIntensity = 0.25f;

            Blit(Normal, VertexColor, AdditiveRGB);

            gd.SetRenderTarget(null);
        }

        public void DebugDraw(SpriteBatch sb)
        {
            if (VertexColor != null)
            {
                sb.Draw(VertexColor, new Vector2(), Color.White);
                sb.Draw(Normal, new Vector2(0, 512), Color.White);
            }
        }

        public Texture2D GetVertexColor()
        {
            return VertexColor;
        }

        public void Dispose()
        {
            Normal?.Dispose();
            VertexColor?.Dispose();
            VertexColorTemp?.Dispose();
            JumpFlood?.Dispose();
            JumpFloodAlt?.Dispose();
            GaussianWorking?.Dispose();
            TerrainType?.Dispose();
            Elevation?.Dispose();
            ForestDensity?.Dispose();
            TerrainEdge?.Dispose();
            WaterGradient?.Dispose();
        }
    }
}
