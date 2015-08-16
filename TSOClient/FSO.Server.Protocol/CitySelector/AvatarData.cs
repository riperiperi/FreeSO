using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Protocol.CitySelector
{
    public class AvatarData : IXMLEntity
    {
        public uint ID;
        public string Name;
        public string ShardName;

        /** Appearance **/
        public AvatarAppearanceType AppearanceType { get; set; }
        public ulong HeadOutfitID { get; set; }
        public ulong BodyOutfitID { get; set; }

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
        }

        #endregion
    }
}
