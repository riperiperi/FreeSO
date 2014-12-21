/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Vitaboy
{
    public class BoneBinding
    {
        public string BoneName;
        public int BoneIndex;
        public int FirstRealVertex;
        public int RealVertexCount;
        public int FirstBlendVertex;
        public int BlendVertexCount;
    }
}
