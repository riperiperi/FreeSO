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
using System.IO;

namespace TSO_CityServer
{
    public class ConfigurationManager
    {
        /// <summary>
        /// Loads the city configuration file.
        /// </summary>
        /// <returns>False if it doesn't exist.</returns>
        public static bool LoadCityConfig()
        {
            try
            {
                string[] Lines = File.ReadAllLines("ServerConfig.ini");

                foreach (string Line in Lines)
                {
                    if (!Line.StartsWith("//"))
                    {
                        if(Line.StartsWith("ID: "))
                            GlobalSettings.Default.ServerID = Line.Replace("ID: ", "").Trim();
                        if (Line.StartsWith("Name: "))
                            GlobalSettings.Default.CityName = Line.Replace("Name: ", "").Trim();
                        else if (Line.StartsWith("Description: "))
                            GlobalSettings.Default.CityDescription = Line.Replace("Description: ", "").Trim().Replace("\r", "").
                                Replace("\n", "");
                        else if (Line.StartsWith("Thumbnail: "))
                            GlobalSettings.Default.CityThumbnail = Convert.ToUInt64(Line.Replace("Thumbnail: ", ""), 16);
                        else if (Line.StartsWith("Map: "))
                            GlobalSettings.Default.Map = Convert.ToUInt64(Line.Replace("Map: ", ""), 16);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
