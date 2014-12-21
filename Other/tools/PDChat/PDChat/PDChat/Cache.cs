/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using PDChat.Sims;
using System.Linq;

namespace PDChat
{
    /// <summary>
    /// Cache for storing characters received by the login server.
    /// </summary>
    public class Cache
    {
        private static string CacheDir = GlobalSettings.Default.DocumentsPath + 
            "CharacterCache\\" + PlayerAccount.Username;

        public static void DeleteCache()
        {
            if (Directory.Exists(CacheDir))
                File.Delete(CacheDir + "\\Sims.cache");
        }

        /// <summary>
        /// Gets the last time sims were cached from the cache.
        /// </summary>
        /// <returns>A string representing the last time the sims were cached.</returns>
        public static string GetDateCached()
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);

            if (!File.Exists(CacheDir + "\\Sims.cache"))
                return "";

            BinaryReader Reader = new BinaryReader(File.Open(CacheDir + "\\Sims.cache", FileMode.Open));
            string LastCached = Reader.ReadString();
            Reader.Close();

            return LastCached;
        }

        /// <summary>
        /// Caches sims received from the LoginServer to the disk.
        /// </summary>
        /// <param name="FreshSims">A list of the sims received by the LoginServer.</param>
        public static void CacheSims(List<Sim> FreshSims)
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);

            using(BinaryWriter Writer = new BinaryWriter(File.Create(CacheDir + "\\Sims.tempcache")))
            {
                //Last time these sims were cached.
                Writer.Write(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss", CultureInfo.InvariantCulture));

                Writer.Write(FreshSims.Count);

                foreach (Sim S in FreshSims)
                {
                    //Length of the current entry, so its skippable...
                    Writer.Write((int)4 + S.GUID.ToString().Length + 4 + S.Timestamp.Length + S.Name.Length + S.Sex.Length +
                        S.Description.Length + 17 + S.ResidingCity.Name.Length + 8 + S.ResidingCity.UUID.ToString().Length + 8 + 
                        S.ResidingCity.IP.Length + 4);
                    Writer.Write(S.GUID.ToString());
                    Writer.Write(S.CharacterID);
                    Writer.Write(S.Timestamp);
                    Writer.Write(S.Name);
                    Writer.Write(S.Sex);
                    Writer.Write(S.Description);
                    Writer.Write(S.HeadOutfitID);
                    Writer.Write(S.BodyOutfitID);
                    Writer.Write((byte)S.Appearance);
                    Writer.Write(S.ResidingCity.Name);
                    Writer.Write(S.ResidingCity.Thumbnail);
                    Writer.Write(S.ResidingCity.UUID);
                    Writer.Write(S.ResidingCity.Map);
                    Writer.Write(S.ResidingCity.IP);
                    Writer.Write(S.ResidingCity.Port);
                }

                if (File.Exists(CacheDir + "\\Sims.cache"))
                {
                    using (BinaryReader Reader = new BinaryReader(File.Open(CacheDir + "\\Sims.cache", FileMode.Open)))
                    {
                        //Last time these sims were cached.
                        Reader.ReadString();
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
                                S.Description = Reader.ReadString();
                                S.HeadOutfitID = Reader.ReadUInt64();
                                S.BodyOutfitID = Reader.ReadUInt64();
                                S.Appearance = (AppearanceType)Reader.ReadByte();
                                S.ResidingCity = new ProtocolAbstractionLibraryD.CityInfo(Reader.ReadString(), "",
                                    Reader.ReadUInt64(), Reader.ReadString(), Reader.ReadUInt64(), Reader.ReadString(), 
                                    Reader.ReadInt32());
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
                                S.Description = Reader.ReadString();
                                S.HeadOutfitID = Reader.ReadUInt64();
                                S.BodyOutfitID = Reader.ReadUInt64();
                                S.Appearance = (AppearanceType)Reader.ReadByte();
                                S.ResidingCity = new ProtocolAbstractionLibraryD.CityInfo(Reader.ReadString(), "", 
                                    Reader.ReadUInt64(), Reader.ReadString(), Reader.ReadUInt64(), Reader.ReadString(), 
                                    Reader.ReadInt32());
                                UnchangedSims.Add(S);

                                Reader.ReadInt32(); //Length of third entry.
                                S.CharacterID = Reader.ReadInt32();
                                S.Timestamp = Reader.ReadString();
                                S.Name = Reader.ReadString();
                                S.Sex = Reader.ReadString();
                                S.Description = Reader.ReadString();
                                S.HeadOutfitID = Reader.ReadUInt64();
                                S.BodyOutfitID = Reader.ReadUInt64();
                                S.Appearance = (AppearanceType)Reader.ReadByte();
                                S.ResidingCity = new ProtocolAbstractionLibraryD.CityInfo(Reader.ReadString(), "", 
                                    Reader.ReadUInt64(), Reader.ReadString(), Reader.ReadUInt64(), Reader.ReadString(),
                                    Reader.ReadInt32());
                                UnchangedSims.Add(S);
                            }

                            Reader.Close();

                            foreach (Sim S in UnchangedSims)
                            {
                                //Length of the current entry, so its skippable...
                                Writer.Write((int)4 + S.GUID.ToString().Length + 4 + S.Timestamp.Length + S.Name.Length + S.Sex.Length +
                                    S.Description.Length + 17 + S.ResidingCity.Name.Length + 8 + S.ResidingCity.UUID.ToString().Length + 8 +
                                    S.ResidingCity.IP.Length + 4);
                                Writer.Write(S.GUID.ToString());
                                Writer.Write(S.CharacterID);
                                Writer.Write(S.Timestamp);
                                Writer.Write(S.Name);
                                Writer.Write(S.Sex);
                                Writer.Write(S.Description);
                                Writer.Write(S.HeadOutfitID);
                                Writer.Write(S.BodyOutfitID);
                                Writer.Write((byte)S.Appearance);
                                Writer.Write(S.ResidingCity.Name);
                                Writer.Write(S.ResidingCity.Thumbnail);
                                Writer.Write(S.ResidingCity.UUID);
                                Writer.Write(S.ResidingCity.Map);
                                Writer.Write(S.ResidingCity.IP);
                                Writer.Write(S.ResidingCity.Port);
                            }
                        }
                    }
                }

                Writer.Flush();
                Writer.Close();
            }

            if (File.Exists(CacheDir + "\\Sims.cache"))
            {
                File.Delete(CacheDir + "\\Sims.cache");
                File.Move(CacheDir + "\\Sims.tempcache", CacheDir + "\\Sims.cache");
            }
            else
                File.Move(CacheDir + "\\Sims.tempcache", CacheDir + "\\Sims.cache");
        }

        /// <summary>
        /// Loads all sims from the player's cache.
        /// </summary>
        /// <returns>List of cached sims. Will be empty if no cache existed.</returns>
        public static List<Sim> LoadAllSims()
        {
            List<Sim> CachedSims = new List<Sim>();

            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);
                return CachedSims;
            }

            if (!File.Exists(CacheDir + "\\Sims.cache"))
                return CachedSims;

            using (BinaryReader Reader = new BinaryReader(File.Open(CacheDir + "\\Sims.cache", FileMode.Open)))
            {
                //Last time these sims were cached.
                Reader.ReadString();
                int NumSims = Reader.ReadInt32();

                for (int i = 0; i < NumSims; i++)
                {
                    Reader.ReadInt32(); //Length of entry.
                    string GUID = Reader.ReadString();

                    Sim S = new Sim(GUID);

                    S.CharacterID = Reader.ReadInt32();
                    S.Timestamp = Reader.ReadString();
                    S.Name = Reader.ReadString();
                    S.Sex = Reader.ReadString();
                    S.Description = Reader.ReadString();
                    S.HeadOutfitID = Reader.ReadUInt64();
                    S.BodyOutfitID = Reader.ReadUInt64();
                    S.Appearance = (AppearanceType)Reader.ReadByte();
                    S.ResidingCity = new ProtocolAbstractionLibraryD.CityInfo(Reader.ReadString(), "",
                        Reader.ReadUInt64(), Reader.ReadString(), Reader.ReadUInt64(), Reader.ReadString(),
                        Reader.ReadInt32());
                    CachedSims.Add(S);
                }

                Reader.Close();
            }

            return CachedSims;
        }

        /// <summary>
        /// Loads sims that weren't received by the login server.
        /// </summary>
        /// <param name="ReceivedSims">Sims that were received by the login server.</param>
        /// <returns>A list of all sims, the ones received from the login server and loaded from cache.</returns>
        public static List<Sim> LoadCachedSims(List<Sim> ReceivedSims)
        {
            List<Sim> CachedSims = LoadAllSims();

            for(int i = 0; i < CachedSims.Count; i++)
            {
                for (int j = 0; j < ReceivedSims.Count; j++)
                {
                    if (CachedSims[i].GUID != ReceivedSims[j].GUID)
                        ReceivedSims.Add(CachedSims[i]);
                }
            }

            //Once in a blue moon, LINQ syntax can be pretty (or at least not downright ugly).
            //This makes sure that no duplicates exist in the returned list.
            return ReceivedSims.Distinct().ToList();
        }
    }
}
