/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
