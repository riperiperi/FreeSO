using FSO.Common.Utils;
using FSO.Server.Database.DA.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
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

        /// <summary>
        /// Serves a file as a PNG with proper HTTP cache validation. Sends Last-Modified
        /// and ETag headers so the browser/CDN can issue conditional requests; returns
        /// 304 Not Modified when the client's cached copy is still current. Cache-Control
        /// is "public, max-age=300, must-revalidate" — short freshness window, then
        /// always revalidate.
        ///
        /// Returns null if the file doesn't exist (caller should fall back / 404).
        /// </summary>
        public static IActionResult FileWithCache(HttpRequest request, string path, string contentType = "image/png")
        {
            FileInfo info;
            try
            {
                info = new FileInfo(path);
                if (!info.Exists) return null;
            }
            catch
            {
                return null;
            }

            // Truncate to seconds — HTTP date headers don't carry sub-second precision
            // and a mismatch would prevent 304s from ever firing.
            var lastModified = new DateTimeOffset(info.LastWriteTimeUtc).AddTicks(
                -(info.LastWriteTimeUtc.Ticks % TimeSpan.TicksPerSecond));
            // ETag = mtime + size: stable across reads of the same file, changes on rewrite.
            var etag = new EntityTagHeaderValue("\"" + info.LastWriteTimeUtc.Ticks.ToString("x")
                + "-" + info.Length.ToString("x") + "\"");

            var headers = request.GetTypedHeaders();
            bool notModified = false;
            if (headers.IfNoneMatch != null && headers.IfNoneMatch.Count > 0)
            {
                foreach (var tag in headers.IfNoneMatch)
                {
                    if (tag.Tag == etag.Tag) { notModified = true; break; }
                }
            }
            else if (headers.IfModifiedSince.HasValue && headers.IfModifiedSince.Value >= lastModified)
            {
                notModified = true;
            }

            var response = request.HttpContext.Response;
            response.Headers[HeaderNames.LastModified] = lastModified.ToString("R");
            response.Headers[HeaderNames.ETag] = etag.ToString();
            response.Headers[HeaderNames.CacheControl] = "public, max-age=300, must-revalidate";

            if (notModified)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotModified);
            }

            try
            {
                return new FileContentResult(File.ReadAllBytes(path), contentType);
            }
            catch
            {
                return null;
            }
        }
    }
}