using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Vitaboy;
using TSO.Content.framework;

namespace TSO.Content.codecs
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
