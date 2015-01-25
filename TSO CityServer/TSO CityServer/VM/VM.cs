/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TSO_CityServer.Network;

namespace TSO_CityServer.VM
{
	public class VM
	{
		public VMClock Clock { get; internal set; }
		private long m_LastTick = 0;
		private long TickInterval = 33 * TimeSpan.TicksPerMillisecond;
		private TimeSpan RunningTime = (TimeSpan)(DateTime.Now - NetworkFacade.StartTime);

		/// <summary>
		/// Performs VM initialization.
		/// </summary>
		public void Init()
		{
			Clock.TicksPerMinute = 10; //1 minute per 3 irl seconds
		}

		public void Update()
		{
			RunningTime = (TimeSpan)(DateTime.Now - NetworkFacade.StartTime);

			if (m_LastTick == 0 || (RunningTime.Ticks - m_LastTick) >= TickInterval)
				Tick();
		}

		private void Tick()
		{
			Clock.Tick();

			m_LastTick = RunningTime.Ticks;
		}
	}
}
