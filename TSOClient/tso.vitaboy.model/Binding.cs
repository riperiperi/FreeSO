/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Common.utils;
using TSO.Files.utils;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Bindings points to meshes and appearances.
    /// </summary>
    public class Binding
    {
        public string Bone;
        public uint MeshGroupID;
        public uint MeshFileID;
        public uint MeshTypeID;

        public uint TextureGroupID;
        public uint TextureFileID;
        public uint TextureTypeID;

        /// <summary>
        /// Reads a binding from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a binding.</param>
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();
                if (version != 1)
                {
                    throw new Exception("Unknown binding version");
                }

                Bone = io.ReadPascalString();
                var meshType = io.ReadUInt32();
                if (meshType == 8)
                {
                    this.MeshGroupID = io.ReadUInt32();
                    this.MeshFileID = io.ReadUInt32();
                    this.MeshTypeID = io.ReadUInt32();
                }

                var textureType = io.ReadUInt32();
                if(textureType == 8)
                {
                    this.TextureGroupID = io.ReadUInt32();
                    this.TextureFileID = io.ReadUInt32();
                    this.TextureTypeID = io.ReadUInt32();
                }
            }
        }
    }
}
