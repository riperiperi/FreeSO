using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Xml;

namespace FSO.Server.Api.Utils
{
    public class ApiResponse
    {
        public static HttpResponseMessage Plain(HttpStatusCode code, string text)
        {
            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(text, Encoding.UTF8, "text/plain");
            return response;
        }

        public static HttpResponseMessage Xml(HttpStatusCode code, IXMLEntity xml)
        {
            var doc = new XmlDocument();
            var firstChild = xml.Serialize(doc);
            doc.AppendChild(firstChild);

            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(doc.OuterXml, Encoding.UTF8, "text/xml");
            return response;
        }

        public static Func<HttpResponseMessage> XmlFuture(HttpStatusCode code, IXMLEntity xml)
        {
            var doc = new XmlDocument();
            var firstChild = xml.Serialize(doc);
            doc.AppendChild(firstChild);

            return () =>
            {
                var response = new HttpResponseMessage(code);
                response.Content = new StringContent(doc.OuterXml, Encoding.UTF8, "text/xml");
                return response;
            };
        }
    }
}