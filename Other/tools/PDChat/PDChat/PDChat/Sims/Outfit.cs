/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using TSO.Common.content;
using TSO.Files.utils;

namespace PDChat.Sims
{
    /// <summary>
    /// Outfits collect together the light-, medium-, and dark-skinned versions of an 
    /// appearance and associate them collectively with a hand group and a body region (head or body).
    /// </summary>
    public class Outfit 
    {
        public uint LightAppearanceFileID;
        public uint LightAppearanceTypeID;

        public uint MediumAppearanceFileID;
        public uint MediumAppearanceTypeID;

        public uint DarkAppearanceFileID;
        public uint DarkAppearanceTypeID;

        public uint HandGroup;
        public uint Region;

        /// <summary>
        /// Gets the ContentID for the appearances referenced by this Outfit.
        /// </summary>
        /// <param name="type">The type of appearance to get.</param>
        /// <returns>A ContentID instance.</returns>
        public ContentID GetAppearance(AppearanceType type)
        {
            switch (type)
            {
                case AppearanceType.Light:
                    return new ContentID(LightAppearanceTypeID, LightAppearanceFileID);
                case AppearanceType.Medium:
                    return new ContentID(MediumAppearanceTypeID, MediumAppearanceFileID);
                case AppearanceType.Dark:
                    return new ContentID(DarkAppearanceTypeID, DarkAppearanceFileID);
            }

            return null;
        }

        /// <summary>
        /// Gets the ContentID for the Handgroup referenced by this Outfit.
        /// </summary>
        /// <returns>A ContentID instance.</returns>
        public ContentID GetHandgroup()
        {
            return new ContentID((uint)18, HandGroup);
        }

        /// <summary>
        /// Reads an Outfit from the supplied Stream.
        /// </summary>
        /// <param name="stream">A Stream instance.</param>
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();
                var unknown = io.ReadUInt32();

                LightAppearanceFileID = io.ReadUInt32();
                LightAppearanceTypeID = io.ReadUInt32();

                MediumAppearanceFileID = io.ReadUInt32();
                MediumAppearanceTypeID = io.ReadUInt32();

                DarkAppearanceFileID = io.ReadUInt32();
                DarkAppearanceTypeID = io.ReadUInt32();

                HandGroup = io.ReadUInt32();
                Region = io.ReadUInt32();
            }
        }

    }
}
