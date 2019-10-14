using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Effects
{
    public class LightMappedEffect : WorldEffect
    {
        private EffectParameter pOutsideDark;
        private EffectParameter pMinAvg;
        private EffectParameter pWorldToLightFactor;
        private EffectParameter pLightOffset;
        private EffectParameter pMapLayout;
        private EffectParameter pLevel;
        private EffectParameter pLightingAdjust;

        private EffectParameter pAdvancedLight;
        private EffectParameter pAdvancedDirection;

        public Vector4 OutsideDark
        {
            set
            {
                pOutsideDark.SetValue(value);
            }
        }
        public Vector2 MinAvg
        {
            set
            {
                pMinAvg?.SetValue(value);
            }
        }
        public Vector3 WorldToLightFactor
        {
            set
            {
                pWorldToLightFactor.SetValue(value);
            }
        }
        public Vector2 LightOffset
        {
            set
            {
                pLightOffset.SetValue(value);
            }
        }
        public Vector2 MapLayout
        {
            set
            {
                pMapLayout.SetValue(value);
            }
        }
        public float Level
        {
            set
            {
                pLevel.SetValue(value);
            }
        }

        public Texture2D AdvancedLight {
            set
            {
                pAdvancedLight.SetValue(value);
            }
        }
        public Texture2D AdvancedDirection
        {
            set
            {
                pAdvancedDirection?.SetValue(value);
            }
        }

        //should default to 1s. Used to adjust slow updating surround lighting to match main lighting.
        public Vector3 LightingAdjust
        {
            set
            {
                pLightingAdjust.SetValue(value);
            }
        }

        public LightMappedEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public LightMappedEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public LightMappedEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();
            pOutsideDark = Parameters["OutsideDark"];
            pMinAvg = Parameters["MinAvg"];
            pWorldToLightFactor = Parameters["WorldToLightFactor"];
            pLightOffset = Parameters["LightOffset"];
            pMapLayout = Parameters["MapLayout"];
            pLevel = Parameters["Level"];
            pLightingAdjust = Parameters["LightingAdjust"];

            pAdvancedDirection = Parameters["advancedDirection"];
            pAdvancedLight = Parameters["advancedLight"];
        }
    }

    public enum LightingEmptyTechniques
    {

    }
}
