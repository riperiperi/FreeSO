using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FSO.Server.Api.Core.Controllers.GameAPI
{
    [EnableCors]
    [Route("userapi/update")]
    public class UpdateController : ControllerBase
    {

        // GET userapi/update
        // get recent PUBLISHED updates for the active branch, ordered by publish date
        [HttpGet()]
        public IActionResult Get(int id)
        {
            var api = Api.INSTANCE;
            using (var da = api.DAFactory.Get())
            {
                var recents = da.Updates.GetRecentUpdatesForBranchByName(api.Config.BranchName, 20);
                return new JsonResult(recents.ToList());
            }
        }

        // GET: userapi/update/<branch>
        // get recent PUBLISHED updates for a specific branch, ordered by publish date
        [HttpGet("{branch}")]
        public IActionResult Get(string branch)
        {
            var api = Api.INSTANCE;
            using (var da = api.DAFactory.Get())
            {
                var recents = da.Updates.GetRecentUpdatesForBranchByName(branch, 20);
                return new JsonResult(recents.ToList());
            }
        }
    }
}
