/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Content
{
    /// <summary>
    /// Represents the ID of a content resource.
    /// Consists of two parts: TypeID (uint) and FileID (uint).
    /// </summary>
    public class ContentID
    {
        public uint TypeID;
        public uint FileID;
        public string FileName;
        private long v;


        /// <summary>
        /// Creates a new ContentID instance.
        /// </summary>
        /// <param name="typeID">The TypeID of the content resource.</param>
        /// <param name="fileID">The FileID of the content resource.</param>
        public ContentID(uint typeID, uint fileID)
        {
            this.TypeID = typeID;
            this.FileID = fileID;
        }

        public ContentID(string name)
        {
            this.FileName = name;
        }

        public ContentID(long v)
        {
            this.TypeID = (uint)v;
            this.FileID = (uint)(v >> 32);
        }

        public ulong Shift()
        {
            var fileIDLong = ((ulong)FileID) << 32;
            return fileIDLong | TypeID;
        }
    }
}
