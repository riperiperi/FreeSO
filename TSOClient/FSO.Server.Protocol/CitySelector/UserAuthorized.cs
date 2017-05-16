using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Protocol.CitySelector
{
    public class UserAuthorized : IXMLEntity
    {
        public string FSOVersion;
        public string FSOBranch;
        public string FSOUpdateUrl;

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var element = doc.CreateElement("User-Authorized");
            element.AppendTextNode("FSO-Version", FSOVersion);
            element.AppendTextNode("FSO-Branch", FSOBranch);
            element.AppendTextNode("FSO-UpdateUrl", FSOUpdateUrl);
            return element;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.FSOVersion = element.ReadTextNode("FSO-Version");
            this.FSOBranch = element.ReadTextNode("FSO-Branch");
            this.FSOUpdateUrl = element.ReadTextNode("FSO-UpdateUrl");
        }
    }
}
