using FSO.Common.Utils;
using FSO.Server.Database.DA.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
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

        public static HttpResponseMessage Json(HttpStatusCode code, object obj)
        {
            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            return response;
        }

        public static HttpResponseMessage PagedList<T>(HttpStatusCode code, PagedList<T> list)
        {
            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(list), Encoding.UTF8, "application/json");
            response.Headers.Add("X-Total-Count", list.Total.ToString());
            response.Headers.Add("X-Offset", list.Offset.ToString());
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
            var serialized = doc.OuterXml;

            return () =>
            {
                var response = new HttpResponseMessage(code);
                response.Content = new StringContent(serialized, Encoding.UTF8, "text/xml");
                return response;
            };
        }
    }
}