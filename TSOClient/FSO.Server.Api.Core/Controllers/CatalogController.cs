using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        [HttpGet]
        [Route("userapi/catalog")]
        public IActionResult GetCatalog()
        {
            var api = Api.INSTANCE;
            if (api.GetCatalogItems == null)
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, "Catalog not available");
            return new JsonResult(api.GetCatalogItems());
        }

        [HttpGet]
        [Route("userapi/catalog/{guid}/thumbnail.png")]
        public IActionResult GetObjectThumbnail(string guid)
        {
            var api = Api.INSTANCE;
            if (!uint.TryParse(guid, System.Globalization.NumberStyles.HexNumber, null, out var guidInt))
                return NotFound();

            var path = Path.Combine(api.Config.NFSdir, "Objects/" + guidInt.ToString("x8") + "/thumb.png");
            if (!System.IO.File.Exists(path) && api.ObjectThumbnailGenerator != null)
                api.ObjectThumbnailGenerator(guidInt, api.Config.NFSdir);

            try { return File(System.IO.File.ReadAllBytes(path), "image/png"); }
            catch { return NotFound(); }
        }
    }

    public class JSONCatalogItem
    {
        public string guid { get; set; }
        public string name { get; set; }
        public int category { get; set; }
        public string category_name { get; set; }
        public int price { get; set; }
        public string tags { get; set; }
        public int disable_level { get; set; }
        public string thumbnail_url { get; set; }
    }
}