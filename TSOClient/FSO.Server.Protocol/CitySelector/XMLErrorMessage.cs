using FSO.Common.Utils;
using System;

namespace FSO.Server.Protocol.CitySelector
{
    public class XMLErrorMessage : IXMLEntity
    {
        public String Code;
	    public String Message;
    	
	    public XMLErrorMessage(){
	    }

        public XMLErrorMessage(String code, String message)
        {
            this.Code = code;
            this.Message = message;
	    }

        #region IXMLPrinter Members

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var element = doc.CreateElement("Error-Message");
            element.AppendTextNode("Error-Number", Code);
            element.AppendTextNode("Error", Message);
            return element;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            this.Code = element.ReadTextNode("Error-Number");
            this.Message = element.ReadTextNode("Error");
        }

        #endregion
    }
}
