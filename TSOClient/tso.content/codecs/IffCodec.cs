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
using FSO.Files.Formats.IFF;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for iffs (*.iff).
    /// </summary>
    public class IffCodec : IContentCodec<IffFile>
    {
        #region IContentCodec<Iff> Members

        public IffFile Decode(System.IO.Stream stream)
        {
            var result = new IffFile();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}
