using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;
using GonzoNet;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;
using PDChat.Sims;

namespace PDChat
{
    public class PacketHandlers
    {
        public static void HandleLoginNotify(NetworkClient Client, ProcessedPacket Packet)
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
                PacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                if (!File.Exists(CacheDir + "\\Sims.cache"))
                {
                    //The charactercache didn't exist, so send the Unix epoch, which is
                    //older than the server's stamp. This will cause the server to send the entire cache.
                    PacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    string LastDateCached = Cache.GetDateCached();
                    if (LastDateCached == "")
                        PacketSenders.SendCharacterInfoRequest(new DateTime(1970, 1, 1, 0, 0, 0, 0).ToString(CultureInfo.InvariantCulture));
                    else
                        PacketSenders.SendCharacterInfoRequest(LastDateCached);
                }
            }
        }

        /// <summary>
        /// LoginServer sent information about the player's characters.
        /// </summary>
        /// <param name="Packet">The packet that was received.</param>
        public static void OnCharacterInfoResponse(ProcessedPacket Packet, NetworkClient Client)
        {
            byte NumCharacters = (byte)Packet.ReadByte();
            byte NewCharacters = (byte)Packet.ReadByte();

            List<Sim> FreshSims = new List<Sim>();

            for (int i = 0; i < NewCharacters; i++)
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
                FreshSim.Appearance = (AppearanceType)Packet.ReadByte();
                FreshSim.ResidingCity = new CityInfo(Packet.ReadString(), "", Packet.ReadUInt64(), Packet.ReadString(),
                    Packet.ReadUInt64(), Packet.ReadString(), Packet.ReadInt32());

                FreshSims.Add(FreshSim);
            }

            if ((NewCharacters < 3) && (NewCharacters > 0))
            {
                FreshSims = Cache.LoadCachedSims(FreshSims);
                NetworkFacade.Avatars = FreshSims;
                Cache.CacheSims(FreshSims);
            }

            if (NewCharacters == 0 && NumCharacters > 0)
                NetworkFacade.Avatars = Cache.LoadAllSims();
            else if (NewCharacters == 3 && NumCharacters == 3)
            {
                NetworkFacade.Avatars = FreshSims;
                Cache.CacheSims(FreshSims);
            }
            else if (NewCharacters == 0 && NumCharacters == 0)
                //Make sure if sims existed in the cache, they are deleted (because they didn't exist in DB).
                Cache.DeleteCache();

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
    }
}
