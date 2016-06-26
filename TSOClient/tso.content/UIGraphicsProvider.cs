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
using FSO.Content.Framework;
using FSO.Content.Codecs;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Model;
using System.IO;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to UI texture (*.bmp) data in FAR3 archives.
    /// </summary>
    public class UIGraphicsProvider : FAR3Provider<ITextureRef>
    {
        public static uint[] MASK_COLORS = new uint[]{
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        private Dictionary<ulong, string> Files = new Dictionary<ulong, string>();
        private Dictionary<ulong, ITextureRef> FilesCache = new Dictionary<ulong, ITextureRef>();


        public UIGraphicsProvider(Content contentManager)
            : base(contentManager, new TextureCodec(MASK_COLORS), new Regex("uigraphics/.*\\.dat"))
        {
            Files[0x00000Cb800000002] = "uigraphics/friendshipweb/friendshipwebalpha.tga";
            Files[0x00000Cbfb00000001] = "uigraphics/hints/hint_mechanicskill.bmp";

            Files[0x1AF0856DDBAC] = "uigraphics/chat/balloonpointersadbottom.bmp";
            Files[0x1B00856DDBAC] = "uigraphics/chat/balloonpointersadside.bmp";
            Files[0x1B10856DDBAC] = "uigraphics/chat/balloontilessad.bmp";
        }

        protected override ITextureRef ResolveById(ulong id)
        {
            if (Files.ContainsKey(id))
            {
                //Non far3 file
                if (FilesCache.ContainsKey(id)) { return FilesCache[id]; }
                var path = this.ContentManager.GetPath(Files[id]);
                using (var stream = File.OpenRead(path)) {
                    FilesCache.Add(id, Codec.Decode(stream));
                    return FilesCache[id];
                }
            }
            return base.ResolveById(id);
        }
    }
}
