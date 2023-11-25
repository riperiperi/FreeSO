using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FSO.Server.Api.Core.Models;
using FSO.Server.Api.Core.Services;
using FSO.Server.Api.Core.Utils;
using FSO.Server.Database.DA.Updates;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/updates")]
    public class AdminUpdatesController : ControllerBase
    {
        //List updates
        [HttpGet]
        public IActionResult Get(int limit, int offset, string order)
        {
            if (limit == 0) limit = 20;
            if (order == null) order = "date";
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {

                if (limit > 100)
                {
                    limit = 100;
                }

                var result = da.Updates.All((int)offset, (int)limit);
                return ApiResponse.PagedList<DbUpdate>(Request, HttpStatusCode.OK, result);
            }
        }


        // GET all branches
        [HttpGet("branches")]
        public IActionResult GetBranches()
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            using (var da = api.DAFactory.Get())
            {
                return new JsonResult(da.Updates.GetBranches().ToList());
            }
        }

        // GET all addons
        [HttpGet("addons")]
        public IActionResult GetAddons()
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            using (var da = api.DAFactory.Get())
            {
                return new JsonResult(da.Updates.GetAddons(20).ToList());
            }
        }

        // POST create a branch.
        [HttpPost("branches")]
        public IActionResult AddBranch(DbUpdateBranch branch)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            using (var da = api.DAFactory.Get())
            {
                if (da.Updates.AddBranch(branch)) return Ok();
                else return NotFound();
            }
        }

        // POST update a branch.
        [HttpPost("branches/{id}")]
        public IActionResult UpdateBranch(DbUpdateBranch branch)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            using (var da = api.DAFactory.Get())
            {
                if (da.Updates.UpdateBranchInfo(branch)) return Ok();
                else return NotFound();
            }
        }

        public class AddonUploadModel
        {
            public string name { get; set; }
            public string description { get; set; }
            public IFormFile clientAddon { get; set; }
            public IFormFile serverAddon { get; set; }
        }

        static int AddonRequestID = 0;
        [HttpPost("uploadaddon")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 500000000)]
        public async Task<IActionResult> UploadAddon(AddonUploadModel upload)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            var reqID = ++AddonRequestID;

            var info = new DbUpdateAddon();
            if (upload.name == null || upload.name.Length > 128) return BadRequest("Invalid name.");
            if (upload.description == null || upload.description.Length > 1024) return BadRequest("Invalid description.");
            info.name = upload.name;
            info.description = upload.description;
            info.date = DateTime.UtcNow;

            if (upload.clientAddon == null && upload.serverAddon == null)
                return BadRequest("client or server addon binary must be uploaded.");

            var addonID = DateTime.UtcNow.Ticks;
            Directory.CreateDirectory("updateTemp/addons/");
            if (upload.clientAddon != null)
            {
                using (var file = System.IO.File.Open($"updateTemp/addons/client{reqID}.zip", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await upload.clientAddon.CopyToAsync(file);
                }
                info.addon_zip_url = await api.UpdateUploader.UploadFile($"addons/client{addonID}.zip", $"updateTemp/addons/client{reqID}.zip", $"addon-{addonID}");
                System.IO.File.Delete($"updateTemp/addons/client{reqID}.zip");
            }

            if (upload.serverAddon != null)
            {
                using (var file = System.IO.File.Open($"updateTemp/addons/server{reqID}.zip", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await upload.serverAddon.CopyToAsync(file);
                }
                info.server_zip_url = await api.UpdateUploader.UploadFile($"addons/server{addonID}.zip", $"updateTemp/addons/server{reqID}.zip", $"addon-{addonID}");
                System.IO.File.Delete($"updateTemp/addons/server{reqID}.zip");
            }

            using (var da = api.DAFactory.Get())
            {
                da.Updates.AddAddon(info);
                return new JsonResult(info);
            }
        }

        // GET status for ongoing update generation
        [HttpGet("updateTask/{id}")]
        public IActionResult GetTask(int id)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            var task = GenerateUpdateService.INSTANCE.GetTask(id);
            if (task == null) return NotFound();
            else return new JsonResult(task);
        }

        // POST admin/updates (start update generation)
        [HttpPost]
        public IActionResult Post([FromBody]UpdateCreateModel request)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);
            var task = GenerateUpdateService.INSTANCE.CreateTask(request);
            return new JsonResult(task);
        }
    }
}
