using System;
using System.Linq;
using System.Net;
using FSO.Server.Api.Core.Models;
using FSO.Server.Api.Core.Utils;
using FSO.Server.Database.DA.DbEvents;
using FSO.Server.Database.DA.Tuning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/events")]
    [ApiController]
    public class AdminEventsController : ControllerBase
    {
        //List events
        [HttpGet]
        public IActionResult Get(int limit, int offset, string order)
        {
            if (limit == 0) limit = 20;
            if (order == null) order = "start_day";
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {

                if (limit > 100)
                {
                    limit = 100;
                }

                var result = da.Events.All((int)offset, (int)limit, order);
                return ApiResponse.PagedList<DbEvent>(Request, HttpStatusCode.OK, result);
            }
        }

        [HttpGet("presets")]
        public IActionResult GetPresets()
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {
                return new JsonResult(da.Tuning.GetAllPresets().ToList());
            }
        }

        [HttpPost("presets")]
        public IActionResult CreatePreset([FromBody]PresetCreateModel request)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {
                //make the preset first
                var preset_id = da.Tuning.CreatePreset(
                    new DbTuningPreset()
                    {
                        name = request.name,
                        description = request.description,
                        flags = request.flags
                    });

                foreach (var item in request.items)
                {
                    da.Tuning.CreatePresetItem(new DbTuningPresetItem()
                    {
                        preset_id = preset_id,
                        tuning_type = item.tuning_type,
                        tuning_table = item.tuning_table,
                        tuning_index = item.tuning_index,
                        value = item.value
                    });
                }
                return new JsonResult(da.Tuning.GetAllPresets().ToList());
            }
        }

        [HttpGet("presets/{preset_id}")]
        public IActionResult GetPresetEntries(int preset_id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {
                return new JsonResult(da.Tuning.GetPresetItems(preset_id).ToList());
            }
        }

        [HttpDelete("presets/{preset_id}")]
        public IActionResult DeletePreset(int preset_id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {
                return da.Tuning.DeletePreset(preset_id) ? (IActionResult)Ok() : NotFound();
            }
        }

        // POST admin/updates (start update generation)
        [HttpPost]
        public IActionResult Post([FromBody]EventCreateModel request)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                DbEventType type;
                try
                {
                    type = Enum.Parse<DbEventType>(request.type);
                }
                catch
                {
                    return BadRequest("Event type must be one of:" + string.Join(", ", Enum.GetNames(typeof(DbEventType))));
                }
                var model = new DbEvent()
                {
                    title = request.title,
                    description = request.description,
                    start_day = request.start_day,
                    end_day = request.end_day,
                    type = type,
                    value = request.value,
                    value2 = request.value2,
                    mail_subject = request.mail_subject,
                    mail_message = request.mail_message,
                    mail_sender = request.mail_sender,
                    mail_sender_name = request.mail_sender_name
                };
                return new JsonResult(new { id = da.Events.Add(model) });
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public IActionResult Delete(int id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                if (!da.Events.Delete(id)) return NotFound();
            }

            return Ok();
        }

    }
}
