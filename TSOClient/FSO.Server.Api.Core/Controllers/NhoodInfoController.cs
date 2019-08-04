using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FSO.Server.Database.DA.Avatars;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class NhoodInfoController : ControllerBase
    {
        [HttpGet]
        [Route("userapi/city/{shardid}/neighborhoods/all.json")]
        public IActionResult GetAll(int shardid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Nhoods = da.Neighborhoods.All(shardid);
                if (Nhoods == null)
                {
                    var JSONError = new JSONNhoodError();
                    JSONError.Error = "Neighborhoods not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONNhood> NhoodJSON = new List<JSONNhood>();
                foreach (var Nhood in Nhoods)
                {
                    NhoodJSON.Add(new JSONNhood
                    {
                        Neighborhood_ID = Nhood.neighborhood_id,
                        Name = Nhood.name,
                        Description = Nhood.description,
                        Color = Nhood.color,
                        Town_Hall_ID = Nhood.town_hall_id,
                        Icon_Url = Nhood.icon_url,
                        Mayor_ID = Nhood.mayor_id,
                        Mayor_Elected_Date = Nhood.mayor_elected_date,
                        Election_Cycle_ID = Nhood.election_cycle_id
                    });

                }
                var NhoodsJSON = new JSONNhoods();
                NhoodsJSON.Neighborhoods = NhoodJSON;
                return ApiResponse.Json(HttpStatusCode.OK, NhoodsJSON);
            }
        }
        [HttpGet]
        [Route("userapi/neighborhoods/id/{Nhoodid}.json")]
        public IActionResult GetByID(uint Nhoodid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Nhood = da.Neighborhoods.Get(Nhoodid);
                if (Nhood == null)
                {
                    var JSONError = new JSONNhoodError();
                    JSONError.Error = "Neighborhood not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var NhoodJSON = new JSONNhood
                {
                    Neighborhood_ID = Nhood.neighborhood_id,
                    Name = Nhood.name,
                    Description = Nhood.description,
                    Color = Nhood.color,
                    Town_Hall_ID = Nhood.town_hall_id,
                    Icon_Url = Nhood.icon_url,
                    Mayor_ID = Nhood.mayor_id,
                    Mayor_Elected_Date = Nhood.mayor_elected_date,
                    Election_Cycle_ID = Nhood.election_cycle_id
                };
                return ApiResponse.Json(HttpStatusCode.OK, NhoodJSON);
            }
        }
        [HttpGet]
        [Route("userapi/city/{shardid}/neighborhoods/name/{NhoodName}.json")]
        public IActionResult GetByName(int shardid, string NhoodName)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var SearchNhood = da.Neighborhoods.SearchExact(shardid, NhoodName, 1).FirstOrDefault();
                if (SearchNhood == null)
                {
                    var JSONError = new JSONNhoodError();
                    JSONError.Error = "Neighborhood not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var Nhood = da.Neighborhoods.Get((uint)SearchNhood.neighborhood_id);
                if (Nhood == null)
                {
                    var JSONError = new JSONNhoodError();
                    JSONError.Error = "Neighborhood not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var NhoodJSON = new JSONNhood
                {
                    Neighborhood_ID = Nhood.neighborhood_id,
                    Name = Nhood.name,
                    Description = Nhood.description,
                    Color = Nhood.color,
                    Town_Hall_ID = Nhood.town_hall_id,
                    Icon_Url = Nhood.icon_url,
                    Mayor_ID = Nhood.mayor_id,
                    Mayor_Elected_Date = Nhood.mayor_elected_date,
                    Election_Cycle_ID = Nhood.election_cycle_id
                };
                return ApiResponse.Json(HttpStatusCode.OK, NhoodJSON);
            }
        }
    }
    public class JSONNhoodError
    {
        public string Error { get; set; }
    }
    public class JSONNhoods
    {
        public List<JSONNhood> Neighborhoods { get; set; }
    }
    public class JSONNhood
    {
        public int Neighborhood_ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public uint Color { get; set; }
        public int? Town_Hall_ID { get; set; }
        public string Icon_Url { get; set; }
        public uint? Mayor_ID { get; set; }
        public uint Mayor_Elected_Date { get; set; }
        public uint? Election_Cycle_ID { get; set; }

    }
}
