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
using FSO.Files.Formats.IFF;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to object global (*.iff) data in FAR3 archives.
    /// </summary>
    public class WorldObjectGlobals : FileProvider<IffFile>
    {
        public WorldObjectGlobals(Content contentManager)
            : base(contentManager, new IffCodec(), new Regex(".*/globals/.*\\.iff"))
        {
        }
    }
}
