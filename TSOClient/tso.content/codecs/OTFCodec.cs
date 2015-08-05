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
using FSO.Files.Formats.OTF;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for object tuning files (*.otf).
    /// </summary>
    public class OTFCodec : IContentCodec<OTFFile>
    {
        #region IContentCodec<OTF> Members

        public OTFFile Decode(System.IO.Stream stream)
        {
            var result = new OTFFile();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}
