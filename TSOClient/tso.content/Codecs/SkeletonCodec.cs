using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Vitaboy;
using FSO.Content.Framework;
using FSO.Files.Utils;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for skeletons (*.skel).
    /// </summary>
    public class SkeletonCodec : IContentCodec<Skeleton> 
    {

        #region IContentCodec<Skeleton> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new Skeleton();
            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                result.Read((BCFReadProxy)io, false);
            }
            return result;
        }

        #endregion
    }
}
