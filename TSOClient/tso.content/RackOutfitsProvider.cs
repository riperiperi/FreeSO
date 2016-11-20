using FSO.Common.Content;
using FSO.Content.Model;
using FSO.Files;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FSO.Content
{
    public class RackOutfitsProvider
    {
        private Dictionary<RackType, RackOutfits> Racks = new Dictionary<RackType, RackOutfits>();
        private Content Content;

        public RackOutfitsProvider(Content contentManager)
        {
            this.Content = contentManager;
        }

        public void Init()
        {
            var purchasable = Content.GetPath("packingslips\\purchasable.xml");
            if (!File.Exists(purchasable)){
                return;
            }

            var xml = new XmlDocument();
            xml.Load(purchasable);

            foreach (XmlNode child in xml.FirstChild.ChildNodes)
            {
                if (!(child is XmlElement)) { continue; }

                var name = child.Name;
                var assetId = child.Attributes["assetID"];
                var price = child.Attributes["price"];

                if (assetId == null || price == null)
                {
                    continue;
                }

                var outfit = new RackOutfit
                {
                    AssetID = (uint)ulong.Parse(assetId.InnerText.Substring(2), NumberStyles.HexNumber),
                    Price = int.Parse(price.InnerText)
                };

                if (name.EndsWith("Male"))
                {
                    outfit.Gender = RackOutfitGender.Male;
                    outfit.RackType = GetRackType(name.Substring(0, name.Length - 4));
                }
                else if (name.EndsWith("Female"))
                {
                    outfit.Gender = RackOutfitGender.Female;
                    outfit.RackType = GetRackType(name.Substring(0, name.Length - 6));
                }
                else if (name.StartsWith("Decor"))
                {
                    outfit.Gender = RackOutfitGender.Neutral;
                    var type = name.Split('_');
                    outfit.RackType = GetRackType(type[0] + "_" + type[1]);
                }else{
                    continue;
                }

                if (!Racks.ContainsKey(outfit.RackType))
                {
                    Racks.Add(outfit.RackType, new RackOutfits()
                    {
                        Outfits = new List<RackOutfit>(),
                        RackType = outfit.RackType
                    });
                }

                Racks[outfit.RackType].Outfits.Add(outfit);
            }
        }

        public RackOutfits GetByRackType(RackType rackType){
            return Racks[rackType];
        }

        public List<RackOutfits> GetAll(){
            return Racks.Values.ToList();
        }

        private RackType GetRackType(string name)
        {
            switch (name)
            {
                case "Daywear":
                    return RackType.Daywear;
                case "Formalwear":
                    return RackType.Formalwear;
                case "Swimwear":
                    return RackType.Swimwear;
                case "Sleepwear":
                    return RackType.Sleepwear;
                case "Decor_Head":
                    return RackType.Decor_Head;
                case "Decor_Back":
                    return RackType.Decor_Back;
                case "Decor_Tail":
                    return RackType.Decor_Tail;
                case "Decor_Shoe":
                    return RackType.Decor_Shoe;
            }

            throw new Exception("Unknown rackType " + name);
        }
    }
}
