/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Rendering.City
{
    public interface ICityGeom
    {
        float CellWidth { get; set; }
        float CellHeight { get; set; }
        float CellYScale { get; set; }

        void Process(CityData city);
        void CreateBuffer(GraphicsDevice gd);
        void Draw(GraphicsDevice gd);
    }
}
