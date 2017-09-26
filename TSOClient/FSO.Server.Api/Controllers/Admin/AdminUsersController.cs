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
}
