using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Utils;

namespace FSO.Server.Protocol.CitySelector
{
    public class ShardStatusItem : IXMLEntity
    {
        public string Name;
        public int Rank;
        public string Map;
        public ShardStatus Status;

        public ShardStatusItem()
        {
        }

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var result = doc.CreateElement("Shard-Status");
            result.AppendTextNode("Location", "public");
            result.AppendTextNode("Name", Name);
            result.AppendTextNode("Rank", Rank.ToString());
            result.AppendTextNode("Map", Map);
            result.AppendTextNode("Status", Status.ToString());
            return result;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.Name = element.ReadTextNode("Name");
            this.Rank = int.Parse(element.ReadTextNode("Rank"));
            this.Map = element.ReadTextNode("Map");
            this.Status = (ShardStatus)Enum.Parse(typeof(ShardStatus), element.ReadTextNode("Status"));
        }
    }
}
