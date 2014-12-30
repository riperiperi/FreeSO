/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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

            //GetConsumingEnumerable() should be used to modify a BlockingCollection<T>
            foreach (CityInfo Info in NetworkFacade.CServerListener.CityServers.GetConsumingEnumerable())
            {
                if (Info.Client == Client)
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