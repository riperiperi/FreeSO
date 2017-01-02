using FSO.Server.Database.DA;
using FSO.Server.Servers.Api.JsonWebToken;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers.Admin
{
    public class AdminShardsController : NancyModule
    {
        public AdminShardsController(IDAFactory daFactory, JWTFactory jwt) : base("/admin")
        {
            JWTTokenAuthentication.Enable(this, jwt);

            this.Get["/shards"] = _ =>
            {
                this.DemandAdmin();

                using (var db = daFactory.Get())
                {
                    var shards = db.Shards.All();
                    return Response.AsJson(shards);
                }
            };
        }
    }
}
