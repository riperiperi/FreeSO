/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.ThreeD
{
    /// <summary>
    /// Bindings (known as skins in The Sims 1) specify a mesh and the bone to which it is applied. 
    /// They may optionally specify a texture to apply to the mesh.
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

        public ulong MeshAssetID
        {
            get { return (ulong)MeshFileID << 32 | MeshTypeID; }
        }

        public ulong TextureAssetID
        {
            get { return (ulong)TextureFileID << 32 | TextureTypeID; }
        }

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
                if (textureType == 8)
                {
                    this.TextureGroupID = io.ReadUInt32();
                    this.TextureFileID = io.ReadUInt32();
                    this.TextureTypeID = io.ReadUInt32();
                }
            }
        }
    }
}