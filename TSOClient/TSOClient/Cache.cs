using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TSOClient.VM;

namespace TSOClient
{
    public class Cache
    {
        /// <summary>
        /// Gets the last time sims were cached from the cache.
        /// </summary>
        /// <returns>A string representing the last time the sims were cached.</returns>
        public static string GetDateCached()
        {
            BinaryReader Reader = new BinaryReader(File.Open("CharacterCache\\Sims.cache", FileMode.Open));
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
            if (!Directory.Exists("CharacterCache"))
                Directory.CreateDirectory("CharacterCache");

            using(BinaryWriter Writer = new BinaryWriter(File.Create("CharacterCache\\Sims.tempcache")))
            {
                //Last time these sims were cached.
                Writer.Write(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"));

                Writer.Write(FreshSims.Count);

                foreach (Sim S in FreshSims)
                {
                    //Length of the current entry, so its skippable...
                    Writer.Write((int)4 + S.GUID.ToString().Length + S.Timestamp.Length + S.Name.Length + S.Sex.Length +
                        S.Description.Length + 16 + S.CityID.ToString().Length);
                    Writer.Write(S.CharacterID);
                    Writer.Write(S.GUID.ToString());
                    Writer.Write(S.Timestamp);
                    Writer.Write(S.Name);
                    Writer.Write(S.Sex);
                    Writer.Write(S.Description);
                    Writer.Write(S.HeadOutfitID);
                    Writer.Write(S.BodyOutfitID);
                    Writer.Write(S.CityID.ToString());
                }

                if (File.Exists("CharacterCache\\Sims.cache"))
                {
                    using (BinaryReader Reader = new BinaryReader(File.Open("CharacterCache\\Sims.cache", FileMode.Open)))
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
                                S.CityID = new Guid(Reader.ReadString());
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
                                S.CityID = new Guid(Reader.ReadString());
                                UnchangedSims.Add(S);

                                Reader.ReadInt32(); //Length of third entry.
                                S.CharacterID = Reader.ReadInt32();
                                S.Timestamp = Reader.ReadString();
                                S.Name = Reader.ReadString();
                                S.Sex = Reader.ReadString();
                                S.Description = Reader.ReadString();
                                S.HeadOutfitID = Reader.ReadUInt64();
                                S.BodyOutfitID = Reader.ReadUInt64();
                                S.CityID = new Guid(Reader.ReadString());
                                UnchangedSims.Add(S);
                            }

                            Reader.Close();

                            foreach (Sim S in UnchangedSims)
                            {
                                //Length of the current entry, so its skippable...
                                Writer.Write((int)4 + S.GUID.ToString().Length + S.Timestamp.Length + S.Name.Length + S.Sex.Length +
                                    S.Description.Length + 16 + S.CityID.ToString().Length);
                                Writer.Write(S.CharacterID);
                                Writer.Write(S.Timestamp);
                                Writer.Write(S.Name);
                                Writer.Write(S.Sex);
                                Writer.Write(S.Description);
                                Writer.Write(S.HeadOutfitID);
                                Writer.Write(S.BodyOutfitID);
                                Writer.Write(S.CityID.ToString());
                            }
                        }
                    }
                }

                Writer.Flush();
                Writer.Close();
            }

            if (File.Exists("CharacterCache\\Sims.cache"))
                File.Replace("CharacterCache\\Sims.tempcache", "CharacterCache\\Sims.cache", "CaracterCache\\cache.backup");
            else
                File.Move("CharacterCache\\Sims.tempcache", "CharacterCache\\Sims.cache");
        }
    }
}
