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
using FSO.Common.Content;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to animation (*.anim) data in FAR3 archives.
    /// </summary>
    public class AvatarAnimationProvider : CompositeProvider<Animation>
    {
        public FAR3Provider<Animation> FAR;
        public RuntimeProvider<Animation> Runtime;

        public List<string> AnimationsByName
        {
            get
            {
                var farp = Providers.FirstOrDefault(x => x is FAR3Provider<Animation>) as FAR3Provider<Animation>;
                var runtime = Providers.FirstOrDefault(x => x is RuntimeProvider<Animation>) as RuntimeProvider<Animation>;
                
                return farp.EntriesByName.Keys.ToList().Concat(runtime.EntriesByName.Keys).ToList(); //expose so we can list all animations, for now.
                //todo: cleanup
            }
        }

        public Dictionary<string, Animation> VerbatimAnimations;

        public AvatarAnimationProvider(Content contentManager) : base()
        {
            FAR = new FAR3Provider<Animation>(contentManager, new AnimationCodec(), new Regex(".*/animations/.*\\.dat"));
            Runtime = new RuntimeProvider<Animation>();

            SetProviders(new List<IContentProvider<Animation>> {
                FAR,
                Runtime
            });
        }

        public void Init()
        {
            FAR.Init();
        }
    }
}
