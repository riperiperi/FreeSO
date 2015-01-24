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
using System.Linq;
using System.Text;
using TSO_CityServer.Network;
using Nancy;

namespace TSO_CityServer
{
	/// <summary>
	/// Module that dynamically generates a webpage with statistics using Nancy.
	/// </summary>
	public class WebModule : NancyModule
	{
		public WebModule()
		{
			Get["/"] = parameters =>
			{
				TimeSpan RunningTime = (TimeSpan)(DateTime.Now - NetworkFacade.StartTime);

				return "<b>City:</b> " + GlobalSettings.Default.CityName + "</br>"
					+ "<b>Players currently online:</b> " + NetworkFacade.CurrentSession.PlayersInSession + "</br>"
					+ "<b>Running time:</b> " + RunningTime.Hours + " hours, " + RunningTime.Minutes + " minutes, " +
					+ RunningTime.Seconds + " seconds. </br></br>"
					+ "<b>Proudly powered by</b> <a href=\"http://nancyfx.org/\">Nancy</a></br>";
			};
		}
	}
}
