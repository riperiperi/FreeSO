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
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using TSOClient.Code.UI.Controls;
using TSOClient.Events;
using TSOClient.Network.Events;
using GonzoNet;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;
using TSO.Vitaboy;

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
        public static void OnLoginNotify(NetworkClient Client, ProcessedPacket Packet)
        {
            //Should this be stored for permanent access?
            byte[] ServerPublicKey = Packet.ReadBytes(Packet.ReadByte());
            byte[] EncryptedData = Packet.ReadBytes(Packet.ReadByte());

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
            Enc.PublicKey = ServerPublicKey;
            Client.ClientEncryptor = Enc;
            NetworkFacade.Client.ClientEncryptor = Enc;

            ECDiffieHellmanCng PrivateKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.PrivateKey;
            byte[] NOnce = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            byte[] ChallengeResponse = StaticStaticDiffieHellman.Decrypt(PrivateKey,
                ECDiffieHellmanCngPublicKey.FromByteArray(ServerPublicKey, CngKeyBlobFormat.EccPublicBlob),
                NOnce, EncryptedData);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);

            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);

            Writer.Write(Client.ClientEncryptor.Username);
            Writer.Write((byte)PlayerAccount.Hash.Length);
            Writer.Write(PlayerAccount.Hash);
            Writer.Flush();

            //Encrypt data using key and IV from server, hoping that it'll be decrypted correctly at the other end...
            Client.SendEncrypted((byte)PacketType.CHALLENGE_RESPONSE, StreamToEncrypt.ToArray());
        }

        public static void OnLoginSuccessResponse(ref NetworkClient Client, ProcessedPacket Packet)
        {
            string CacheDir = GlobalSettings.Default.DocumentsPath + "CharacterCache\\" + PlayerAccount.Username;

            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);

                //The charactercache didn't exist, so send the Unix epoch, which is
                //older than the server's stamp. This will cause the server to send the entire cache.
                UIPacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                if (!File.Exists(CacheDir + "\\Sims.cache"))
                {
                    //The charactercache didn't exist, so send the Unix epoch, which is
                    //older than the server's stamp. This will cause the server to send the entire cache.
                    UIPacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    string LastDateCached = Cache.GetDateCached();
                    if (LastDateCached == "")
                        UIPacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
                    else
                        UIPacketSenders.SendCharacterInfoRequest(LastDateCached);
                }
            }
        }

        /// <summary>
        /// Occurs when the client was not authenticated by the loginserver.
        /// Called by UILoginDialog.cs.
        /// </summary>
        /// <param name="Client">The client that received the packet.</param>
        /// <param name="Packet">The packet that was received.</param>
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

        public static void OnInvalidVersionResponse(ref NetworkClient Client, ProcessedPacket Packet)
        {
            Client.Disconnect();
        }

        /// <summary>
        /// LoginServer sent information about the player's characters.
        /// </summary>
        /// <param name="Packet">The packet that was received.</param>
        public static void OnCharacterInfoResponse(ProcessedPacket Packet, NetworkClient Client)
        {
            byte NumCharacters = (byte)Packet.ReadByte();

            //If the decrypted length == 1, it means that there were 0
            //characters that needed to be updated, or that the user
            //hasn't created any characters on his/her account yet.
            //Since the Packet. Length property is equal to the length
            //of the encrypted data, it cannot be used to get the length
            //of the decrypted data.
            if (Packet.DecryptedLength > 1)
            {
                List<UISim> FreshSims = new List<UISim>();

                for (int i = 0; i < NumCharacters; i++)
                {
                    int CharacterID = Packet.ReadInt32();

                    UISim FreshSim = new UISim(Packet.ReadString(), false);
                    FreshSim.CharacterID = CharacterID;
                    FreshSim.Timestamp = Packet.ReadString();
                    FreshSim.Name = Packet.ReadString();
                    FreshSim.Sex = Packet.ReadString();
                    FreshSim.Description = Packet.ReadString();
                    FreshSim.HeadOutfitID = Packet.ReadUInt64();
                    FreshSim.BodyOutfitID = Packet.ReadUInt64();
                    FreshSim.Avatar.Appearance = (AppearanceType)Packet.ReadByte();
                    FreshSim.ResidingCity = new CityInfo(Packet.ReadString(), "", Packet.ReadUInt64(), Packet.ReadString(),
                        Packet.ReadUInt64(), Packet.ReadString(), Packet.ReadInt32());

                    FreshSims.Add(FreshSim);
                }

                if (NumCharacters < 3)
                    FreshSims = Cache.LoadCachedSims(FreshSims);
                NetworkFacade.Avatars = FreshSims;
                Cache.CacheSims(FreshSims);
            }
            else
            {
                if (NumCharacters > 0)
                    NetworkFacade.Avatars = Cache.LoadAllSims();
                else //No characters existed in the DB, which means whatever existed in the cache is bogus.
                    Cache.DeleteCache();
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

        /// <summary>
        /// Received CharacterCreation packet from LoginServer.
        /// </summary>
        /// <returns>The result of the character creation.</returns>
        public static CharacterCreationStatus OnCharacterCreationProgress(NetworkClient Client, ProcessedPacket Packet)
        {
            CharacterCreationStatus CCStatus = (CharacterCreationStatus)Packet.ReadByte();

            if (CCStatus == CharacterCreationStatus.Success)
            {
                Guid CharacterGUID = new Guid();

                CharacterGUID = new Guid(Packet.ReadPascalString());
                PlayerAccount.CityToken = Packet.ReadPascalString();
                PlayerAccount.CurrentlyActiveSim.AssignGUID(CharacterGUID.ToString());
            }

            return CCStatus;
        }

        /// <summary>
        /// Received initial packet from CityServer.
        /// </summary>
        public static void OnLoginNotifyCity(NetworkClient Client, ProcessedPacket Packet)
        {
            LogThis.Log.LogThis("Received OnLoginNotifyCity!", LogThis.eloglevel.info);

            //Should this be stored for permanent access?
            byte[] ServerPublicKey = Packet.ReadBytes(Packet.ReadByte());
            byte[] EncryptedData = Packet.ReadBytes(Packet.ReadByte());

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
            Enc.PublicKey = ServerPublicKey;
            Client.ClientEncryptor = Enc;
            NetworkFacade.Client.ClientEncryptor = Enc;

            ECDiffieHellmanCng PrivateKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.PrivateKey;
            byte[] NOnce = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            byte[] ChallengeResponse = StaticStaticDiffieHellman.Decrypt(PrivateKey,
                ECDiffieHellmanCngPublicKey.FromByteArray(ServerPublicKey, CngKeyBlobFormat.EccPublicBlob),
                NOnce, EncryptedData);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);

            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);
            Writer.Flush();

            //Encrypt data using key and IV from server, hoping that it'll be decrypted correctly at the other end...
            Client.SendEncrypted((byte)PacketType.CHALLENGE_RESPONSE, StreamToEncrypt.ToArray());
        }

        /// <summary>
        /// Received CharacterCreation packet from CityServer.
        /// </summary>
        /// <returns>The result of the character creation.</returns>
        public static CharacterCreationStatus OnCharacterCreationStatus(NetworkClient Client, ProcessedPacket Packet)
        {
            LogThis.Log.LogThis("Received OnCharacterCreationStatus!", LogThis.eloglevel.info);

            CharacterCreationStatus CCStatus = (CharacterCreationStatus)Packet.ReadByte();

            return CCStatus;
        }

        /// <summary>
        /// Received from the LoginServer in response to a CITY_TOKEN_REQUEST packet.
        /// </summary>
        public static void OnCityToken(NetworkClient Client, ProcessedPacket Packet)
        {
            PlayerAccount.CityToken = Packet.ReadPascalString();
        }

        /// <summary>
        /// Received from the CityServer in response to a CITY_TOKEN packet.
        /// </summary>
        public static CityTransferStatus OnCityTokenResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            LogThis.Log.LogThis("Received OnCityTokenResponse", LogThis.eloglevel.info);

            CityTransferStatus Status = (CityTransferStatus)Packet.ReadByte();
            return Status;
        }

        /// <summary>
        /// Received from the LoginServer in response to a RETIRE_CHARACTER packet.
        /// </summary>
        /// <returns>Name of character that was retired.</returns>
        public static string OnCharacterRetirement(NetworkClient Client, ProcessedPacket Packet)
        {
            string GUID = Packet.ReadPascalString();
            return GUID;
        }
    }
}
