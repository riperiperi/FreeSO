/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Files.Utils
{
    /// <summary>
    /// A texture used in the game world.
    /// </summary>
    public class WorldTexture
    {
        public Texture2D Pixel;
        public Texture2D ZBuffer;
        public Texture2D Palette;
    }
}
