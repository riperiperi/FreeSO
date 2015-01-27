/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Afr0. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Lot
{
    public class Wall
    {
        private Tile m_Tile;        //The tile that this wall is placed on.
        private TileSegment m_Segment;  //The segment of a tile that this wall is placed on.

        /// <summary>
        /// Constructor for the Wall class.
        /// </summary>
        /// <param name="Tle">The tile that this wall is standing on.</param>
        /// <param name="Segment">The segment of the tile that this wall is occupying.</param>
        public Wall(Tile Tle, TileSegment Segment)
        {
            m_Tile = Tle;
            m_Segment = Segment;
        }

        /// <summary>
        /// The segment of a tile that this wall is placed on.
        /// </summary>
        public TileSegment Segment
        {
            get { return m_Segment; }
        }

        enum DiagonalSideSelector
        {
            NotSpecified,
            Left,
            Top,
            Right,
            Bottom
        }

        enum ShearPlacement
        {
            Upper = 1,
            Lower = 2,
            Both = 3
        }
    }
}
