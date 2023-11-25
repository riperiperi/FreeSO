using FSO.Server.Api.Core.Utils;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class NhoodInfoController : ControllerBase
    {
        [HttpGet]
        [Route("userapi/city/{shardId}/neighborhoods/all")]
        public IActionResult GetAll(int shardId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var nhoods = da.Neighborhoods.All(shardId);
                if (nhoods == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONNhoodError("Neighborhoods not found"));

                List<JSONNhood> nhoodJson = new List<JSONNhood>();
                foreach (var nhood in nhoods)
                {
                    nhoodJson.Add(new JSONNhood
                    {
                        neighborhood_id = nhood.neighborhood_id,
                        name = nhood.name,
                        description = nhood.description,
                        color = nhood.color,
                        town_hall_id = nhood.town_hall_id,
                        icon_url = nhood.icon_url,
                        mayor_id = nhood.mayor_id,
                        mayor_elected_date = nhood.mayor_elected_date,
                        election_cycle_id = nhood.election_cycle_id
                    });

                }
                var nhoodsJson = new JSONNhoods();
                nhoodsJson.neighborhoods = nhoodJson;
                return ApiResponse.Json(HttpStatusCode.OK, nhoodsJson);
            }
        }
        [HttpGet]
        [Route("userapi/neighborhoods/{nhoodId}")]
        public IActionResult GetByID(uint nhoodId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var nhood = da.Neighborhoods.Get(nhoodId);
                if (nhood == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONNhoodError("Neighborhood not found"));

                var nhoodJson = new JSONNhood
                {
                    neighborhood_id = nhood.neighborhood_id,
                    name = nhood.name,
                    description = nhood.description,
                    color = nhood.color,
                    town_hall_id = nhood.town_hall_id,
                    icon_url = nhood.icon_url,
                    mayor_id = nhood.mayor_id,
                    mayor_elected_date = nhood.mayor_elected_date,
                    election_cycle_id = nhood.election_cycle_id
                };
                return ApiResponse.Json(HttpStatusCode.OK, nhoodJson);
            }
        }
        [HttpGet]
        [Route("userapi/city/{shardId}/neighborhoods/name/{nhoodName}")]
        public IActionResult GetByName(int shardId, string nhoodName)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var searchNhood = da.Neighborhoods.SearchExact(shardId, nhoodName, 1).FirstOrDefault();
                if (searchNhood == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONNhoodError("Neighborhood not found"));

                var nhood = da.Neighborhoods.Get((uint)searchNhood.neighborhood_id);
                if (nhood == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONNhoodError("Neighborhood not found"));

                var nhoodJson = new JSONNhood
                {
                    neighborhood_id = nhood.neighborhood_id,
                    name = nhood.name,
                    description = nhood.description,
                    color = nhood.color,
                    town_hall_id = nhood.town_hall_id,
                    icon_url = nhood.icon_url,
                    mayor_id = nhood.mayor_id,
                    mayor_elected_date = nhood.mayor_elected_date,
                    election_cycle_id = nhood.election_cycle_id
                };
                return ApiResponse.Json(HttpStatusCode.OK, nhoodJson);
            }
        }
    }
    public class JSONNhoodError
    {
        public string error;
        public JSONNhoodError(string errorString)
        {
            error = errorString;
        }
    }
    public class JSONNhoods
    {
        public List<JSONNhood> neighborhoods { get; set; }
    }
    public class JSONNhood
    {
        public int neighborhood_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public uint color { get; set; }
        public int? town_hall_id { get; set; }
        public string icon_url { get; set; }
        public uint? mayor_id { get; set; }
        public uint mayor_elected_date { get; set; }
        public uint? election_cycle_id { get; set; }

    }
}
