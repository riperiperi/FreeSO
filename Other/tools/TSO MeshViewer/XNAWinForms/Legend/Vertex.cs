/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Dressup
{
    public class Vertex
    {
        public Vector3 Coord;
        public Vector3 Normal;
        public Vector2 TextureCoord;
        
        public BlendData Blend;
    }
}