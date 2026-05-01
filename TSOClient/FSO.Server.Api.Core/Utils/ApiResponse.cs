using FSO.Common.Utils;
using FSO.Server.Database.DA.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
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
        // Headers are read/written as raw strings to avoid a hard load-time dependency on
        // Microsoft.Net.Http.Headers, which the deployed netcoreapp2.2 runtime doesn't
        // always ship at the exact patch version the build resolved against.
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
            var etag = "\"" + info.LastWriteTimeUtc.Ticks.ToString("x")
                + "-" + info.Length.ToString("x") + "\"";

            bool notModified = MatchesETag(request.Headers["If-None-Match"], etag)
                || (!request.Headers.ContainsKey("If-None-Match")
                    && MatchesIfModifiedSince(request.Headers["If-Modified-Since"], lastModified));

            var response = request.HttpContext.Response;
            response.Headers["Last-Modified"] = lastModified.ToString("R", CultureInfo.InvariantCulture);
            response.Headers["ETag"] = etag;
            response.Headers["Cache-Control"] = "public, max-age=300, must-revalidate";

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

        // Accepts the literal "*" wildcard or any comma-separated ETag matching ours.
        // Tolerates the optional weak-validator prefix (W/) by stripping it before compare.
        private static bool MatchesETag(Microsoft.Extensions.Primitives.StringValues ifNoneMatch, string etag)
        {
            if (ifNoneMatch.Count == 0) return false;
            foreach (var raw in ifNoneMatch)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                foreach (var part in raw.Split(','))
                {
                    var trimmed = part.Trim();
                    if (trimmed == "*") return true;
                    if (trimmed.StartsWith("W/", StringComparison.Ordinal)) trimmed = trimmed.Substring(2);
                    if (trimmed == etag) return true;
                }
            }
            return false;
        }

        private static bool MatchesIfModifiedSince(Microsoft.Extensions.Primitives.StringValues ifModifiedSince, DateTimeOffset lastModified)
        {
            if (ifModifiedSince.Count == 0) return false;
            if (!DateTimeOffset.TryParse(ifModifiedSince[0], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var since))
                return false;
            return since >= lastModified;
        }
    }
}