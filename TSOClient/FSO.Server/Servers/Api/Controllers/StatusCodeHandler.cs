using Nancy;
using Nancy.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers
{
    public class StatusCodeHandler : IStatusCodeHandler
    {
        private readonly IRootPathProvider _rootPathProvider;

        public StatusCodeHandler(IRootPathProvider rootPathProvider)
        {
            _rootPathProvider = rootPathProvider;
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.NotFound;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            context.Response.Contents = stream =>
            {
                 
            };
        }
    }
}
