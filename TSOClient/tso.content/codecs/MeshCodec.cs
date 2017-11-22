/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using System.IO;
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
                mesh.Read((BCFReadProxy)io, false);
            }
            return mesh;
        }

        #endregion
    }
}
