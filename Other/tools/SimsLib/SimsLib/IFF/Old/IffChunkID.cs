/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
