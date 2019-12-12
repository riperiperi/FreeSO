using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Effects
{
    public class WorldBatchEffect : LightMappedEffect
    {
        protected override Type TechniqueType
        {
            get { return typeof(WorldBatchTechniques); }
        }

        private EffectParameter pViewProjection;
        private EffectParameter pWorldViewProjection;
        private EffectParameter pRotProjection;
        private EffectParameter piWVP;

        private EffectParameter pWorldUnitsPerTile;
        private EffectParameter pDirToFront;
        private EffectParameter pOffToBack;
        private EffectParameter pDepthOutMode;

        private EffectParameter pMaxFloor;

        private EffectParameter pPixelTexture;
        private EffectParameter pDepthTexture;
        private EffectParameter pMaskTexture;
        private EffectParameter pAmbientLight;

        private EffectParameter pPxOffset;
        private EffectParameter pWorldOffset;

        public Matrix viewProjection
        {
            set
            {
                pViewProjection.SetValue(value);
            }
        }
        public Matrix worldViewProjection
        {
            set
            {
                pWorldViewProjection.SetValue(value);
            }
        }
        public Matrix rotProjection
        {
            set
            {
                pRotProjection.SetValue(value);
            }
        }
        public Matrix iWVP
        {
            set
            {
                piWVP.SetValue(value);
            }
        }

        public float worldUnitsPerTile
        {
            set
            {
                pWorldUnitsPerTile.SetValue(value);
            }
        }
        public Vector3 dirToFront
        {
            set
            {
                pDirToFront.SetValue(value);
            }
        }
        public Vector4 offToBack
        {
            set
            {
                pOffToBack.SetValue(value);
            }
        }
        public bool depthOutMode
        {
            set
            {
                pDepthOutMode.SetValue(value);
            }
        }

        public float MaxFloor
        {
            set
            {
                pMaxFloor.SetValue(value);
            }
        }
        
        // Textures
        public Texture2D pixelTexture
        {
            set
            {
                pPixelTexture.SetValue(value);
            }
        }
        public Texture2D depthTexture
        {
            set
            {
                pDepthTexture.SetValue(value);
            }
        }
        public Texture2D maskTexture
        {
            set
            {
                pMaskTexture.SetValue(value);
            }
        }
        public Texture2D ambientLight
        {
            set
            {
                pAmbientLight.SetValue(value);
            }
        }

        public Vector2 PxOffset
        {
            set
            {
                pPxOffset?.SetValue(value);
            }
        }

        public Vector4 WorldOffset
        {
            set
            {
                pWorldOffset?.SetValue(value);
            }
        }

        public WorldBatchEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        public WorldBatchEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
        }

        public WorldBatchEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        protected override void PrepareParams()
        {
            base.PrepareParams();
            //2d world batch params

            pViewProjection = Parameters["viewProjection"];
            pWorldViewProjection = Parameters["worldViewProjection"];
            pRotProjection = Parameters["rotProjection"];
            piWVP = Parameters["iWVP"];

            pWorldUnitsPerTile = Parameters["worldUnitsPerTile"];
            pDirToFront = Parameters["dirToFront"];
            pOffToBack = Parameters["offToBack"];
            pDepthOutMode = Parameters["depthOutMode"];

            pMaxFloor = Parameters["MaxFloor"];

            pPixelTexture = Parameters["pixelTexture"];
            pDepthTexture = Parameters["depthTexture"];
            pMaskTexture = Parameters["maskTexture"];
            pAmbientLight = Parameters["ambientLight"];

            pPxOffset = Parameters["PxOffset"];
            pWorldOffset = Parameters["WorldOffset"];
        }

        public void SetTechnique(WorldBatchTechniques technique)
        {
            SetTechnique((int)technique);
        }
    }

    public enum WorldBatchTechniques
    {
        drawSimple = 0,
        drawSimpleID,
        drawZSprite,
        drawZWall,
        drawZSpriteDepthChannel,
        drawZWallDepthChannel,
        drawZSpriteOBJID,
        drawSimpleRestoreDepth
    }
}
