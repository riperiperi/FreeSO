using FSO.Content.Framework;
using FSO.Vitaboy;
using FSO.Files.Utils;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for meshes (*.mesh).
    /// </summary>
    public class MeshCodec : IContentCodec<Mesh>
    {
        #region IContentCodec<Mesh> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var mesh = new Mesh();
            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                mesh.Read(io, false);
            }
            return mesh;
        }

        #endregion
    }
}
