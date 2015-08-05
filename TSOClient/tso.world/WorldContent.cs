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

namespace FSO.LotView
{
    /// <summary>
    /// Handles XNA content for the world.
    /// </summary>
    public class WorldContent
    {
        public static ContentManager ContentManager;

        public static void Init(GameServiceContainer serviceContainer, string rootDir)
        {
            ContentManager = new ContentManager(serviceContainer);
            ContentManager.RootDirectory = rootDir;
        }

        public static Effect _2DWorldBatchEffect
        {
            get{
                return ContentManager.Load<Effect>("Effects/2DWorldBatch");
            }
        }

        public static Texture2D GridTexture
        {
            get
            {
                return ContentManager.Load<Texture2D>("Textures/gridTexture");
            }
        }
    }
}
