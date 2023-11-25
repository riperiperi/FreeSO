using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    /// <summary>
    /// An effect for drawing gradients. Used to draw objcet/wall shadow geometry for Moderate FSO Lighting
    /// </summary>
    public class GradEffect : WorldEffect
    {
        protected override Type TechniqueType => base.TechniqueType;

        private EffectParameter pProjection;
        
        public Matrix Projection
        {
            set
            {
                pProjection.SetValue(value);
            }
        }

        public GradEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public GradEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public GradEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();

            pProjection = Parameters["Projection"];
        }
    }

    public enum GradEffectTechniques
    {
        Draw2D
    }
}
