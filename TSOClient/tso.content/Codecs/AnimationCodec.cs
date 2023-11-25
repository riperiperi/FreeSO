using FSO.Vitaboy;
using FSO.Content.Framework;
using FSO.Files.Utils;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for animations (*.anim).
    /// </summary>
    public class AnimationCodec : IContentCodec<Animation>
    {
        #region IContentCodec<Animation> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var ani = new Animation();
            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                ani.Read(io, false);
            }
            return ani;
        }

        #endregion
    }
}
