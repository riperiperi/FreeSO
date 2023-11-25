using FSO.Common.Utils;

namespace FSO.Server.Protocol.CitySelector
{
    public class ShardSelectorServletResponse : IXMLEntity
    {
        public string Address;
        public string Ticket;
        public string ConnectionID;
        public uint PlayerID;
        public string AvatarID;

        public bool PreAlpha = false;

        #region IXMLPrinter Members

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var result = doc.CreateElement("Shard-Selection");
            result.AppendTextNode("Connection-Address", Address);
            result.AppendTextNode("Authorization-Ticket", Ticket);
            result.AppendTextNode("PlayerID", PlayerID.ToString());

            if (PreAlpha == false)
            {
                result.AppendTextNode("ConnectionID", ConnectionID);
                result.AppendTextNode("EntitlementLevel", "");
            }
            result.AppendTextNode("AvatarID", AvatarID); //freeso now uses this

            return result;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.Address = element.ReadTextNode("Connection-Address");
            this.Ticket = element.ReadTextNode("Authorization-Ticket");
            this.PlayerID = uint.Parse(element.ReadTextNode("PlayerID"));

            this.AvatarID = element.ReadTextNode("AvatarID");
        }

        #endregion
    }
}
