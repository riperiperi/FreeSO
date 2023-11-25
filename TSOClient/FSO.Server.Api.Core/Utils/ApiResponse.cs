using FSO.Common.Utils;
using FSO.Server.Database.DA.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Xml;

namespace FSO.Server.Api.Core.Utils
{
    public class ApiResponse
    {
        public static IActionResult Plain(HttpStatusCode code, string text)
        {
            return new ContentResult
            {
                StatusCode = (int)code,
                Content = text,
                ContentType = "text/plain"
            };
        }

        public static IActionResult Json(HttpStatusCode code, object obj)
        {
            return new ContentResult
            {
                StatusCode = (int)code,
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(obj),
                ContentType = "application/json"
            };
        }

        public static IActionResult PagedList<T>(HttpRequest request, HttpStatusCode code, PagedList<T> list)
        {
            request.HttpContext.Response.Headers.Add("X-Total-Count", list.Total.ToString());
            request.HttpContext.Response.Headers.Add("X-Offset", list.Offset.ToString());

            return new ContentResult
            {
                StatusCode = (int)code,
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(list),
                ContentType = "application/json"
            };
        }

        public static IActionResult Xml(HttpStatusCode code, IXMLEntity xml)
        {
            var doc = new XmlDocument();
            var firstChild = xml.Serialize(doc);
            doc.AppendChild(firstChild);

            return new ContentResult
            {
                StatusCode = (int)code,
                Content = doc.OuterXml,
                ContentType = "text/xml"
            };
        }

        public static Func<IActionResult> XmlFuture(HttpStatusCode code, IXMLEntity xml)
        {
            var doc = new XmlDocument();
            var firstChild = xml.Serialize(doc);
            doc.AppendChild(firstChild);
            var serialized = doc.OuterXml;

            return () =>
            {
                return new ContentResult
                {
                    StatusCode = (int)code,
                    Content = serialized,
                    ContentType = "text/xml"
                };
            };
        }
    }
}