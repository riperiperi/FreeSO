/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common;
using FSO.LotView.Effects;

namespace FSO.LotView
{
    /// <summary>
    /// Handles XNA content for the world.
    /// </summary>
    public static class WorldContent
    {
        public static ContentManager ContentManager;
        private static BasicEffect _be;

        public static void Init(GameServiceContainer serviceContainer, string rootDir)
        {
            ContentManager = new ContentManager(serviceContainer);
            ContentManager.RootDirectory = rootDir;

            LoadEffects(false);
        }

        public static void LoadEffects(bool reload)
        {
            _2DWorldBatchEffect = new WorldBatchEffect(ContentManager.Load<Effect>("Effects/2DWorldBatch" + EffectSuffix));
            Grad2DEffect = new GradEffect(ContentManager.Load<Effect>("Effects/gradpoly2D"));
            Light2DEffect = new LightMap2DEffect(ContentManager.Load<Effect>("Effects/LightMap2D"));
            GrassEffect = new GrassEffect(ContentManager.Load<Effect>("Effects/GrassShader" + EffectSuffix));
            RCObject = new RCObjectEffect(ContentManager.Load<Effect>("Effects/RCObject" + EffectSuffix));
            SSAA = ContentManager.Load<Effect>("Effects/SSAA");
            SpriteEffect = new Effects.SpriteEffect(ContentManager.Load<Effect>("Effects/SpriteEffects" + EffectSuffix));
            ParticleEffect = new LightMappedEffect(ContentManager.Load<Effect>("Effects/ParticleShader"));
            AvatarEffect = new LightMappedEffect(ContentManager.Load<Effect>("Effects/Vitaboy" + EffectSuffix));

            LightEffects = new List<LightMappedEffect>()
            {
                _2DWorldBatchEffect,
                GrassEffect,
                RCObject,
                ParticleEffect,
                AvatarEffect
            };
        }

        public static List<LightMappedEffect> LightEffects;

        public static string EffectSuffix
        {
            get { return ((FSOEnvironment.GLVer == 2) ?"iOS":""); }
        }

        public static WorldBatchEffect _2DWorldBatchEffect;

        public static GradEffect Grad2DEffect;

        public static LightMap2DEffect Light2DEffect;

        public static GrassEffect GrassEffect;

        public static RCObjectEffect RCObject;

        public static Effect SSAA;

        public static Effects.SpriteEffect SpriteEffect;

        public static LightMappedEffect ParticleEffect;

        public static LightMappedEffect AvatarEffect;

        private static VertexBuffer _TextureVerts;
        public static VertexBuffer GetTextureVerts(GraphicsDevice gd) 
        {
            if (_TextureVerts == null)
            {
                var verts = new VertexPositionTexture[4];
                verts[0] = new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 0));
                verts[1] = new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 1));
                verts[2] = new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 0));
                verts[3] = new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 1));
                _TextureVerts = new VertexBuffer(gd, typeof(VertexPositionTexture), 4, BufferUsage.None);
                _TextureVerts.SetData(verts);
            }
            return _TextureVerts;
        }

        private static VertexBuffer _TextureVertsInv;
        public static VertexBuffer GetTextureVertsInv(GraphicsDevice gd)
        {
            if (_TextureVertsInv == null)
            {
                var verts = new VertexPositionTexture[4];
                verts[0] = new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 0));
                verts[2] = new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 1));
                verts[1] = new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 0));
                verts[3] = new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 1));
                _TextureVertsInv = new VertexBuffer(gd, typeof(VertexPositionTexture), 4, BufferUsage.None);
                _TextureVertsInv.SetData(verts);
            }
            return _TextureVertsInv;
        }

        public static BasicEffect GetBE(GraphicsDevice gd)
        {
            if (_be == null) _be = new BasicEffect(gd);
            return _be;
        }
    }
}
