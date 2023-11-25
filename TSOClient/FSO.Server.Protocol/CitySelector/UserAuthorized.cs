using FSO.Common.Utils;

namespace FSO.Server.Protocol.CitySelector
{
    public class UserAuthorized : IXMLEntity
    {
        public string FSOVersion;
        public string FSOBranch;
        public string FSOUpdateUrl;
        public string FSOCDNUrl;

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var element = doc.CreateElement("User-Authorized");
            element.AppendTextNode("FSO-Version", FSOVersion);
            element.AppendTextNode("FSO-Branch", FSOBranch);
            element.AppendTextNode("FSO-UpdateUrl", FSOUpdateUrl);
            element.AppendTextNode("FSO-CDNUrl", FSOCDNUrl);
            return element;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.FSOVersion = element.ReadTextNode("FSO-Version");
            this.FSOBranch = element.ReadTextNode("FSO-Branch");
            this.FSOUpdateUrl = element.ReadTextNode("FSO-UpdateUrl");
            this.FSOCDNUrl = element.ReadTextNode("FSO-CDNUrl");
        }
    }
}
