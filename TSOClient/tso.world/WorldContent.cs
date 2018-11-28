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

namespace FSO.LotView
{
    /// <summary>
    /// Handles XNA content for the world.
    /// </summary>
    public class WorldContent
    {
        public static ContentManager ContentManager;
        private static BasicEffect _be;

        public static void Init(GameServiceContainer serviceContainer, string rootDir)
        {
            ContentManager = new ContentManager(serviceContainer);
            ContentManager.RootDirectory = rootDir;
        }

        public static string EffectSuffix
        {
            get { return ((FSOEnvironment.GLVer == 2) ?"iOS":""); }
        }

        public static Effect _2DWorldBatchEffect
        {
            get{
                return ContentManager.Load<Effect>("Effects/2DWorldBatch"+EffectSuffix);
            }
        }

        public static Effect Grad2DEffect
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/gradpoly2D");
            }
        }

        public static Effect Light2DEffect
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/LightMap2D");
            }
        }

        public static Effect GrassEffect
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/GrassShader"+EffectSuffix);
            }
        }

        public static Effect RCObject
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/RCObject" + EffectSuffix);
            }
        }

        public static Effect SSAA
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/SSAA");
            }
        }

        public static Effect SpriteEffect
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/SpriteEffects");
            }
        }

        public static Effect ParticleEffect
        {
            get
            {
                return ContentManager.Load<Effect>("Effects/ParticleShader");
            }
        }

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

        public static BasicEffect GetBE(GraphicsDevice gd)
        {
            if (_be == null) _be = new BasicEffect(gd);
            return _be;
        }
    }
}
