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
using System.Linq;
using System.Diagnostics;
using CityDataModel;
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

			if (P.DecryptedSuccessfully)
			{
				int Length = P.ReadByte();
				byte[] CResponse;

				if (P.BufferLength >= Length)
					CResponse = P.ReadBytes(Length);
				else
					return;

				AESDecryptionArgs DecryptionArgs = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs;

				if (DecryptionArgs.Challenge.SequenceEqual(CResponse))
				{
					OutPacket = new PacketStream((byte)PacketType.LOGIN_SUCCESS_CITY, 0);
					OutPacket.WriteByte(0x01);
					Client.SendEncrypted((byte)PacketType.LOGIN_SUCCESS_CITY, OutPacket.ToArray());

					Logger.LogInfo("Sent LOGIN_SUCCESS_CITY!");
				}
				else
				{
					//Authentication failed, so send this packet unencrypted.
					OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE_CITY, 0);
					OutPacket.WriteHeader();
					OutPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + 1));
					OutPacket.WriteByte(0x01);
					Client.Send(OutPacket.ToArray());
					Client.Disconnect();

					Logger.LogInfo("Sent LOGIN_FAILURE_CITY!");
				}
			}
			else
			{
				//Authentication failed, so send this packet unencrypted.
				OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE_CITY, 0);
				OutPacket.WriteHeader();
				OutPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + 1));
				OutPacket.WriteByte(0x01);
				Client.Send(OutPacket.ToArray());
				Client.Disconnect();

				Debug.WriteLine("HandleChallengeResponse - decryption failed!");
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
				if (P.BufferLength >= AccountStrLength)
				{
					P.Read(AccountNameBuf, 0, AccountStrLength);
					string AccountName = Encoding.ASCII.GetString(AccountNameBuf);
				}
				else
					return;

				using (DataAccess db = DataAccess.Get())
				{
					//No need to check for empty string here, because all it will do is have ClientAuthenticated be false
					string Token = P.ReadString();
					string GUID = "";
					int AccountID = 0;

					ClientToken TokenToRemove = NetworkFacade.GetClientToken(Client.RemoteIP);

					//Just in case...
					if (TokenToRemove != null)
					{
						if (TokenToRemove.Token.Equals(Token, StringComparison.CurrentCultureIgnoreCase))
						{
							PacketStream SuccessPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, 0);
							SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.Success);

							House[] Houses = NetworkFacade.CurrentSession.GetHousesInSession();
							SuccessPacket.WriteUInt16((ushort)Houses.Length);

							//Ho, ho, ho...
							foreach (House Ho in Houses)
							{
								SuccessPacket.WriteInt32(Ho.HouseID);
								SuccessPacket.WriteUInt16((ushort)Ho.X);
								SuccessPacket.WriteUInt16((ushort)Ho.Y);
								SuccessPacket.WriteByte((byte)Ho.Flags); //Might have to save this as unsigned in DB?
								SuccessPacket.WriteInt32(Ho.Cost);
							}

							Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY, SuccessPacket.ToArray());
							ClientAuthenticated = true;

							GUID = TokenToRemove.CharacterGUID;
							AccountID = TokenToRemove.AccountID;

							Sim Char = new Sim(new Guid(GUID));
							Char.Timestamp = P.ReadString();
							Char.Name = P.ReadString();
							Char.Sex = P.ReadString();
							Char.Description = P.ReadString();
							Char.HeadOutfitID = P.ReadUInt64();
							Char.BodyOutfitID = P.ReadUInt64();
							Char.Appearance = (AppearanceType)P.ReadByte();
							Char.CreatedThisSession = true;

							//These are going into DB, so be nazi. Sieg heil!
							if (Char.Timestamp == string.Empty || Char.Name == string.Empty || Char.Sex == string.Empty ||
								Char.Description == string.Empty)
							{
								//TODO: Tell loginserver to clean up DB?
								ClientAuthenticated = false;
							}

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
					}

					NetworkFacade.TransferringClients.TryRemove(out TokenToRemove);
				}

				//Invalid token, should never occur...
				if (!ClientAuthenticated)
				{
					NetworkFacade.CurrentSession.RemovePlayer(Client);

					PacketStream FailPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, (int)(PacketHeaders.ENCRYPTED + 1));
					FailPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.GeneralError);
					Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, FailPacket.ToArray());
					Client.Disconnect();
				}
			}
			catch (Exception E)
			{
				Debug.WriteLine("Exception in HandleCharacterCreate: " + E.ToString());
				Logger.LogDebug("Exception in HandleCharacterCreate: " + E.ToString());
				
				PacketStream FailPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, (int)(PacketHeaders.ENCRYPTED + 1));
				FailPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.GeneralError);
				Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY_FAILED, FailPacket.ToArray());
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
				//bool ClientAuthenticated = false;
				ClientToken TokenToRemove = new ClientToken();

				using (DataAccess db = DataAccess.Get())
				{
					string Token = P.ReadString();
					ClientToken Tok;

					if (Token == string.Empty)
						return;

					Tok = NetworkFacade.GetClientToken(new Guid(Token));

					if (Tok != null)
					{
						//ClientAuthenticated = true;
						TokenToRemove = Tok;

						Character Char = db.Characters.GetForCharacterGUID(new Guid(Tok.CharacterGUID));
						if (Char != null)
						{
							NetworkFacade.CurrentSession.AddPlayer(Client, Char);

							PacketStream SuccessPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
							SuccessPacket.WriteByte((byte)CityTransferStatus.Success);

							House[] Houses = NetworkFacade.CurrentSession.GetHousesInSession();
							SuccessPacket.WriteUInt16((ushort)Houses.Length);

							//Ho, ho, ho...
							foreach (House Ho in Houses)
							{
								SuccessPacket.WriteInt32(Ho.HouseID);
								SuccessPacket.WriteUInt16((ushort)Ho.X);
								SuccessPacket.WriteUInt16((ushort)Ho.Y);
								SuccessPacket.WriteByte((byte)Ho.Flags); //Might have to save this as unsigned in DB?
								SuccessPacket.WriteInt32(Ho.Cost);
							}

							Client.SendEncrypted((byte)PacketType.CITY_TOKEN, SuccessPacket.ToArray());
						}
						/*else
						{
							ClientAuthenticated = false;
							break;
						}*/
					}

					NetworkFacade.TransferringClients.TryRemove(out TokenToRemove);

					//This is not really valid anymore, because if the token doesn't exist yet,
					//the client will now receive it when it arrives - see LoginPacketHandlers.cs
					// - HandleClientToken()
					/*if (!ClientAuthenticated)
					{
						PacketStream ErrorPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
						ErrorPacket.WriteByte((byte)CityTransferStatus.GeneralError);
						Client.SendEncrypted((byte)PacketType.CITY_TOKEN, ErrorPacket.ToArray());
					}*/
				}
			}
			catch (Exception E)
			{
				Logger.LogDebug("Exception in HandleCityToken: " + E.ToString());
				Debug.WriteLine("Exception in HandleCityToken: " + E.ToString());

				PacketStream ErrorPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
				ErrorPacket.WriteByte((byte)CityTransferStatus.GeneralError);
				Client.SendEncrypted((byte)PacketType.CITY_TOKEN, ErrorPacket.ToArray());
			}
		}

		/// <summary>
		/// Player sent a letter to another player.
		/// </summary>
		public static void HandlePlayerSentLetter(NetworkClient Client, ProcessedPacket Packet)
		{
			string GUID = Packet.ReadString();

			if (GUID == string.Empty)
				return;

			string Subject = Packet.ReadString();
			string Msg = Packet.ReadString();

			NetworkClient SendTo = NetworkFacade.CurrentSession.GetPlayersClient(GUID);
			Character FromChar = NetworkFacade.CurrentSession.GetPlayer(Client);

			if (SendTo != null)
			{
				if (FromChar != null)
					NetworkFacade.CurrentSession.SendPlayerReceivedLetter(SendTo, Subject, Msg, FromChar.Name);
			}
			else
			{
				//TODO: Error handling.
			}
		}

		/// <summary>
		/// Player (admin?) broadcast a letter.
		/// </summary>
		public static void HandleBroadcastLetter(NetworkClient Client, ProcessedPacket Packet)
		{
			string Subject = Packet.ReadString();
			string Msg = Packet.ReadString();

			NetworkFacade.CurrentSession.SendBroadcastLetter(Client, Subject, Msg);
		}

		/// <summary>
		/// Player requested the cost of a lot.
		/// </summary>
		public static void HandleLotCostRequest(NetworkClient Client, ProcessedPacket Packet)
		{
			ushort X = Packet.ReadUInt16();
			ushort Y = Packet.ReadUInt16();
			int LotID;

			using (DataAccess db = DataAccess.Get())
			{
				LotID = db.Houses.GetForPosition(X, Y).HouseID;
			}

			PacketStream LotCostPacket = new PacketStream((byte)PacketType.LOT_PURCHASE_OCCUPIED, 0);
			LotCostPacket.WriteUInt16(X);
			LotCostPacket.WriteUInt16(Y);
			LotCostPacket.WriteInt32(LotID);

			byte Flags = 0;

			using (DataAccess db = DataAccess.Get())
			{
				if (db.Houses.GetForPosition(X, Y).HouseID != 0)
				{
					if (!NetworkFacade.CurrentSession.IsLotOccupied(X, Y))
					{
						ProtoHelpers.SetBit(ref Flags, 0, true);  //Online.
						ProtoHelpers.SetBit(ref Flags, 1, false); //Spotlight, this will have to be checked against DB.
						ProtoHelpers.SetBit(ref Flags, 2, false); //Locked - is the house locked for public access?
						ProtoHelpers.SetBit(ref Flags, 3, true);  //Occupied.
						LotCostPacket.WriteByte(Flags);
					}
				}
				else
				{
					ProtoHelpers.SetBit(ref Flags, 0, true);  //Online.
					ProtoHelpers.SetBit(ref Flags, 1, false); //Spotlight, this will have to be checked against DB.
					ProtoHelpers.SetBit(ref Flags, 2, false); //Locked - is the house locked for public access?
					ProtoHelpers.SetBit(ref Flags, 3, false);  //Occupied.
					LotCostPacket.WriteByte(Flags);
				}
			}

			LotCostPacket.WriteInt32(NetworkFacade.LOT_COST); //TODO: Figure out a way to deal with this...
			Client.SendEncrypted((byte)PacketType.LOT_COST, LotCostPacket.ToArray());
		}

		/// <summary>
		/// A player sent a lot purchase request!
		/// </summary>
		public static void HandleLotPurchaseRequest(NetworkClient Client, ProcessedPacket Packet)
		{
			int X = Packet.ReadUInt16();
			int Y = Packet.ReadUInt16();

			if(!NetworkFacade.CurrentSession.IsLotOccupied(X, Y))
			{
				using (DataAccess db = DataAccess.Get())
				{
					if (db.Houses.GetForPosition(X, Y).HouseID != 0)
					{
						if (NetworkFacade.CurrentTerrain.IsLandBuildable(X, Y))
						{
							Guid CharGuid = NetworkFacade.CurrentSession.GetPlayer(Client).GUID;
							Character Char = db.Characters.GetForCharacterGUID(CharGuid);

							if (Char.Money >= NetworkFacade.LOT_COST)
							{
								Char.HouseHouse = new House();
								Char.HouseHouse.X = X;
								Char.HouseHouse.Y = Y;
								Char.Money -= NetworkFacade.LOT_COST;
							}
						}
						else
						{
							PacketStream UnbuildablePacket = new PacketStream((byte)PacketType.LOT_UNBUILDABLE, 0);
							UnbuildablePacket.WriteByte(0x00);
							Client.SendEncrypted((byte)PacketType.LOT_UNBUILDABLE, UnbuildablePacket.ToArray());
						}
					}
				}
			}
			else
			{
				PacketStream OccupiedPacket = new PacketStream((byte)PacketType.LOT_PURCHASE_OCCUPIED, 0);
				OccupiedPacket.WriteByte(0x00);
				Client.SendEncrypted((byte)PacketType.LOT_PURCHASE_OCCUPIED, OccupiedPacket.ToArray());
			}
		}
	}
}