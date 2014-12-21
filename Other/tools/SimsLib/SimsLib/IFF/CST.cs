/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

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
    /// That's 'C' 'S' 'T' '\0'. Equivalent in format to STR#. 
    /// Whether or not this chunk type relates with Caret-separated text is unknown.
    /// </summary>
    public class CST : STR
    {
    }
}
