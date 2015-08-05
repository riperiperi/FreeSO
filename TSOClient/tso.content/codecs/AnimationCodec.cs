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

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for animations (*.anim).
    /// </summary>
    public class AnimationCodec : IContentCodec<Animation>
    {
        #region IContentCodec<Animation> Members

        public Animation Decode(System.IO.Stream stream)
        {
            var ani = new Animation();
            ani.Read(stream);
            return ani;
        }

        #endregion
    }
}
