using FSO.Common.Utils;
using System;

namespace FSO.Server.Protocol.CitySelector
{
    public class AvatarData : IXMLEntity
    {
        public uint ID;
        public string Name;
        public string ShardName;



        /** Non standard **/

        /** Appearance **/
        public AvatarAppearanceType AppearanceType { get; set; }
        public ulong HeadOutfitID { get; set; }
        public ulong BodyOutfitID { get; set; }
        public string Description { get; set; }

        /** Lot **/
        public uint? LotId { get; set; }
        public uint? LotLocation { get; set; }
        public string LotName { get; set; }

        #region IXMLPrinter Members

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var result = doc.CreateElement("Avatar-Data");
            result.AppendTextNode("AvatarID", ID.ToString());
            result.AppendTextNode("Name", Name);
            result.AppendTextNode("Shard-Name", ShardName);

            //NEW: Appearance info
            result.AppendTextNode("Head", HeadOutfitID.ToString());
            result.AppendTextNode("Body", BodyOutfitID.ToString());
            result.AppendTextNode("Appearance", AppearanceType.ToString());

            if (LotId.HasValue && LotLocation.HasValue && LotName != null){
                result.AppendTextNode("LotId", LotId.Value.ToString());
                result.AppendTextNode("LotName", LotName);
                result.AppendTextNode("LotLocation", LotLocation.Value.ToString());
            }

            result.AppendTextNode("Description", Description);

            return result;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.ID = uint.Parse(element.ReadTextNode("AvatarID"));
            this.Name = element.ReadTextNode("Name");
            this.ShardName = element.ReadTextNode("Shard-Name");

            var headString = element.ReadTextNode("Head");
            if (headString != null)
            {
                this.HeadOutfitID = ulong.Parse(headString);
            }

            var bodyString = element.ReadTextNode("Body");
            if (bodyString != null)
            {
                this.BodyOutfitID = ulong.Parse(bodyString);
            }

            var apprString = element.ReadTextNode("Appearance");
            if (apprString != null)
            {
                this.AppearanceType = (AvatarAppearanceType)Enum.Parse(typeof(AvatarAppearanceType), apprString);
            }

            var lotId = element.ReadTextNode("LotId");
            if(lotId != null)
            {
                this.LotId = uint.Parse(lotId);
            }

            var lotLocation = element.ReadTextNode("LotLocation");
            if (lotLocation != null)
            {
                this.LotLocation = uint.Parse(lotLocation);
            }

            LotName = element.ReadTextNode("LotName");

            var descString = element.ReadTextNode("Description");
            this.Description = descString ?? "";
        }

        #endregion
    }
}
