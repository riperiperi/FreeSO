using FSO.Server.Api;
using FSO.Server.Api.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.Users;
using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace FSO.Server.Api.Controllers.Admin
{
    public class AdminUsersController : ApiController
    {
        private const string BAN_TYPE_IP = "ip";
        private const string BAN_TYPE_USER = "user";

        //Get information about me, useful for the admin user interface to disable UI based on who you login as
        public HttpResponseMessage current()
        {
            var api = Api.INSTANCE;

            var user = api.RequireAuthentication(Request);

            using (var da = api.DAFactory.Get())
            {
                var userModel = da.Users.GetById(user.UserID);
                if (userModel == null)
                {
                    throw new Exception("Unable to find user");
                }
                return ApiResponse.Json(HttpStatusCode.OK, userModel);
            }
        }


        //Get the attributes of a specific user
        public HttpResponseMessage Get(string id)
        {
            if (id == "current") return current();
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {
                var userModel = da.Users.GetById(uint.Parse(id));
                if (userModel == null) { throw new Exception("Unable to find user"); }
                return ApiResponse.Json(HttpStatusCode.OK, userModel);
            }
        }

        /// <summary>
        /// Allows banning users outside of the game.
        /// </summary>
        /// <param name="id">ID of the user to ban.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("admin/ban")]
        public HttpResponseMessage BanUser(BanCreateModel ban)
        {
            Api api = Api.INSTANCE;

            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                User userModel = da.Users.GetById(uint.Parse(ban.user_id));

                if (userModel == null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                    {
                        status = "invalid_id"
                    });
                }

                if (ban.ban_type == BAN_TYPE_IP)
                {
                    if (da.Bans.GetByIP(userModel.last_ip) != null)
                    {
                        return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                        {
                            status = "already_banned"
                        });
                    }

                    if (userModel.last_ip == "127.0.0.1")
                    {
                        return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                        {
                            status = "invalid_ip"
                        });
                    }

                    da.Bans.Add(userModel.last_ip, userModel.user_id, ban.reason, int.Parse(ban.end_date), userModel.client_id);

                    api.RequestUserDisconnect(userModel.user_id);

                    return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                    {
                        status = "success"
                    });
                }
                else if (ban.ban_type == BAN_TYPE_USER)
                {
                    if (userModel.is_banned)
                    {
                        return ApiResponse.Json(HttpStatusCode.NotFound, new AdminRequestResponse()
                        {
                            status = "already_banned"
                        });
                    }

                    da.Users.UpdateBanned(userModel.user_id, true);

                    api.RequestUserDisconnect(userModel.user_id);

                    return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                    {
                        status = "success"
                    });
                }

                return ApiResponse.Json(HttpStatusCode.OK, new AdminRequestResponse()
                {
                    status = "invalid_ban_type"
                });
            }
        }

        public HttpResponseMessage Get() { return Get(20, 0, "register_date"); }
        public HttpResponseMessage Get(int offset) { return Get(20, offset, "register_date"); }

        //List users
        public HttpResponseMessage Get(int limit, int offset, string order)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {

                if (limit > 100)
                {
                    limit = 100;
                }

                var result = da.Users.All((int)offset, (int)limit);
                return ApiResponse.PagedList<User>(HttpStatusCode.OK, result);
            }
        }

        //Create a new user
        public HttpResponseMessage Post(UserCreateModel user)
        {
            var api = Api.INSTANCE;
            var nuser = api.RequireAuthentication(Request);
            api.DemandModerator(nuser);

            if (user.is_admin)
            {
                //I need admin claim to do this
                api.DemandAdmin(nuser);
            }

            using (var da = api.DAFactory.Get())
            {
                var userModel = new User();
                userModel.username = user.username;
                userModel.email = user.email;
                userModel.is_admin = user.is_admin;
                userModel.is_moderator = user.is_moderator;
                userModel.user_state = UserState.valid;
                userModel.register_date = Epoch.Now;
                userModel.is_banned = false;

                var userId = da.Users.Create(userModel);

                userModel = da.Users.GetById(userId);
                if (userModel == null) { throw new Exception("Unable to find user"); }
                return ApiResponse.Json(HttpStatusCode.OK, userModel);
            }
        }
    }

    public class UserCreateModel
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public bool is_admin { get; set; }
        public bool is_moderator { get; set; }
    }

    public class BanCreateModel
    {
        public string ban_type { get; set; }
        public string user_id { get; set; }
        public string reason { get; set; }
        public string end_date { get; set; }
    }

    public class AdminRequestResponse
    {
        //public string error_description { get; set; }
        public string status { get; set; }
    }
}
