using FSO.Server.Api.Core.Utils;
using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/hosts")]
    [ApiController]
    public class AdminHostsController : ControllerBase
    {
        public IActionResult Get()
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            var hosts = api.HostPool.GetAll();

            return ApiResponse.Json(HttpStatusCode.OK, hosts.Select(x => new {
                role = x.Role,
                call_sign = x.CallSign,
                internal_host = x.InternalHost,
                public_host = x.PublicHost,
                connected = x.Connected,
                time_boot = x.BootTime
            }));
        }
    }
}
