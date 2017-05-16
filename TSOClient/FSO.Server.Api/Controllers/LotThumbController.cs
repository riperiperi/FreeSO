using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace FSO.Server.Api.Controllers
{
    public class LotThumbController : ApiController
    {

        public HttpResponseMessage Get(int shardid, uint id)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                FileStream stream;
                try
                {
                    stream = File.OpenRead(Path.Combine(api.Config.NFSdir, "Lots/" + lot.lot_id.ToString("x8") + "/thumb.png"));
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(stream);
                    response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(0, 3, 0),
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                    return response;
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
        }
    }
}
