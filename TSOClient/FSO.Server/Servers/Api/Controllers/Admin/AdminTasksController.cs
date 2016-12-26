using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Utils;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers.Admin
{
    public class AdminTasksController : NancyModule
    {
        public AdminTasksController(IDAFactory daFactory, JWTFactory jwt, IGluonHostPool hostPool) : base("/admin")
        {
            JWTTokenAuthentication.Enable(this, jwt);

            this.Get["/tasks"] = _ =>
            {
                this.DemandAdmin();

                using (var da = daFactory.Get())
                {
                    var offset = this.Request.Query["offset"];
                    var limit = this.Request.Query["limit"];

                    if (offset == null) { offset = 0; }
                    if (limit == null) { limit = 20; }

                    if (limit > 100)
                    {
                        limit = 100;
                    }

                    var result = da.Tasks.All((int)offset, (int)limit);
                    return Response.AsPagedList<DbTask>(result);
                }
            };

            this.Post["/tasks/request"] = x =>
            {
                var task = this.Bind<TaskRequest>();

                var taskServer = hostPool.GetByRole(Database.DA.Hosts.DbHostRole.task).FirstOrDefault();
                if(taskServer == null)
                {
                    return Response.AsJson(-1);
                }else{
                    try {
                        var id = taskServer.Call(new RequestTask() {
                            TaskType = task.task_type.ToString()
                        }).Result;
                        return Response.AsJson(id);
                    }catch(Exception ex)
                    {
                        return Response.AsJson(-1);
                    }
                }
            };
        }
    }

    public class TaskRequest
    {
        public DbTaskType task_type;
    }
}
