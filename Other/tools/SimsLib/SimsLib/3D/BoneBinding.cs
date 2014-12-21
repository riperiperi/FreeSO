/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace SimsLib.ThreeD
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