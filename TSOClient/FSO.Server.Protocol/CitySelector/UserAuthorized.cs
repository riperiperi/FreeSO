using FSO.Common.Utils;

namespace FSO.Server.Protocol.CitySelector
{
    public class UserAuthorized : IXMLEntity
    {
        public string FSOVersion;
        public string FSOBranch;
        public string FSOUpdateUrl;
        public string FSOCDNUrl;
        // JWT minted by InitialConnectServlet; carried back over the XML
        // response so the TSO client (which doesn't share a CookieContainer
        // across screens) can stash it and present it as
        // Authorization: Bearer <token> on subsequent /userapi/* calls.
        // Existing endpoints continue to honour the JWT cookie; the new
        // daily-quest endpoints honour the header.
        public string FSOApiAuthToken;

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var element = doc.CreateElement("User-Authorized");
            element.AppendTextNode("FSO-Version", FSOVersion);
            element.AppendTextNode("FSO-Branch", FSOBranch);
            element.AppendTextNode("FSO-UpdateUrl", FSOUpdateUrl);
            element.AppendTextNode("FSO-CDNUrl", FSOCDNUrl);
            if (!string.IsNullOrEmpty(FSOApiAuthToken))
                element.AppendTextNode("FSO-ApiAuthToken", FSOApiAuthToken);
            return element;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.FSOVersion = element.ReadTextNode("FSO-Version");
            this.FSOBranch = element.ReadTextNode("FSO-Branch");
            this.FSOUpdateUrl = element.ReadTextNode("FSO-UpdateUrl");
            this.FSOCDNUrl = element.ReadTextNode("FSO-CDNUrl");
            this.FSOApiAuthToken = element.ReadTextNode("FSO-ApiAuthToken");
        }
    }
}
