using FSO.Server.Database.DA;
using Nancy;
using Nancy.Authentication.Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Token;
using Nancy.Security;
using FSO.Server.Common;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Database.DA.Users;
using FSO.Server.Database.DA.Utils;
using Nancy.ModelBinding;

namespace FSO.Server.Servers.Api.Controllers
{
    /// <summary>
    /// Provides administration APIs for server setup
    /// </summary>
    public class AdminUsersController : NancyModule
    {
        private IDAFactory DAFactory;

        public AdminUsersController(IDAFactory daFactory, JWTFactory jwt) : base("/admin")
        {
            JWTTokenAuthentication.Enable(this, jwt);

            this.DAFactory = daFactory;

            this.After.AddItemToEndOfPipeline(x =>
            {
                x.Response.WithHeader("Access-Control-Allow-Origin", "*");
            });

            //Get information about me, useful for the admin user interface to disable UI based on who you login as
            this.Get["/users/current"] = _ =>
            {
                this.RequiresAuthentication();
                JWTUserIdentity user = (JWTUserIdentity)this.Context.CurrentUser;

                using (var da = daFactory.Get())
                {
                    var userModel = da.Users.GetById(user.UserID);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return Response.AsJson<User>(userModel);
                }
            };

            //Get the attributes of a specific user
            this.Get["/users/{id}"] = parameters =>
            {
                this.DemandModerator();

                using (var da = daFactory.Get())
                {
                    var userModel = da.Users.GetById((uint)parameters.id);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return Response.AsJson<User>(userModel);
                }
            };

            //List users
            this.Get["/users"] = _ =>
            {
                this.DemandModerator();
                using (var da = daFactory.Get())
                {
                    var offset = this.Request.Query["offset"];
                    var limit = this.Request.Query["limit"];

                    if(offset == null) { offset = 0; }
                    if(limit == null) { limit = 20; }

                    if(limit > 100){
                        limit = 100;
                    }

                    var result = da.Users.All((int)offset, (int)limit);
                    return Response.AsPagedList<User>(result);
                }
            };

            //Create a new user
            this.Post["/users"] = x =>
            {
                this.DemandModerator();
                var user = this.Bind<UserCreateModel>();

                if (user.is_admin){
                    //I need admin claim to do this
                    this.DemandAdmin();
                }

                using (var da = daFactory.Get())
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
                    return Response.AsJson<User>(userModel);
                }

                return null;
            };
        }
    }




    class UserCreateModel
    {
        public string username;
        public string email;
        public string password;
        public bool is_admin;
        public bool is_moderator;
    }
    

}
