using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Content;
using FSO.Content.TS1;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Utils
{
    public static class VMTS1PurchasableOutfitHelper
    {
        public static string[] OutfitTypes = new string[] { "b", "f", "s", "l", "w", "h" };
        public static short[] OutfitTypeToInd = new short[] { 1, 30, 31, 32, 33, 34 };

        public static Tuple<string, string>[] GetValidOutfits(VMAvatar avatar, short outfitType)
        {
            string skin = "";
            string collectionType, simtype;
            if (avatar == null || outfitType < 0)
            {
                collectionType = "b";
                simtype = (outfitType == -1) ? "dog" : "cat";
            }
            else {
                if (avatar.IsCat)
                {
                    collectionType = "b";
                    simtype = "cat";
                }
                else if (avatar.IsDog)
                {
                    collectionType = "b";
                    simtype = "dog";
                }
                else
                {
                    collectionType = OutfitTypes[outfitType];

                    var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);
                    simtype = bodyStrings.GetString(1).Substring(4);
                    simtype = simtype.Substring(0, simtype.IndexOf('_'));
                    skin = bodyStrings.GetString(14);
                }
            }
            
            var col = Content.Content.Get().BCFGlobal.CollectionsByName[collectionType];
            var bodies = col.ClothesByAvatarType[simtype];

            var tex = (TS1AvatarTextureProvider)Content.Content.Get().AvatarTextures;
            var texnames = tex.GetAllNames();

            var bodyTex = bodies.Select(x => RemoveExt(texnames.FirstOrDefault(y => y.StartsWith(ExtractID(x, skin))))).ToList();
            var handgroupTex = bodies.Select(x => (RemoveExt(texnames.FirstOrDefault(y => y == "huao" + FindHG(x))) ?? "huao" + skin).Substring(4)).ToList();

            var result = new List<Tuple<string, string>>();
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodyTex[i] == null)
                {
                    bodyTex.RemoveAt(i);
                    handgroupTex.RemoveAt(i);
                    bodies.RemoveAt(i--);
                } else
                {
                    result.Add(new Tuple<string, string>(
                        bodies[i] + ",BODY=" + bodyTex[i],
                        handgroupTex[i]
                        ));
                }
            }

            return result.ToArray();
        }

        public static short GetSuitIndex(VMAvatar avatar, short outfitType)
        {
            var validSuits = GetValidOutfits(avatar, outfitType);
            var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);

            var bsInd = OutfitTypeToInd[outfitType];
            var prevInd = Array.IndexOf(validSuits, bodyStrings.GetString(bsInd).Split(';')[0].ToLowerInvariant());
            return (short)prevInd;
        }

        public static short SetSuit(VMAvatar avatar, short outfitType, short newIndex)
        {
            var validSuits = GetValidOutfits(avatar, outfitType);
            var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);

            var bsInd = OutfitTypeToInd[outfitType];
            var oldSuit = bodyStrings.GetString(bsInd).Split(';')[0];
            var prevInd = Array.FindIndex(validSuits, x => x.Item1.ToLowerInvariant() == bodyStrings.GetString(bsInd).Split(';')[0].ToLowerInvariant());

            var newSuit = validSuits[newIndex];
            bodyStrings.SetString(bsInd, newSuit.Item1, STRLangCode.EnglishUS);

            if (outfitType == 0 && newSuit.Item2 != "")
            {
                //right now only replace the handgroup when changing the default outfit.
                var hg = newSuit.Item2;
                bodyStrings.SetString(17, TilEQ(bodyStrings.GetString(17, STRLangCode.EnglishUS)) + "huao" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(18, TilEQ(bodyStrings.GetString(18, STRLangCode.EnglishUS)) + "huao" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(19, TilEQ(bodyStrings.GetString(19, STRLangCode.EnglishUS)) + "huap" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(20, TilEQ(bodyStrings.GetString(20, STRLangCode.EnglishUS)) + "huap" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(21, TilEQ(bodyStrings.GetString(21, STRLangCode.EnglishUS)) + "huac" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(22, TilEQ(bodyStrings.GetString(22, STRLangCode.EnglishUS)) + "huac" + hg, STRLangCode.EnglishUS);
            }

            Content.Content.Get().Neighborhood.AvatarChanged(avatar.Object.OBJ.GUID);

            return (short)prevInd;
        }

        public static string TilEQ(string item)
        {
            var ind = item.IndexOf('=');
            if (ind != -1) item = item.Substring(ind);
            return item;
        }

        private static string FindHG(string item)
        {
            var ind = item.IndexOf('_');
            if (ind != -1) item = item.Substring(ind);
            return item;
        }

        private static string RemoveExt(string item)
        {
            if (item == null) return null;
            var ind = item.LastIndexOf('.');
            if (ind != -1) return item.Substring(0, ind);
            return item;
        }

        private static string ExtractID(string item, string skncol)
        {
            var ind = item.IndexOf('_');
            if (ind != -1) item = item.Substring(0, ind);
            return item + skncol;
        }

        private static string InsertSkinColor(string name, string skncol)
        {
            var ind = name.IndexOf('_');
            if (ind != -1) name = name.Insert(ind, skncol);
            return name;
        }
    }
}
