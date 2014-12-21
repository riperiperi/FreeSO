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
            CityServerClient CityClient = (CityServerClient)Client;

            Logger.LogInfo("CityServer logged in!\r\n");

            string Name = P.ReadString();
            string Description = P.ReadString();
            string IP = P.ReadString();
            int Port = P.ReadInt32();
            CityInfoStatus Status = (CityInfoStatus)P.ReadByte();
            ulong Thumbnail = P.ReadUInt64();
            string UUID = P.ReadString();
            ulong Map = P.ReadUInt64();

            CityInfo Info = new CityInfo(Name, Description, Thumbnail, UUID, Map, IP, Port);
            Info.Status = Status;
            CityClient.ServerInfo = Info;

            //Client instance changed, so update it...
            NetworkFacade.CServerListener.UpdateClient(CityClient);
        }

        public static void HandlePulse(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                CityServerClient CityClient = (CityServerClient)Client;

                if (CityClient.ServerInfo != null)
                    CityClient.ServerInfo.Online = true;

                CityClient.LastPulseReceived = DateTime.Now;

                NetworkFacade.CServerListener.UpdateClient(CityClient);
            }
            catch (Exception E)
            {
                Logger.LogDebug("Exception in HandlePulse:\r\n" + E.ToString());
            }
        }
    }
}
