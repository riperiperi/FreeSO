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
