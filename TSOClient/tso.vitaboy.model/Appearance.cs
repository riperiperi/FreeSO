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
using TSO.Common.content;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Represents an appearance for a model.
    /// </summary>
    public class Appearance
    {
        public uint ThumbnailTypeID;
        public uint ThumbnailFileID;
        public AppearanceBinding[] Bindings;

        public ContentID ThumbnailID
        {
            get
            {
                return new ContentID(ThumbnailTypeID, ThumbnailFileID);
            }
        }

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream)){
                var version = io.ReadUInt32();

                ThumbnailFileID = io.ReadUInt32();
                ThumbnailTypeID = io.ReadUInt32();

                var numBindings = io.ReadUInt32();
                Bindings = new AppearanceBinding[numBindings];

                for (var i = 0; i < numBindings; i++){
                    Bindings[i] = new AppearanceBinding {
                        FileID = io.ReadUInt32(),
                        TypeID = io.ReadUInt32()
                    };
                }
            }
        }
    }

    public class AppearanceBinding
    {
        public uint TypeID;
        public uint FileID;
    }
}
