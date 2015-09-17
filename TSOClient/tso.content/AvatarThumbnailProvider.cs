/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to avatar thumbnail data in FAR3 archives.
    /// </summary>
    public class AvatarThumbnailProvider : PackingslipProvider<Texture2D> {
        public AvatarThumbnailProvider(Content contentManager, GraphicsDevice device)
            : base(contentManager, "packingslips/thumbnails.xml", new TextureCodec(device))
        {
        }
    }
}
