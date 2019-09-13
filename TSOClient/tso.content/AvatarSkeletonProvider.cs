/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Vitaboy;
using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Codecs;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to skeleton (*.skel) data in FAR3 archives.
    /// </summary>
    public class AvatarSkeletonProvider : TSOAvatarContentProvider<Skeleton>
    {
        public AvatarSkeletonProvider(Content contentManager) : base(contentManager, new SkeletonCodec(),
            new Regex(".*/skeletons/.*\\.dat"),
            new Regex("Avatar/Skeletons/.*\\.skel"))
        {
        }
    }
}
