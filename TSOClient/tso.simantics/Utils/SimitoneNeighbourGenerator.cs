using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Utils
{
    public static class SimitoneNeighbourGenerator
    {
        //Template Person: 0x7FD96B54
        public static uint TEMPLATE_GUID = 0x7FD96B54;

        //process of creating a family
        // - other function provides persondata and bodystrings
        // - create FAMI with a new ID
        //   - generate GUID for each sim, and populate FamilyGUIDs
        // - set family in generated persondatas
        // - make the sims
        //   - AddNeighbour to NBRS as below (get back ID. do not set neighborID in personData, as the original doesn't)
        //   - gets a free user_#### id and makes that the name
        //   - replace relevant body strings in TemplatePerson
        //   - add/replace CTSS in TemplatePerson
        //   - change OBJD name to "user_#### - name". maybe change ctss id?
        //   - save sim to userdata

        public static void PrepareTemplatePerson(uint guid, SimTemplateCreateInfo info)
        {
            var neigh = Content.Content.Get().Neighborhood;
            //userid
            var userid = neigh.NextSim;

            var tempObj = Content.Content.Get().WorldObjects.Get(TEMPLATE_GUID);
            tempObj.OBJ.ChunkParent.RetainChunkData = true;
            tempObj.OBJ.GUID = guid;
            tempObj.OBJ.ChunkLabel = "user" + userid.ToString().PadLeft(5, '0') + " - " + info.Name;

            var ctss = tempObj.Resource.Get<CTSS>(2000);
            if (ctss == null)
            {
                ctss = new CTSS()
                {
                    ChunkLabel = "",
                    ChunkID = 2000,
                    ChunkProcessed = true,
                    ChunkType = "CTSS",
                    ChunkParent = tempObj.Resource.MainIff,
                    AddedByPatch = true,
                };
                tempObj.Resource.MainIff.AddChunk(ctss);
                ctss.InsertString(0, new STRItem() { Value = "", Comment = "" });
                ctss.InsertString(0, new STRItem() { Value = "", Comment = "" });
            }
            ctss.SetString(0, info.Name);
            ctss.SetString(1, info.Bio);
            tempObj.OBJ.CatalogStringsID = 2000;

            var bodyStrings = tempObj.Resource.Get<STR>(200);
            foreach (var item in info.BodyStringReplace)
            {
                bodyStrings.SetString(item.Key, item.Value);
            }

            neigh.SaveNewNeighbour(tempObj);
            bodyStrings.SetString(1, "");
            bodyStrings.SetString(2, "");
        }

        public static FAMI CreateFamily(string name, int count, SimTemplateCreateInfo[] infos)
        {
            var fami = CreateFamily(name, count);
            for (int i=0; i<count; i++)
            {
                var guid = fami.FamilyGUIDs[i];
                var info = infos[i];
                info.FamilyID = (short)fami.ChunkID;
                PrepareTemplatePerson(guid, info);
                AddNeighbor(guid, 9, info.MakePersonData());
            }
            Content.Content.Get().Neighborhood.SaveNeighbourhood(true);
            return fami;
        }

        public static Neighbour CreateNeighbor(uint guid, SimTemplateCreateInfo info)
        {
            PrepareTemplatePerson(guid, info);
            return AddNeighbor(guid, 9, info.MakePersonData());
        }

        public static FAMI CreateFamily(string name, int count)
        {
            var neigh = Content.Content.Get().Neighborhood;
            var families = neigh.MainResource.List<FAMI>() ?? new List<FAMI>();
            families = families.OrderBy(x => x.ChunkID).ToList();
            ushort newID = 0;
            for (int i = 0; i < families.Count; i++)
            {
                if (families[i].ChunkID == newID) newID++;
                else break;
            }

            var guids = new uint[count];
            for (int i=0; i<count; i++)
            {
                guids[i] = GenerateGUID(guids);
            }

            var newFam = new FAMI()
            {
                ChunkLabel = "",
                ChunkID = newID,
                ChunkProcessed = true,
                ChunkType = "FAMI",
                ChunkParent = neigh.MainResource,
                AddedByPatch = true,

                FamilyGUIDs = guids,
                FamilyNumber = families.Max(x => x.FamilyNumber) + 1,
                Unknown = 24,
                Budget = 20000,
            };
            neigh.MainResource.AddChunk(newFam);

            var newFams = new FAMs()
            {
                ChunkLabel = "",
                ChunkID = newID,
                ChunkProcessed = true,
                ChunkType = "FAMs",
                ChunkParent = neigh.MainResource,
                AddedByPatch = true,
            };
            newFams.InsertString(0, new STRItem() { Comment = "", Value = name });
            neigh.MainResource.AddChunk(newFams);

            return newFam;
        }

        public static uint GenerateGUID(uint[] avoid)
        {
            var objProvider = Content.Content.Get().WorldObjects;
            lock (objProvider.Entries)
            {
                var rand = new Random();
                var guid = (uint)rand.Next();
                //doesnt cover entire uint space, but not really a problem right now.
                while (objProvider.Entries.ContainsKey(guid) || avoid.Contains(guid))
                {
                    guid = (uint)rand.Next();
                    //todo: if you get really unlucky, you can get stuck here forever. I mean really unlucky...
                }
                return guid;
            }
        }

        public static Neighbour AddNeighbor(uint guid, int personMode, short[] personData)
        {
            var neigh = Content.Content.Get().Neighborhood;
            var ns = neigh.Neighbors.Entries;
            //find the lowest id that is free
            short newID = 1;
            for (int i=0; i<ns.Count; i++)
            {
                if (ns[i].NeighbourID == newID) newID++;
                else if (ns[i].NeighbourID < newID) continue;
                else break;
            }

            var newN = new Neighbour()
            {
                Name = "iffname",
                NeighbourID = newID,
                GUID = guid,
                Relationships = new Dictionary<int, List<short>>(),
                PersonMode = personMode,
                PersonData = personData
            };
            neigh.Neighbors.AddNeighbor(newN);
            
            return newN;
        }
    }

    public class SimTemplateCreateInfo {
        public string Name;
        public string Bio;
        public short Gender;
        public short SkinTone;
        public bool Child;
        public short FamilyID;
        public short[] PersonalityPoints = new short[5];

        public Dictionary<int, string> BodyStringReplace;

        //code is mafat, etc
        public SimTemplateCreateInfo(string code, string skin)
        {
            //mcchd, mafat, etc
            Child = code[1] == 'c';
            var gender = code[0];
            var bodytype = code.Substring(2, 3);
            var codeunisex = (Child) ? "uchd" : (gender+bodytype);
            BodyStringReplace = new Dictionary<int, string>()
            {
                {0, Child?"child":"adult" },
                {12, (gender == 'm')?"male":"female" },
                {13, Child?"9":"27" },
                {14, skin },
                {15, "n"+codeunisex+"_01,BODY=n"+codeunisex+skin+"_01" },
                {16, "n"+codeunisex+"_01,BODY=u"+gender+bodytype+skin+(Child?"undies":"lguard")+"_01" },

                {27, (gender == 'm')?",BODY=f"+gender+bodytype+skin+"_01":"ff"+bodytype+"_01,BODY=ff"+bodytype+skin+"_01" },
                {28, (gender == 'm')?"HmLO,HAND=gmao_yeti":"HfLO,HAND=gfao_bear1" },
                {29, (gender == 'm')?"HmRO,HAND=gmao_yeti":"HfRO,HAND=gfao_bear1" },
                {30, (Child)?"ADDED":("f100"+code+"_original,BODY=f100"+code+skin+"_original") },
                {31, "s100"+code+"_original,BODY=s100"+code+skin+"_original" },
                {32, "l100"+code+"_original,BODY=l100"+code+skin+"_original" },
                {33, "w100"+code+"_original,BODY=w100"+code+skin+"_original" },
                {34, (Child)?"ADDED":("h533"+code+"_zoot,BODY=h533"+code+skin+"_zoot") },
            };

            Gender = (short)((gender == 'f') ? 1 : 0);
            switch (skin)
            {
                case "lgt":
                    SkinTone = 0; break;
                case "med":
                    SkinTone = 1; break;
                case "drk":
                    SkinTone = 2; break;
            }
        }

        public short[] MakePersonData()
        {
            var pd = new short[88];
            var rand = new Random();

            pd[2] = PersonalityPoints[0];
            pd[3] = PersonalityPoints[1];
            pd[5] = PersonalityPoints[2];
            pd[6] = PersonalityPoints[3];
            pd[7] = PersonalityPoints[4];

            pd[13] = (short)(500+rand.Next(5)*100);
            pd[14] = (short)(500 + rand.Next(5) * 100);

            pd[16] = 600;

            pd[26] = 600;

            //personality init
            //same as init traits on avatar
            var need = new int[] { 1, 3, 3, 3 };
            //none 0
            //low <4
            //med <7
            //hi otherwise
            for (int i=46; i<56; i++)
            {
                var pers = rand.Next(10);
                bool good = false;
                int nindex = 0;
                if (pers == 0)
                    nindex = 0;
                else if (pers < 4)
                    nindex = 1;
                else if (pers < 7)
                    nindex = 2;
                else
                    nindex = 3;

                good = need[nindex] > 0;
                if (!good) i--; //try again
                else
                {
                    need[nindex]--;
                    pd[i] = (short)(pers * 100);
                }
            }

            pd[58] = (short)(Child ? 9 : 27);
            pd[60] = SkinTone;
            pd[61] = FamilyID;
            pd[65] = Gender;

            return pd;
        }
    }
}
