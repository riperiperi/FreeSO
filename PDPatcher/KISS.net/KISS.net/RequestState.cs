/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the KISS.Net.

The Initial Developer of the Original Code is
Afr0. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;

namespace KISS.net
{
    /// <summary>
    /// A RequestState is used to store request information between
    /// asynchronous calls to callbacks.
    /// </summary>
    public class RequestState
    {
        public WebRequest Request;
        public WebResponse Response;
        public string ContentType = "";
        public int ContentLength;

        public Stream ResponseStream;
        
        public DateTime TransferStart;
        public int BytesRead = 0;
        public double PctComplete = 0;
        public double KBPerSec = 0;

        public byte[] RequestBuffer;

        public RequestState()
        {
            RequestBuffer = new byte[11024];
        }
    }
}
