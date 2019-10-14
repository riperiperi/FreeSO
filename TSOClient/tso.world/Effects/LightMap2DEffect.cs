using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    public class LightMap2DEffect : WorldEffect
    {
        protected override Type TechniqueType => typeof(LightMap2DTechniques);

        //fixed parameters
        private EffectParameter pProjection;
        private EffectParameter pTileSize; 

        //change between rendering light passes
        private EffectParameter pRoomTarget;
        private EffectParameter pRoomUVRescale;
        private EffectParameter pRoomUVOff;

        private EffectParameter pLightPosition;
        private EffectParameter pLightColor;
        private EffectParameter pLightSize;
        private EffectParameter pLightPower;
        private EffectParameter pLightIntensity;
        private EffectParameter pTargetRoom;
        private EffectParameter pBlurMax;
        private EffectParameter pBlurMin;
        private EffectParameter pMapLayout;
        private EffectParameter pUVBase;

        private EffectParameter pLightDirection;
        private EffectParameter pLightHeight;

        private EffectParameter pShadowPowers;
        private EffectParameter pSSAASize;

        private EffectParameter pIsOutdoors;

        private EffectParameter proomMap;
        private EffectParameter pshadowMap; 
        private EffectParameter pfloorShadowMap;

        //fixed parameters
        /// <summary>
        /// Projection matrix for drawing lights onto the lightmap.
        /// </summary>
        public Matrix Projection
        {
            set
            {
                pProjection.SetValue(value);
            }
        }
        /// <summary>
        /// used for position to room masking. percentage of position space (0, 1) a tile takes up.
        /// </summary>
        public Vector2 TileSize
        {
            set
            {
                pTileSize.SetValue(value);
            }
        }

        //change between rendering light passes

        /// <summary>
        /// room number for room masking
        /// </summary>
        public float RoomTarget
        {
            set
            {
                pRoomTarget.SetValue(value);
            }
        }
        public Vector2 RoomUVRescale
        {
            set
            {
                pRoomUVRescale.SetValue(value);
            }
        }
        public Vector2 RoomUVOff
        {
            set
            {
                pRoomUVOff.SetValue(value);
            }
        }

        /// <summary>
        /// in position space (0,1)
        /// </summary>
        public Vector2 LightPosition
        {
            set
            {
                pLightPosition.SetValue(value);
            }
        }
        public Vector4 LightColor
        {
            set
            {
                pLightColor.SetValue(value);
            }
        }
        /// <summary>
        /// in position space (0,1)
        /// </summary>
        public float LightSize
        {
            set
            {
                pLightSize.SetValue(value);
            }
        }
        /// <summary>
        /// gamma correction on lights. can get some nicer distributions.
        /// </summary>
        public float LightPower
        {
            set
            {
                pLightPower.SetValue(value);
            }
        }
        public float LightIntensity
        {
            set
            {
                pLightIntensity?.SetValue(value);
            }
        }
        public float TargetRoom
        {
            set
            {
                pTargetRoom.SetValue(value);
            }
        }
        public float BlurMax
        {
            set
            {
                pBlurMax.SetValue(value);
            }
        }
        public float BlurMin
        {
            set
            {
                pBlurMin.SetValue(value);
            }
        }
        public Vector2 MapLayout
        {
            set
            {
                pMapLayout.SetValue(value);
            }
        }
        public Vector2 UVBase
        {
            set
            {
                pUVBase.SetValue(value);
            }
        }

        public Vector3 LightDirection
        {
            set
            {
                pLightDirection.SetValue(value);
            }
        }
        public float LightHeight
        {
            set
            {
                pLightHeight.SetValue(value);
            }
        }

        public Vector2 ShadowPowers
        {
            set
            {
                pShadowPowers.SetValue(value);
            }
        }
        public Vector2 SSAASize
        {
            set
            {
                pSSAASize.SetValue(value);
            }
        }

        public bool IsOutdoors
        {
            set
            {
                pIsOutdoors?.SetValue(value);
            }
        }

        public Texture2D roomMap
        {
            set
            {
                proomMap.SetValue(value);
            }
        }
        /// <summary>
        /// alpha texture containing occlusion for this light. White = full occlusion.
        /// </summary>
        public Texture2D shadowMap
        {
            set
            {
                pshadowMap.SetValue(value);
            }
        }

        /// <summary>
        /// same as shadow, but floors only.
        /// </summary>
        public Texture2D floorShadowMap
        {
            set
            {
                pfloorShadowMap.SetValue(value);
            }
        }

        public LightMap2DEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public LightMap2DEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public LightMap2DEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();

            //fixed parameters
            pProjection = Parameters["Projection"];
            pTileSize = Parameters["TileSize"];

            //change between rendering light passes
            pRoomTarget = Parameters["RoomTarget"];
            pRoomUVRescale = Parameters["RoomUVRescale"];
            pRoomUVOff = Parameters["RoomUVOff"];

            pLightPosition = Parameters["LightPosition"];
            pLightColor = Parameters["LightColor"];
            pLightSize = Parameters["LightSize"];
            pLightPower = Parameters["LightPower"];
            pLightIntensity = Parameters["LightIntensity"];
            pTargetRoom = Parameters["TargetRoom"];
            pBlurMax = Parameters["BlurMax"];
            pBlurMin = Parameters["BlurMin"];
            pMapLayout = Parameters["MapLayout"];
            pUVBase = Parameters["UVBase"];

            pLightDirection = Parameters["LightDirection"];
            pLightHeight = Parameters["LightHeight"];

            pShadowPowers = Parameters["ShadowPowers"];
            pSSAASize = Parameters["SSAASize"];

            pIsOutdoors = Parameters["IsOutdoors"];

            proomMap = Parameters["roomMap"];
            pshadowMap = Parameters["shadowMap"];
            pfloorShadowMap = Parameters["floorShadowMap"];
        }

        public void SetTechnique(LightMap2DTechniques technique)
        {
            SetTechnique((int)technique);
        }
    }

    public enum LightMap2DTechniques
    {
        Draw2D = 0,
        DrawDirection
    }
}
