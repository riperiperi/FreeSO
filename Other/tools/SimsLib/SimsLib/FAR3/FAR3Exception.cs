/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace SimsLib.FAR3
{
    /// <summary>
    /// Represents an exception thrown by a FAR3Archive instance.
    /// </summary>
    public class FAR3Exception : Exception
    {
        public FAR3Exception(string Message)
            : base(Message)
        {
        }
    }
}
