/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Contains static methods for handling incoming packets from CityServers.
    /// </summary>
    class CityServerPacketHandlers
    {
        /// <summary>
        /// A cityserver logged in!
        /// </summary>
        public static void HandleCityServerLogin(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("CityServer logged in!\r\n");

            string Name = P.ReadString();
            string Description = P.ReadString();
            string IP = P.ReadString();
            int Port = P.ReadInt32();
            CityInfoStatus Status = (CityInfoStatus)P.ReadByte();
            ulong Thumbnail = P.ReadUInt64();
            string UUID = P.ReadString();
            ulong Map = P.ReadUInt64();

            foreach(CityInfo Info in NetworkFacade.CServerListener.CityServers.GetConsumingEnumerable())
            {
                if(Info.Client == Client)
                {
                    Info.Name = Name;
                    Info.Description = Description;
                    Info.IP = IP;
                    Info.Port = Port;
                    Info.Status = Status;
                    Info.Thumbnail = Thumbnail;
                    Info.UUID = UUID;
                    Info.Map = Map;
                    Info.Client = Client;
                    Info.Online = true;
                    NetworkFacade.CServerListener.CityServers.Add(Info);

                    break;
                }

				NetworkFacade.CServerListener.CityServers.Add(Info);
            }
        }

        public static void HandlePulse(NetworkClient Client, ProcessedPacket P)
        {
            NetworkFacade.CServerListener.OnReceivedPulse(Client);
        }
    }
}
