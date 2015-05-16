/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the CityDatamodel.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CityDataModel.Entities
{
	public class HouseAccess
	{
		private DataAccess Context;

        public HouseAccess(DataAccess context)
        {
            this.Context = context;
        }

		/// <summary>
		/// Returns the first house for a specific character GUID.
		/// </summary>
		/// <param name="GUID">A Guid instance for a character.</param>
		/// <returns>IQueryable instance containing the first house for the GUID.</returns>
		public House GetForCharacterGUID(Guid GUID)
		{
			return Context.Context.Characters.FirstOrDefault(x => x.GUID == GUID).HouseHouse;
		}

		/// <summary>
		/// Returns the first house found for the given coordinates, or a default value.
		/// </summary>
		/// <param name="X">X coordinate of house.</param>
		/// <param name="Y">Y coordinate of house.</param>
		/// <returns>The house found, or a House instance with default values.</returns>
		public House GetForPosition(int X, int Y)
		{
			return Context.Context.Characters.FirstOrDefault(x => x.HouseHouse.X == X && x.HouseHouse.Y == Y).HouseHouse;
		}
	}
}
