using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    public class SpriteEffect : WorldEffect
    {
        protected override Type TechniqueType => typeof(SpriteEffectTechniques);

        private EffectParameter pMatrixTransform;
        private EffectParameter pGaussianStep;
        private EffectParameter pAllowedValue;
        private EffectParameter pHighlight;

        private EffectParameter pblurAmount;
        private EffectParameter pheightMultiplier;
        private EffectParameter phardenBias;
        private EffectParameter pnoiseTexture;

        public Matrix MatrixTransform
        {
            set
            {
                pMatrixTransform.SetValue(value);
            }
        }
        public Vector2 GaussianStep
        {
            set
            {
                pGaussianStep.SetValue(value);
            }
        }
        public float AllowedValue
        {
            set
            {
                pAllowedValue.SetValue(value);
            }
        }
        public float Highlight
        {
            set
            {
                pHighlight.SetValue(value);
            }
        }

        public Vector2 blurAmount
        {
            set
            {
                pblurAmount.SetValue(value);
            }
        }
        public Vector2 heightMultiplier
        {
            set
            {
                pheightMultiplier.SetValue(value);
            }
        }
        public Vector2 hardenBias
        {
            set
            {
                phardenBias.SetValue(value);
            }
        }
        public Texture2D noiseTexture
        {
            set
            {
                pnoiseTexture.SetValue(value);
            }
        }

        public SpriteEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public SpriteEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public SpriteEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();

            pMatrixTransform = Parameters["MatrixTransform"];
            pGaussianStep = Parameters["GaussianStep"];
            pAllowedValue = Parameters["AllowedValue"];
            pHighlight = Parameters["Highlight"];

            pblurAmount = Parameters["blurAmount"];
            pheightMultiplier = Parameters["heightMultiplier"];
            phardenBias = Parameters["hardenBias"];
            pnoiseTexture = Parameters["noiseTexture"];
    }
}

    public enum SpriteEffectTechniques
    {
        ShadowBlurBlit,
        ShadowSeparableBlit1,
        ShadowSeparableBlit2,
        ShadowSeparableBlit3,
        ShadowSeparableBlit4,
        StickyEffect, //not used for world
    }
}
