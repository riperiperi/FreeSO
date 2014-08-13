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
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using CityDataModel;
using TSO.Vitaboy;
using ProtocolAbstractionLibraryD;
using GonzoNet;
using GonzoNet.Encryption;

namespace TSO_CityServer.Network
{
    class ClientPacketHandlers
    {
        public static void InitialClientConnect(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("Received InitialClientConnect!");

            PacketStream EncryptedPacket = new PacketStream((byte)PacketType.LOGIN_NOTIFY_CITY, 0);
            EncryptedPacket.WriteHeader();

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
            Enc.PublicKey = P.ReadBytes((P.ReadByte()));
            Enc.NOnce = P.ReadBytes((P.ReadByte()));
            Enc.PrivateKey = NetworkFacade.ServerPrivateKey;
            Client.ClientEncryptor = Enc;
            NetworkFacade.NetworkListener.UpdateClient(Client);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write(Enc.Challenge, 0, Enc.Challenge.Length);
            Writer.Flush();

            byte[] EncryptedData = StaticStaticDiffieHellman.Encrypt(NetworkFacade.ServerPrivateKey,
                System.Security.Cryptography.ECDiffieHellmanCngPublicKey.FromByteArray(Enc.PublicKey, 
                System.Security.Cryptography.CngKeyBlobFormat.EccPublicBlob), Enc.NOnce, StreamToEncrypt.ToArray());

            EncryptedPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED +
                (1 + NetworkFacade.ServerPublicKey.Length) +
                (1 + EncryptedData.Length)));

            EncryptedPacket.WriteByte((byte)NetworkFacade.ServerPublicKey.Length);
            EncryptedPacket.WriteBytes(NetworkFacade.ServerPublicKey);
            EncryptedPacket.WriteByte((byte)EncryptedData.Length);
            EncryptedPacket.WriteBytes(EncryptedData);

            Client.Send(EncryptedPacket.ToArray());
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket P)
        {
            PacketStream OutPacket;

            byte[] CResponse = P.ReadBytes(P.ReadByte());

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;

            if (Enc.Challenge.SequenceEqual(CResponse))
            {
                OutPacket = new PacketStream((byte)PacketType.LOGIN_SUCCESS_CITY, 0);
                OutPacket.WriteByte(0x01);
                Client.SendEncrypted((byte)PacketType.LOGIN_SUCCESS_CITY, OutPacket.ToArray());

                Logger.LogInfo("Sent LOGIN_SUCCESS_CITY!");
            }
            else
            {
                OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE_CITY, 0);
                OutPacket.WriteByte(0x01);
                Client.SendEncrypted((byte)PacketType.LOGIN_FAILURE_CITY, OutPacket.ToArray());
                Client.Disconnect();

                Logger.LogInfo("Sent LOGIN_FAILURE_CITY!");
            }
        }

        public static void HandleCharacterCreate(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                Logger.LogInfo("Received CharacterCreate!");

                bool ClientAuthenticated = false;

                byte AccountStrLength = (byte)P.ReadByte();
                byte[] AccountNameBuf = new byte[AccountStrLength];
                P.Read(AccountNameBuf, 0, AccountStrLength);
                string AccountName = Encoding.ASCII.GetString(AccountNameBuf);

                byte HashLength = (byte)P.ReadByte();
                byte[] HashBuf = new byte[HashLength];
                P.Read(HashBuf, 0, HashLength);

                using (DataAccess db = DataAccess.Get())
                {
                    string Token = P.ReadString();
                    string GUID = "";
                    int AccountID = 0;

                    foreach (ClientToken CToken in NetworkFacade.TransferringClients.GetList())
                    {
                        if (CToken.ClientIP == Client.RemoteIP)
                        {
                            if (CToken.Token == Token)
                            {
                                PacketStream SuccessPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, 0);
                                SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.Success);
                                Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY, SuccessPacket.ToArray());
                                ClientAuthenticated = true;

                                GUID = CToken.CharacterGUID;
                                AccountID = CToken.AccountID;
                            }

                            break;
                        }
                    }

                    Sim Char = new Sim(new Guid(GUID));
                    Char.Timestamp = P.ReadPascalString();
                    Char.Name = P.ReadPascalString();
                    Char.Sex = P.ReadPascalString();
                    Char.Description = P.ReadPascalString();
                    Char.HeadOutfitID = P.ReadUInt64();
                    Char.BodyOutfitID = P.ReadUInt64();
                    Char.Appearance = (AppearanceType)P.ReadByte();
                    Char.CreatedThisSession = true;

                    var characterModel = new Character();
                    characterModel.Name = Char.Name;
                    characterModel.Sex = Char.Sex;
                    characterModel.Description = Char.Description;
                    characterModel.LastCached = ProtoHelpers.ParseDateTime(Char.Timestamp);
                    characterModel.GUID = Char.GUID;
                    characterModel.HeadOutfitID = (long)Char.HeadOutfitID;
                    characterModel.BodyOutfitID = (long)Char.BodyOutfitID;
                    characterModel.AccountID = AccountID;
                    characterModel.AppearanceType = (int)Char.Appearance;

                    NetworkFacade.CurrentSession.AddPlayer(Client, characterModel);

                    var status = db.Characters.CreateCharacter(characterModel);
                }

                //Invalid token, should never occur...
                if (!ClientAuthenticated)
                {
                    PacketStream FailPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, (int)(PacketHeaders.ENCRYPTED + 1));
                    FailPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.GeneralError);
                    Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, FailPacket.ToArray());
                    Client.Disconnect();
                }
            }
            catch (Exception E)
            {
                Logger.LogDebug("Exception in HandleCharacterCreate: " + E.ToString());
                Client.Disconnect();
            }
        }

        /// <summary>
        /// Received client token from login server.
        /// </summary>
        public static void HandleCityToken(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                bool ClientAuthenticated = false;

                using (DataAccess db = DataAccess.Get())
                {
                    byte HashLength = (byte)P.ReadByte();
                    byte[] HashBuf = new byte[HashLength];
                    P.Read(HashBuf, 0, HashLength);

                    string Token = P.ReadString();

                    foreach (ClientToken Tok in NetworkFacade.TransferringClients.GetList())
                    {
                        if (Tok.Token == Token)
                        {
                            ClientAuthenticated = true;
                            PacketStream SuccessPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
                            SuccessPacket.WriteByte((byte)CityTransferStatus.Success);
                            Client.SendEncrypted((byte)PacketType.CITY_TOKEN, SuccessPacket.ToArray());
                        }
                    }

                    if (!ClientAuthenticated)
                    {
                        PacketStream ErrorPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
                        ErrorPacket.WriteByte((byte)CityTransferStatus.GeneralError);
                        Client.SendEncrypted((byte)PacketType.CITY_TOKEN, ErrorPacket.ToArray());
                    }
                }
            }
            catch (Exception E)
            {
                Logger.LogDebug("Exception in HandleCityToken: " + E.ToString());
            }
        }
    }
}
