using FSO.Common.Utils;
using System;

namespace FSO.Server.Protocol.CitySelector
{
    public class InitialConnectServletResult : IXMLEntity
    {
        public InitialConnectServletResultType Status;
        public XMLErrorMessage Error;
        public UserAuthorized UserAuthorized;



        #region IXMLEntity Members

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            throw new NotImplementedException();
        }

        public void Parse(System.Xml.XmlElement element)
        {
            switch (element.Name)
            {
                case "Error-Message":
                    Status = InitialConnectServletResultType.Error;
                    Error = new XMLErrorMessage();
                    Error.Parse(element);
                    break;
                case "User-Authorized":
                    Status = InitialConnectServletResultType.Authorized;
                    UserAuthorized = new UserAuthorized();
                    UserAuthorized.Parse(element);
                    break;
                case "Patch-Result":
                    Status = InitialConnectServletResultType.Patch;
                    break;
            }
        }

        #endregion
    }

    public enum InitialConnectServletResultType
    {
        Authorized,
        Patch,
        Error
    }
}
