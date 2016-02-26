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
    /// Provides access to animation (*.anim) data in FAR3 archives.
    /// </summary>
    public class AvatarAnimationProvider : FAR3Provider<Animation>
    {
        public Dictionary<string, Far3ProviderEntry<Animation>> AnimationsByName
        {
            get
            {
                return EntriesByName; //expose so we can list all animations, for now.
                //todo: cleanup
            }
        }

        public AvatarAnimationProvider(Content contentManager)
            : base(contentManager, new AnimationCodec(), new Regex(".*/animations/.*\\.dat"))
        {
        }
    }
}
