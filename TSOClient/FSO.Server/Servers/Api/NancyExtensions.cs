using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Security;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Servers.Api
{
    public static class NancyExtensions
    {
        public static void DemandModerator(this NancyModule controller)
        {
            controller.RequiresAuthentication();
            controller.RequiresClaims(new string[] { "moderator" });
        }

        public static void DemandAdmin(this NancyModule controller)
        {
            controller.RequiresAuthentication();
            controller.RequiresClaims(new string[] { "admin" });
        }

        public static Response AsPagedList<T>(this IResponseFormatter formatter, PagedList<T> list)
        {
            return FormatterExtensions.AsJson<PagedList<T>>(formatter, list)
                        .WithHeader("X-Total-Count", list.Total.ToString())
                        .WithHeader("X-Offset", list.Offset.ToString());
        }
    }
}
