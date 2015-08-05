/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    public class HandgroupCodec : IContentCodec<HandGroup>
    {
        #region IContentCodec<Binding> Members

        public HandGroup Decode(Stream stream)
        {
            HandGroup Hag = new HandGroup();
            Hag.Read(stream);
            return Hag;
        }

        #endregion
    }
}
