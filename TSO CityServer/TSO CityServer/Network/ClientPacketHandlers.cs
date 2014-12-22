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

            if (Enc == null)
                Enc = new AESEncryptor("");

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

        /// <summary>
        /// Client wanted to create a new character.
        /// </summary>
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

                using (DataAccess db = DataAccess.Get())
                {
                    string Token = P.ReadString();
                    string GUID = "";
                    int AccountID = 0;

                    foreach (ClientToken CToken in NetworkFacade.TransferringClients)
                    {
                        if (CToken.ClientIP == Client.RemoteIP)
                        {
                            if (CToken.Token.Equals(Token, StringComparison.CurrentCultureIgnoreCase))
                            {
                                PacketStream SuccessPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, 0);
                                SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.Success);
                                Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY, SuccessPacket.ToArray());
                                ClientAuthenticated = true;

                                GUID = CToken.CharacterGUID;
                                AccountID = CToken.AccountID;

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

                            break;
                        }
                    }
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
        /// Received client token.
        /// </summary>
        public static void HandleCityToken(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                bool ClientAuthenticated = false;

                using (DataAccess db = DataAccess.Get())
                {
                    string Token = P.ReadString();

                    foreach (ClientToken Tok in NetworkFacade.TransferringClients)
                    {
                        //Token matched, so client must have logged in through login server first.
                        if (Tok.Token.Equals(Token, StringComparison.InvariantCultureIgnoreCase))
                        {
                            ClientAuthenticated = true;

                            Character Char = db.Characters.GetForCharacterGUID(new Guid(Tok.CharacterGUID));
                            if (Char != null)
                            {
                                NetworkFacade.CurrentSession.AddPlayer(Client, Char);

                                PacketStream SuccessPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
                                SuccessPacket.WriteByte((byte)CityTransferStatus.Success);
                                Client.SendEncrypted((byte)PacketType.CITY_TOKEN, SuccessPacket.ToArray());

                                break;
                            }
                            else
                            {
                                ClientAuthenticated = false;
                                break;
                            }
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

        /// <summary>
        /// Player sent a letter to another player.
        /// </summary>
        public static void HandlePlayerSentLetter(NetworkClient Client, ProcessedPacket Packet)
        {
            string GUID = Packet.ReadPascalString();
            string Subject = Packet.ReadPascalString();
            string Msg = Packet.ReadPascalString();

            NetworkClient SendTo = NetworkFacade.CurrentSession.GetPlayersClient(GUID);

            if (SendTo != null)
            {
                ClientPacketSenders.SendPlayerReceivedLetter(SendTo, Subject, Msg,
                    NetworkFacade.CurrentSession.GetPlayer(GUID).Name);
            }
            else
            {
                //TODO: Error handling.
            }
        }
    }
}
