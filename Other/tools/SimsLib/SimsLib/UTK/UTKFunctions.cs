/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SimsLib.UTK
{
    /// <summary>
    /// Imported functions from "utkx64.dll"/"utkx32.dll".
    /// utk_decode should not be called on its own, use the
    /// UTKWrapper class.
    /// </summary>
    public class UTKFunctions
    {
        /// <summary>
        /// Generates tables used by the UTalk decompression function.
        /// Should only be called once, as it is quite memory intensive.
        /// </summary>
        [DllImport("utalkx64.dll")]
        public static extern void UTKGenerateTables();

        [DllImport("utalkx64.dll")]
        public static unsafe extern void utk_decode(byte* InBuffer, byte* OutBuffer, uint Frames);
    }
}
