using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    public class GrassEffect : LightMappedEffect
    {
        protected override Type TechniqueType
        {
            get { return typeof(GrassTechniques); }
        }

        private EffectParameter pProjection;
        private EffectParameter pView;
        private EffectParameter pWorld;

        private EffectParameter pScreenSize;
        private EffectParameter pLightGreen;
        private EffectParameter pDarkGreen;
        private EffectParameter pLightBrown;
        private EffectParameter pDarkBrown;
        private EffectParameter pDiffuseColor;
        private EffectParameter pScreenOffset;
        private EffectParameter pGrassProb;
        private EffectParameter pGrassFadeMul;

        private EffectParameter pTexOffset;
        private EffectParameter pTexMatrix;

        private EffectParameter pScreenRotCenter;
        private EffectParameter pScreenMatrix;

        private EffectParameter pScreenAlignUV;
        private EffectParameter pTexSize;

        private EffectParameter pTileSize;

        private EffectParameter pdepthOutMode;
        private EffectParameter pWater;
        private EffectParameter pCamPos;
        private EffectParameter pLightVec;
        private EffectParameter pAlpha;
        private EffectParameter pGrassShininess;
        private EffectParameter pUseTexture;
        private EffectParameter pIgnoreColor;

        private EffectParameter pParallaxHeight;
        private EffectParameter pParallaxUVTexMat;

        private EffectParameter pFadeRectangle;
        private EffectParameter pFadeWidth;

        private EffectParameter pMulRange;
        private EffectParameter pMulBase;
        private EffectParameter pBlurBounds;

        private EffectParameter pBaseTex;
        private EffectParameter pParallaxTex;
        private EffectParameter pNormalMapTex;
        private EffectParameter pRoomMap;
        private EffectParameter pRoomLight;
        private EffectParameter pTerrainNoise;
        private EffectParameter pTerrainNoiseMip;

        public Matrix Projection
        {
            set
            {
                pProjection.SetValue(value);
            }
        }
        public Matrix View
        {
            set
            {
                pView.SetValue(value);
            }
        }
        public Matrix World
        {
            set
            {
                pWorld.SetValue(value);
            }
        }

        public Vector2 ScreenSize
        {
            set
            {
                pScreenSize.SetValue(value);
            }
        }
        public Vector4 LightGreen
        {
            set
            {
                pLightGreen.SetValue(value);
            }
        }
        public Vector4 DarkGreen
        {
            set
            {
                pDarkGreen.SetValue(value);
            }
        }
        public Vector4 LightBrown
        {
            set
            {
                pLightBrown.SetValue(value);
            }
        }
        public Vector4 DarkBrown
        {
            set
            {
                pDarkBrown.SetValue(value);
            }
        }
        public Vector4 DiffuseColor
        {
            set
            {
                pDiffuseColor.SetValue(value);
            }
        }
        public Vector2 ScreenOffset
        {
            set
            {
                pScreenOffset.SetValue(value);
            }
        }
        public float GrassProb
        {
            set
            {
                pGrassProb.SetValue(value);
            }
        }
        public float GrassFadeMul
        {
            set
            {
                pGrassFadeMul.SetValue(value);
            }
        }

        public Vector2 TexOffset
        {
            set
            {
                pTexOffset.SetValue(value);
            }
        }
        public Vector4 TexMatrix
        {
            set
            {
                pTexMatrix.SetValue(value);
            }
        }

        public Vector2 ScreenRotCenter
        {
            set
            {
                pScreenRotCenter.SetValue(value);
            }
        }
    
        public Vector4 ScreenMatrix
        {
            set
            {
                pScreenMatrix.SetValue(value);
            }
        }

        public bool ScreenAlignUV
        {
            set
            {
                pScreenAlignUV.SetValue(value);
            }
        }

        public Vector2 TexSize
        {
            set
            {
                pTexSize.SetValue(value);
            }
        }


        public Vector2 TileSize
        {
            set
            {
                pTileSize.SetValue(value);
            }
        }

        public bool depthOutMode
        {
            set
            {
                pdepthOutMode.SetValue(value);
            }
        }
        public bool Water
        {
            set
            {
                pWater.SetValue(value);
            }
        }
        public Vector3 CamPos
        {
            set
            {
                pCamPos.SetValue(value);
            }
        }
        public Vector3 LightVec
        {
            set
            {
                pLightVec.SetValue(value);
            }
        }
        public float Alpha
        {
            set
            {
                pAlpha.SetValue(value);
            }
        }
        public float GrassShininess
        {
            set
            {
                pGrassShininess.SetValue(value);
            }
        }
        public bool UseTexture
        {
            set
            {
                pUseTexture.SetValue(value);
            }
        }
        public bool IgnoreColor
        {
            set
            {
                pIgnoreColor.SetValue(value);
            }
        }

        public float ParallaxHeight
        {
            set
            {
                pParallaxHeight.SetValue(value);
            }
        }
        public Vector4 ParallaxUVTexMat
        {
            set
            {
                pParallaxUVTexMat.SetValue(value);
            }
        }

        //=== FADE ===

        public Vector4 FadeRectangle
        {
            set
            {
                pFadeRectangle.SetValue(value);
            }
        }
        public float FadeWidth
        {
            set
            {
                pFadeWidth.SetValue(value);
            }
        }

        public float MulRange
        {
            set
            {
                pMulRange.SetValue(value);
            }
        }
        public float MulBase
        {
            set
            {
                pMulBase.SetValue(value);
            }
        }
        public Vector4 BlurBounds
        {
            set
            {
                pBlurBounds.SetValue(value);
            }
        }

        //=== TEXTURES ===

        public Texture2D BaseTex
        {
            set
            {
                pBaseTex.SetValue(value);
            }
        }
        public Texture2D ParallaxTex
        {
            set
            {
                pParallaxTex.SetValue(value);
            }
        }
        public Texture2D NormalMapTex
        {
            set
            {
                pNormalMapTex.SetValue(value);
            }
        }
        public Texture2D RoomMap
        {
            set
            {
                pRoomMap.SetValue(value);
            }
        }
        public Texture2D RoomLight
        {
            set
            {
                pRoomLight.SetValue(value);
            }
        }
        
        /// <summary>
        /// Noise used for the grass shader.
        /// </summary>
        public Texture2D TerrainNoise
        {
            set
            {
                pTerrainNoise.SetValue(value);
            }
        }

        /// <summary>
        /// Noise used for the grass shader with mipmap filtering.
        /// </summary>
        public Texture2D TerrainNoiseMip
        {
            set
            {
                pTerrainNoiseMip.SetValue(value);
            }
        }

        public GrassEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        public GrassEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public GrassEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();

            pProjection = Parameters["Projection"];
            pView = Parameters["View"];
            pWorld = Parameters["World"];

            pScreenSize = Parameters["ScreenSize"];
            pLightGreen = Parameters["LightGreen"];
            pDarkGreen = Parameters["DarkGreen"];
            pLightBrown = Parameters["LightBrown"];
            pDarkBrown = Parameters["DarkBrown"];
            pDiffuseColor = Parameters["DiffuseColor"];
            pScreenOffset = Parameters["ScreenOffset"];
            pGrassProb = Parameters["GrassProb"];
            pGrassFadeMul = Parameters["GrassFadeMul"];

            pTexOffset = Parameters["TexOffset"];
            pTexMatrix = Parameters["TexMatrix"];

            pScreenRotCenter = Parameters["ScreenRotCenter"];
            pScreenMatrix = Parameters["ScreenMatrix"];

            pScreenAlignUV = Parameters["ScreenAlignUV"];
            pTexSize = Parameters["TexSize"];

            pTileSize = Parameters["TileSize"];

            pdepthOutMode = Parameters["depthOutMode"];
            pWater = Parameters["Water"];
            pCamPos = Parameters["CamPos"];
            pLightVec = Parameters["LightVec"];
            pAlpha = Parameters["Alpha"];
            pGrassShininess = Parameters["GrassShininess"];
            pUseTexture = Parameters["UseTexture"];
            pIgnoreColor = Parameters["IgnoreColor"];

            pParallaxHeight = Parameters["ParallaxHeight"];
            pParallaxUVTexMat = Parameters["ParallaxUVTexMat"];

            pFadeRectangle = Parameters["FadeRectangle"];
            pFadeWidth = Parameters["FadeWidth"];

            pMulRange = Parameters["MulRange"];
            pMulBase = Parameters["MulBase"];
            pBlurBounds = Parameters["BlurBounds"];

            pBaseTex = Parameters["BaseTex"];
            pParallaxTex = Parameters["ParallaxTex"];
            pNormalMapTex = Parameters["NormalMapTex"];
            pRoomMap = Parameters["RoomMap"];
            pRoomLight = Parameters["RoomLight"];
            pTerrainNoise = Parameters["TerrainNoise"];
            pTerrainNoiseMip = Parameters["TerrainNoiseMip"];
        }

        public void SetTechnique(GrassTechniques technique)
        {
            SetTechnique((int)technique);
        }
    }

    public enum GrassTechniques
    {
        DrawBase = 0,
        DrawGrid,
        DrawBlades,
        DrawLMap,
        DrawMask
    }
}
