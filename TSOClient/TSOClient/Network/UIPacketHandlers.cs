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
                if (!File.Exists("CharacterCache\\Sims.tempcache"))
                {
                    //The charactercache didn't exist, so send the current time, which is
                    //newer than the server's stamp. This will cause the server to send the entire cache.
                    UIPacketSenders.SendCharacterInfoRequest(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"));
                }
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

                    FreshSims.Add(FreshSim);
                }

                NetworkFacade.Avatars = FreshSims;
                CacheSims(FreshSims);
            }

            PacketStream CityInfoRequest = new PacketStream(0x06, 0);
            CityInfoRequest.WriteByte(0x00); //Dummy

            Client.SendEncrypted((byte)PacketType.CITY_LIST, CityInfoRequest.ToArray());
        }

        public static void OnCityInfoResponse(ProcessedPacket Packet)
        {
            byte NumCities = (byte)Packet.ReadByte();

            for (int i = 0; i < NumCities; i++)
            {
                string Name = Packet.ReadString();
                string Description = Packet.ReadString();
                string IP = Packet.ReadString();
                int Port = Packet.ReadInt32();
                CityInfoStatus Status = (CityInfoStatus)Packet.ReadByte();
                ulong Thumbnail = Packet.ReadUInt64();
                string UUID = Packet.ReadString();

                CityInfo Info = new CityInfo(Name, Description, Thumbnail, UUID, 0, IP, Port);
                NetworkFacade.Cities.Add(Info);
            }
        }

        /// <summary>
        /// Caches sims received from the LoginServer to the disk.
        /// </summary>
        /// <param name="FreshSims">A list of the sims received by the LoginServer.</param>
        private static void CacheSims(List<Sim> FreshSims)
        {
            if (!Directory.Exists("CharacterCache"))
                Directory.CreateDirectory("CharacterCache");

            BinaryWriter Writer = new BinaryWriter(File.Create("CharacterCache\\Sims.tempcache"));

            Writer.Write(FreshSims.Count);

            foreach (Sim S in FreshSims)
            {
                //Length of the current entry, so its skippable...
                Writer.Write((int)4 + S.GUID.Length + S.Timestamp.Length + S.Name.Length + S.Sex.Length);
                Writer.Write(S.CharacterID);
                Writer.Write(S.GUID);
                Writer.Write(S.Timestamp);
                Writer.Write(S.Name);
                Writer.Write(S.Sex);
            }

            if (File.Exists("CharacterCache\\Sims.cache"))
            {
                BinaryReader Reader = new BinaryReader(File.Open("CharacterCache\\Sims.cache", FileMode.Open));
                int NumSims = Reader.ReadInt32();

                List<Sim> UnchangedSims = new List<Sim>();

                if (NumSims > FreshSims.Count)
                {
                    if (NumSims == 2)
                    {
                        //Skips the first entry.
                        Reader.BaseStream.Position = Reader.ReadInt32();

                        Reader.ReadInt32(); //Length of second entry.
                        string GUID = Reader.ReadString();

                        Sim S = new Sim(GUID);

                        S.CharacterID = Reader.ReadInt32();
                        S.Timestamp = Reader.ReadString();
                        S.Name = Reader.ReadString();
                        S.Sex = Reader.ReadString();
                        UnchangedSims.Add(S);
                    }
                    else if (NumSims == 3)
                    {
                        //Skips the first entry.
                        Reader.BaseStream.Position = Reader.ReadInt32();

                        Reader.ReadInt32(); //Length of second entry.
                        string GUID = Reader.ReadString();

                        Sim S = new Sim(GUID);

                        S.CharacterID = Reader.ReadInt32();
                        S.Timestamp = Reader.ReadString();
                        S.Name = Reader.ReadString();
                        S.Sex = Reader.ReadString();
                        UnchangedSims.Add(S);

                        Reader.ReadInt32(); //Length of third entry.
                        S.CharacterID = Reader.ReadInt32();
                        S.Timestamp = Reader.ReadString();
                        S.Name = Reader.ReadString();
                        S.Sex = Reader.ReadString();
                        UnchangedSims.Add(S);
                    }

                    Reader.Close();

                    foreach (Sim S in UnchangedSims)
                    {
                        //Length of the current entry, so its skippable...
                        Writer.Write((int)4 + S.Timestamp.Length + S.Name.Length + S.Sex.Length);
                        Writer.Write(S.CharacterID);
                        Writer.Write(S.Timestamp);
                        Writer.Write(S.Name);
                        Writer.Write(S.Sex);
                    }
                }
            }

            Writer.Close();

            if (File.Exists("CharacterCache\\Sims.cache"))
                File.Delete("CharacterCache\\Sims.cache");

            File.Move("CharacterCache\\Sims.tempcache", "CharacterCache\\Sims.cache");
        }
    }
}
