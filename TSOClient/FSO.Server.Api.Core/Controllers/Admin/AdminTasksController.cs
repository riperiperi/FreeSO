using FSO.Server.Api.Core.Utils;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Protocol.Gluon.Packets;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/tasks")]
    [ApiController]
    public class AdminTasksController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(int limit, int offset)
            {
            var api = Api.INSTANCE;
                api.DemandAdmin(Request);

                using (var da = api.DAFactory.Get())
                {

                    if (limit > 100)
                    {
                        limit = 100;
                    }

                    var result = da.Tasks.All((int)offset, (int)limit);
                    return ApiResponse.PagedList<DbTask>(Request, HttpStatusCode.OK, result);
                }
            }

        [HttpPost("request")]
        public IActionResult request([FromBody] TaskRequest task)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);

            var taskServer = api.HostPool.GetByRole(Database.DA.Hosts.DbHostRole.task).FirstOrDefault();
            if (taskServer == null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, -1);
            }
            else
            {
                try
                {
                    var id = taskServer.Call(new RequestTask()
                    {
                        TaskType = task.task_type.ToString(),
                        ParameterJson = JsonConvert.SerializeObject(task.parameter),
                        ShardId = (task.shard_id == null || !task.shard_id.HasValue) ? -1 : task.shard_id.Value
                    }).Result;
                    return ApiResponse.Json(HttpStatusCode.OK, id);
                }
                catch (Exception ex)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, -1);
                }
            }
        }
    }
    public class TaskRequest
    {
        public DbTaskType task_type;
        public int? shard_id;
        public dynamic parameter;
    }
}
