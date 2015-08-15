using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mina.Core.Service;
using Mina.Core.Session;
using System.Net;
using NLog;
using System.IO;

namespace FSO.Server.Framework
{
    public delegate void HttpHandler(HttpListenerRequest request, HttpListenerResponse response);
    public delegate void HttpPostHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> formData);

    public class HttpPostHandlerProxy
    {
        private HttpPostHandler Proxy;

        public HttpPostHandlerProxy(HttpPostHandler proxy)
        {
            this.Proxy = proxy;
        }

        public void Handle(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (request.ContentType)
            {
                case "application/x-www-form-urlencoded":
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        var text = reader.ReadToEnd();

                    }
                    break;

                default:
                    throw new Exception("Unknown content type");
            }
        }
    }

    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public class HttpRoute
    {
        public HttpVerb Verb;
        public string Path;
        public HttpHandler Handler;

        public bool Matches(HttpListenerRequest request)
        {
            switch (Verb)
            {
                case HttpVerb.GET:
                    if (request.HttpMethod != "GET") { return false; }
                    break;
                case HttpVerb.POST:
                    if (request.HttpMethod != "POST") { return false; }
                    break;
                case HttpVerb.DELETE:
                    if (request.HttpMethod != "DELETE") { return false; }
                    break;
                case HttpVerb.PUT:
                    if (request.HttpMethod != "PUT") { return false; }
                    break;
            }

            if (request.Url.LocalPath == this.Path){
                return true;
            }
            return false;
        }
    }

    public static class HttpExtensions
    {
        public static void Send(this HttpListenerResponse response, string body)
        {
            try {
                var bytes = System.Text.Encoding.ASCII.GetBytes(body);
                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.Close();
            }catch(Exception ex)
            {
                //May already be closed
            }
        }
    }

    public class HttpRouter
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private List<HttpRoute> Routes = new List<HttpRoute>();

        public HttpRouter()
        {
        }

        public void Handle(HttpListenerContext context)
        {
            foreach (var route in Routes){
                if (route.Matches(context.Request)){
                    try {
                        route.Handler(context.Request, context.Response);
                    }catch(Exception ex)
                    {
                        LOG.Error("Unknown error in http route handler", ex);
                        context.Response.StatusCode = 500;
                        context.Response.Send("Internal Error");
                    }
                    return;
                }
            }

            context.Response.StatusCode = 404;
            context.Response.Send("Not found");
        }

        public void Get(string path, HttpHandler handler)
        {
            this.Routes.Add(new HttpRoute {
                Verb = HttpVerb.GET,
                Path = path,
                Handler = handler
            });
        }

        public void PostForm(string path, HttpPostHandler handler)
        {
            this.Routes.Add(new HttpRoute
            {
                Verb = HttpVerb.POST,
                Path = path,
                Handler = new HttpPostHandlerProxy(handler).Handle
            });
        }

        public void Delete(string path, HttpHandler handler)
        {
            this.Routes.Add(new HttpRoute
            {
                Verb = HttpVerb.DELETE,
                Path = path,
                Handler = handler
            });
        }        
    }
}
