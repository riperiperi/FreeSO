using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using CityDataModel;
using TSO.Vitaboy;
using ProtocolAbstractionLibraryD;
using GonzoNet;
using GonzoNet.Encryption;

namespace TSO_CityServer.Network
{
    class ClientPacketHandlers
    {
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

                using (var db = DataAccess.Get())
                {
                    byte KeyLength = (byte)P.ReadByte();
                    byte[] EncKey = new byte[KeyLength];
                    P.Read(EncKey, 0, KeyLength);
                    Client.ClientEncryptor = new ARC4Encryptor(Convert.ToBase64String(HashBuf), EncKey);
                    Client.ClientEncryptor.Username = AccountName;

                    string Token = P.ReadString();
                    string GUID = "";
                    int AccountID = 0;

                    foreach (ClientToken CToken in NetworkFacade.TransferringClients.GetList())
                    {
                        if (CToken.ClientIP == Client.RemoteIP)
                        {
                            if (CToken.Token == Token)
                            {
                                PacketStream SuccessPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, (int)(PacketHeaders.ENCRYPTED + 1));
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
                    characterModel.LastCached = Char.Timestamp;
                    characterModel.GUID = Char.GUID;
                    characterModel.HeadOutfitID = (long)Char.HeadOutfitID;
                    characterModel.BodyOutfitID = (long)Char.BodyOutfitID;
                    characterModel.AccountID = AccountID;
                    characterModel.AppearanceType = (int)Char.Appearance;

                    var status = db.Characters.CreateCharacter(characterModel);
                }

                //Invalid token, should never occur...
                if (!ClientAuthenticated)
                {
                    PacketStream SuccessPacket = new PacketStream(0x65, (int)(PacketHeaders.ENCRYPTED + 1));
                    SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.GeneralError);
                    Client.SendEncrypted(0x64, SuccessPacket.ToArray());
                    Client.Disconnect();
                }
            }
            catch (Exception E)
            {
                Logger.LogDebug("Exception in HandleCharacterCreate: " + E.ToString());
            }
        }

        public static void HandleCityToken(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                bool ClientAuthenticated = false;

                using (var db = DataAccess.Get())
                {
                    byte HashLength = (byte)P.ReadByte();
                    byte[] HashBuf = new byte[HashLength];
                    P.Read(HashBuf, 0, HashLength);

                    byte KeyLength = (byte)P.ReadByte();
                    byte[] EncKey = new byte[KeyLength];
                    P.Read(EncKey, 0, KeyLength);
                    Client.ClientEncryptor = new ARC4Encryptor(Convert.ToBase64String(HashBuf), EncKey);

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
