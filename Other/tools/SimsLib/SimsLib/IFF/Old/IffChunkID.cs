/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace SimsLib.IFF
{
    /// <summary>
    /// The chunk ID for an IFF chunk.
    /// </summary>
    public class IffChunkID
    {
        public static uint SPR2
        {
            get { return ToChunkID("SPR2"); }
        }

        public static uint SPR
        {
            get { return ToChunkID("SPR#"); }
        }

        public static uint DGRP
        {
            get { return ToChunkID("DGRP"); }
        }

        public static uint RSMP
        {
            get { return ToChunkID("rsmp"); }
        }

        public static uint PALT
        {
            get { return ToChunkID("PALT"); }
        }

        public static uint BHAV
        {
            get { return ToChunkID("BHAV"); }
        }

        public static uint OBJF
        {
            get { return ToChunkID("OBJf"); }
        }

        private static uint ToChunkID(string StrID)
        {
            uint A = StrID[0];
            uint B = StrID[1];
            uint C = StrID[2];
            uint D = StrID[3];

            return ((A << 24) | (B << 16) | (C << 8) | D);
        }
    }
}
