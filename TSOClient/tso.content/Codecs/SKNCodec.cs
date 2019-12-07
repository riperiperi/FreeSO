using FSO.Content.Framework;
using FSO.Files.Utils;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for ts1 meshes (*.skn).
    /// for some reason these are plaintext bmf.
    /// </summary>
    public class SKNCodec : IContentCodec<Mesh>
    {
        #region IContentCodec<Mesh> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var mesh = new Mesh();
            using (var io = new BCFReadString(stream, false))
            {
                mesh.Read((BCFReadProxy)io, true);
            }
            return mesh;
        }

        #endregion
    }
}
