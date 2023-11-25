using FSO.Content.Framework;
using FSO.Vitaboy;
using FSO.Files.Utils;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for ts1 meshes (*.bmf).
    /// </summary>
    public class BMFCodec : IContentCodec<Mesh>
    {
        #region IContentCodec<Mesh> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var mesh = new Mesh();
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                mesh.Read((BCFReadProxy)io, true);
            }
            return mesh;
        }

        #endregion
    }
}
