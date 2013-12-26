/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using TSOClient.VM;
using TSOClient.Events;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Network
{
    /// <summary>
    /// Contains all the packethandlers in the game that are based on an interaction with the UI.
    /// I.E. a packet received because the user clicked on a UINetworkButton that sent a packet.
    /// </summary>
    public class UIPacketHandlers
    {
        /// <summary>
        /// Occurs when the client has been sucessfully authenticated by the loginserver.
        /// Called by UILoginDialog.cs.
        /// </summary>
        /// <param name="Client">The client that received the packet.</param>
        /// <param name="Packet">The packet that was received.</param>
        public static void OnInitLoginNotify(NetworkClient Client, ProcessedPacket Packet)
        {
            //Account was authenticated, so add the client to the player's account.
            PlayerAccount.Client = Client;

            if (!Directory.Exists("CharacterCache"))
            {
                Directory.CreateDirectory("CharacterCache");

                //The charactercache didn't exist, so send the current time, which is
                //newer than the server's stamp. This will cause the server to send the entire cache.
                UIPacketSenders.SendCharacterInfoRequest(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"));
            }
            else
            {
                if (!File.Exists("CharacterCache\\Sims.cache"))
                {
                    //The charactercache didn't exist, so send the current time, which is
                    //newer than the server's stamp. This will cause the server to send the entire cache.
                    UIPacketSenders.SendCharacterInfoRequest(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"));
                }
                else
                    UIPacketSenders.SendCharacterInfoRequest(Cache.GetDateCached());
            }
        }

        /// <summary>
        /// Occurs when the client was not authenticated by the loginserver.
        /// Called by UILoginDialog.cs.
        /// </summary>
        /// <param name="Client">The client that received the packet.</param>
        /// <param name="Packet">The packet that was received.</param>
        /// <param name="Screen">A UIScreen instance on which to display a messagebox to inform the player of the
        ///                      failure state.</param>
        public static void OnLoginFailResponse(ref NetworkClient Client, ProcessedPacket Packet)
        {
            EventObject Event;

            switch (Packet.ReadByte())
            {
                case 0x01:
                    Event = new EventObject(EventCodes.BAD_USERNAME);
                    EventSink.RegisterEvent(Event);
                    break;
                case 0x02:
                    Event = new EventObject(EventCodes.BAD_PASSWORD);
                    EventSink.RegisterEvent(Event);
                    break;
            }

            Client.Disconnect();
        }

        /// <summary>
        /// LoginServer sent information about the player's characters.
        /// </summary>
        /// <param name="Packet">The packet that was received.</param>
        public static void OnCharacterInfoResponse(ProcessedPacket Packet, NetworkClient Client)
        {
            //If the decrypted length == 1, it means that there were 0
            //characters that needed to be updated, or that the user
            //hasn't created any characters on his/her account yet.
            //Since the Packet.Length property is equal to the length
            //of the encrypted data, it cannot be used to get the length
            //of the decrypted data.
            if (Packet.DecryptedLength > 1)
            {
                byte NumCharacters = (byte)Packet.ReadByte();
                List<Sim> FreshSims = new List<Sim>();

                for (int i = 0; i < NumCharacters; i++)
                {
                    int CharacterID = Packet.ReadInt32();

                    Sim FreshSim = new Sim(Packet.ReadString());
                    FreshSim.CharacterID = CharacterID;
                    FreshSim.Timestamp = Packet.ReadString();
                    FreshSim.Name = Packet.ReadString();
                    FreshSim.Sex = Packet.ReadString();
                    FreshSim.Description = Packet.ReadString();
                    FreshSim.HeadOutfitID = Packet.ReadUInt64();
                    FreshSim.BodyOutfitID = Packet.ReadUInt64();
                    FreshSim.AppearanceType = (SimsLib.ThreeD.AppearanceType)Packet.ReadByte();
                    FreshSim.ResidingCity = new CityInfo(Packet.ReadString(), "", Packet.ReadUInt64(), Packet.ReadString(),
                        Packet.ReadUInt64(), Packet.ReadString(), Packet.ReadInt32());

                    FreshSims.Add(FreshSim);
                }

                NetworkFacade.Avatars = FreshSims;
                Cache.CacheSims(FreshSims);
            }

            PacketStream CityInfoRequest = new PacketStream(0x06, 0);
            CityInfoRequest.WriteByte(0x00); //Dummy

            Client.SendEncrypted((byte)PacketType.CITY_LIST, CityInfoRequest.ToArray());
        }

        public static void OnCityInfoResponse(ProcessedPacket Packet)
        {
            byte NumCities = (byte)Packet.ReadByte();

            if (Packet.DecryptedLength > 1)
            {
                for (int i = 0; i < NumCities; i++)
                {
                    string Name = Packet.ReadString();
                    string Description = Packet.ReadString();
                    string IP = Packet.ReadString();
                    int Port = Packet.ReadInt32();
                    byte StatusByte = (byte)Packet.ReadByte();
                    CityInfoStatus Status = (CityInfoStatus)StatusByte;
                    ulong Thumbnail = Packet.ReadUInt64();
                    string UUID = Packet.ReadString();
                    ulong Map = Packet.ReadUInt64();

                    CityInfo Info = new CityInfo(Name, Description, Thumbnail, UUID, Map, IP, Port);
                    Info.Online = true;
                    Info.Status = Status;
                    NetworkFacade.Cities.Add(Info);
                }
            }
        }

        public static void OnCharacterCreationStatus(NetworkClient Client, ProcessedPacket Packet)
        {
            CharacterCreationStatus CCStatus = (CharacterCreationStatus)Packet.ReadByte();

            switch (CCStatus)
            {
                case CharacterCreationStatus.Success:
                    Guid CharacterGUID = new Guid();

                    //CityToken didn't exist, so transition to CityServer hasn't happened yet.
                    if (PlayerAccount.CityToken == "")
                    {
                        CharacterGUID = new Guid(Packet.ReadPascalString());
                        PlayerAccount.CityToken = Packet.ReadPascalString();
                    }

                    NetworkFacade.Controller._OnCharacterCreationStatus(CCStatus);

                    if(PlayerAccount.CityToken == "")
                        PlayerAccount.CurrentlyActiveSim.AssignGUID(CharacterGUID.ToString());

                    break;
                case CharacterCreationStatus.ExceededCharacterLimit:
                    NetworkFacade.Controller._OnCharacterCreationStatus(CCStatus);
                    break;
                case CharacterCreationStatus.NameAlreadyExisted:
                    NetworkFacade.Controller._OnCharacterCreationStatus(CCStatus);
                    break;
                case CharacterCreationStatus.GeneralError:
                    NetworkFacade.Controller._OnCharacterCreationStatus(CCStatus);
                    break;
                default:
                    break;
            }
        }

        public static void OnCityToken(NetworkClient Client, ProcessedPacket Packet)
        {
            PlayerAccount.CityToken = Packet.ReadString();
        }

        public static void OnCityTokenResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            CityTransferStatus Status = (CityTransferStatus)Packet.ReadByte();

            switch (Status)
            {
                case CityTransferStatus.Success:
                    NetworkFacade.Controller._OnCityTokenResponse(Status);
                    break;
                case CityTransferStatus.GeneralError:
                    NetworkFacade.Controller._OnCityTokenResponse(Status);
                    break;
            }
        }
    }
}
