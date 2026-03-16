using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    public class MapGeneration : WorldEffect
    {
        protected override Type TechniqueType => typeof(MapGenerationTechniques);

        private EffectParameter pMatrixTransform;
        private EffectParameter pImageSize;
        private EffectParameter pStepSize;
        private EffectParameter pEdgeValue;

        private EffectParameter pSdfExpand;
        private EffectParameter pSdfFade;
        private EffectParameter pGradientScale;
        private EffectParameter pGradientBase;

        private EffectParameter pTerrainScale;
        private EffectParameter pSunDir;
        private EffectParameter pColor;

        private EffectParameter pSpecularPower;
        private EffectParameter pSpecularIntensity;

        private EffectParameter pBaseTexture;
        private EffectParameter pTerrainType;
        private EffectParameter pDistToColor;

        private EffectParameter pGaussianStep;
        private EffectParameter pGaussianSize;
        private EffectParameter pGaussianWeights;

        public Matrix MatrixTransform
        {
            set
            {
                pMatrixTransform.SetValue(value);
            }
        }
        public Vector2 ImageSize
        {
            set
            {
                pImageSize.SetValue(value);
            }
        }
        public int StepSize
        {
            set
            {
                pStepSize.SetValue(value);
            }
        }

        public int EdgeValue
        {
            set
            {
                pEdgeValue.SetValue(value);
            }
        }

        public float SdfExpand
        {
            set
            {
                pSdfExpand.SetValue(value);
            }
        }
        public float SdfFade
        {
            set
            {
                pSdfFade.SetValue(value);
            }
        }

        public float GradientScale
        {
            set
            {
                pGradientScale.SetValue(value);
            }
        }
        public float GradientBase
        {
            set
            {
                pGradientBase.SetValue(value);
            }
        }

        public float TerrainScale
        {
            set
            {
                pTerrainScale.SetValue(value);
            }
        }
        public Vector3 SunDir
        {
            set
            {
                pSunDir.SetValue(value);
            }
        }
        public Color Color
        {
            set
            {
                pColor.SetValue(value.ToVector4());
            }
        }

        public Vector4 ColorVec
        {
            set
            {
                pColor.SetValue(value);
            }
        }

        public float SpecularPower
        {
            set
            {
                pSpecularPower.SetValue(value);
            }
        }
        public float SpecularIntensity
        {
            set
            {
                pSpecularIntensity.SetValue(value);
            }
        }

        public Texture2D BaseTexture
        {
            set
            {
                pBaseTexture.SetValue(value);
            }
        }
        public Texture2D TerrainType
        {
            set
            {
                pTerrainType.SetValue(value);
            }
        }
        public Texture2D DistToColor
        {
            set
            {
                pDistToColor.SetValue(value);
            }
        }

        public Vector2 GaussianStep
        {
            set
            {
                pGaussianStep.SetValue(value);
            }
        }

        public int GaussianSize
        {
            set
            {
                pGaussianSize.SetValue(value);
            }
        }

        public float[] GaussianWeights
        {
            set
            {
                pGaussianWeights.SetValue(value);
            }
        }

        public MapGeneration(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public MapGeneration(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public MapGeneration(Effect cloneSource) : base(cloneSource)
        {
        }

        private float[] GaussianWorkingArray = new float[21];

        public void PrepareGaussianKernel(float blurSize)
        {
            var array = GaussianWorkingArray;
            int arraySize = Math.Min(array.Length, (int)Math.Round((blurSize + 0.5f) / 2));
            float sigma = (blurSize - 1) / 6;
            float sigma2 = sigma * sigma;

            float sum = 0;
            for (int i = 0; i < arraySize; i++)
            {
                array[i] = MathF.Exp(-(i * i) / (2 * sigma2));
                sum += array[i];
                if (i > 0)
                {
                    sum += array[i];
                }
            }

            for (int i = 0; i < arraySize; i++)
            {
                array[i] /= sum;
            }

            GaussianWeights = array;
            GaussianSize = arraySize;
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();

            pMatrixTransform = Parameters["MatrixTransform"];
            pImageSize = Parameters["ImageSize"];
            pStepSize = Parameters["StepSize"];
            pEdgeValue = Parameters["EdgeValue"];

            pSdfExpand = Parameters["SdfExpand"];
            pSdfFade = Parameters["SdfFade"];
            pGradientScale = Parameters["GradientScale"];
            pGradientBase = Parameters["GradientBase"];

            pTerrainScale = Parameters["TerrainScale"];
            pSunDir = Parameters["SunDir"];
            pColor = Parameters["Color"];

            pSpecularPower = Parameters["SpecularPower"];
            pSpecularIntensity = Parameters["SpecularIntensity"];

            pTerrainType = Parameters["TerrainType"];
            pDistToColor = Parameters["DistToColor"];
            pBaseTexture = Parameters["BaseTexture"];

            pGaussianSize = Parameters["GaussianSize"];
            pGaussianStep = Parameters["GaussianStep"];
            pGaussianWeights = Parameters["GaussianWeights"];
        }

        public void SetTechnique(MapGenerationTechniques technique)
        {
            SetTechnique((int)technique);
        }
    }

    public enum MapGenerationTechniques
    {
        JumpFloodInit,
        JumpFloodStep,
        JumpFloodFinal,
        CityEdgeDetect,
        JumpDistFill,
        TerrainLighting,
        TerrainSpecular,
        TerrainNormal,
        ForestOverlay,
        Gaussian,
    }
}
