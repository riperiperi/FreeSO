using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Protocol.CitySelector
{
    public class UserAuthorized : IXMLEntity
    {
        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            return doc.CreateElement("User-Authorized");
        }

        public void Parse(System.Xml.XmlElement element)
        {
        }
    }
}
