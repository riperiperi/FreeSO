using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using System.IO;
using TSO.Vitaboy;

namespace TSO.Content.codecs
{
    /// <summary>
    /// Codec for meshes (*.mesh).
    /// </summary>
    public class MeshCodec : IContentCodec<Mesh>
    {
        #region IContentCodec<Mesh> Members

        public Mesh Decode(System.IO.Stream stream)
        {
            var mesh = new Mesh();
            mesh.Read(stream);
            return mesh;
        }

        #endregion
    }
}
