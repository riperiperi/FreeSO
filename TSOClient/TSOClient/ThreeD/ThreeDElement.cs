/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// Represents a renderable 3D object.
    /// </summary>
    public abstract class ThreeDElement
    {
        protected ThreeDScene m_Scene;

        public ThreeDElement(ThreeDScene Scene)
        {
            m_Scene = Scene;
        }

        public virtual void Update(GameTime Time) { }

        public virtual void Draw() { }
    }
}
