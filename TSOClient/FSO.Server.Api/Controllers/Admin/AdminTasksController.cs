using FSO.Server.Api.Utils;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Protocol.Gluon.Packets;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Web.Http;

namespace FSO.Server.Api.Controllers.Admin
{
    public class AdminTasksController : ApiController
    {

        public HttpResponseMessage Get(int limit, int offset)
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
                    return ApiResponse.PagedList<DbTask>(HttpStatusCode.OK, result);
                }
            }

        [HttpPost]
        public HttpResponseMessage request([FromBody] TaskRequest task)
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
