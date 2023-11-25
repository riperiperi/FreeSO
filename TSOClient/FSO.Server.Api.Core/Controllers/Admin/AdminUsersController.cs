using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.Inbox;
using FSO.Server.Database.DA.Users;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/users")]
    [ApiController]
    public class AdminUsersController : ControllerBase
    {
        //Get information about me, useful for the admin user interface to disable UI based on who you login as
        public IActionResult current()
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
        [HttpGet("{id}")]
        public IActionResult Get(string id)
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
        /// Unbans a user by IP and user.
        /// </summary>
        /// <param name="user_id">ID of user to unban.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("admin/unban")]
        public IActionResult UnbanUser([FromBody] string user_id)
        {
            Api api = Api.INSTANCE;

            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                User userModel = da.Users.GetById(uint.Parse(user_id));

                if(userModel.is_banned)
                {
                    da.Users.UpdateBanned(uint.Parse(user_id), false);
                }

                var ban = da.Bans.GetByIP(userModel.last_ip);

                if (ban!=null)
                {
                    da.Bans.Remove(userModel.user_id);
                }

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "success"
                });
            }
        }

        /// <summary>
        /// Sends an in-game email message to a player.
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("admin/mail")]
        public IActionResult SendMail(MailCreateModel mail)
        {
            Api api = Api.INSTANCE;

            api.DemandAdmin(Request);

            using (var da = api.DAFactory.Get())
            {
                User recipient = da.Users.GetById(uint.Parse(mail.target_id));

                if (recipient == null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new 
                    {
                        status = "invalid_target_id"
                    });
                }

                if (mail.subject.Trim() == "")
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new 
                    {
                        status = "subject_empty"
                    });
                }

                if (mail.body.Trim() == "")
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new 
                    {
                        status = "body_empty"
                    });
                }

                // Save mail in db
                int message_id = da.Inbox.CreateMessage(new DbInboxMsg
                {
                    sender_id = 2147483648,
                    target_id = uint.Parse(mail.target_id),
                    subject = mail.subject,
                    body = mail.body,
                    sender_name = "FreeSO Staff",
                    time = DateTime.UtcNow,
                    msg_type = 4,
                    msg_subtype = 0,
                    read_state = 0,
                });

                // Try and notify the user ingame
                api.RequestMailNotify(message_id, mail.subject, mail.body, uint.Parse(mail.target_id));

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "success"
                });
            }
        }

        /// <summary>
        /// Kicks a user out the current session.
        /// </summary>
        /// <param name="kick"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("admin/kick")]
        public IActionResult KickUser([FromBody] string user_id)
        {
            Api api = Api.INSTANCE;

            api.DemandModerator(Request);

            api.RequestUserDisconnect(uint.Parse(user_id));

            return ApiResponse.Json(HttpStatusCode.OK, new {
                status = "success"
            });
        }

        /// <summary>
        /// Bans a user and kicks them.
        /// </summary>
        /// <param name="ban"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("admin/ban")]
        public IActionResult BanUser(BanCreateModel ban)
        {
            Api api = Api.INSTANCE;

            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                User userModel = da.Users.GetById(uint.Parse(ban.user_id));

                if (userModel == null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new
                    {
                        status = "invalid_id"
                    });
                }

                if (ban.ban_type == "ip")
                {
                    if (da.Bans.GetByIP(userModel.last_ip) != null)
                    {
                        return ApiResponse.Json(HttpStatusCode.OK, new
                        {
                            status = "already_banned"
                        });
                    }

                    if (userModel.last_ip == "127.0.0.1")
                    {
                        return ApiResponse.Json(HttpStatusCode.OK, new
                        {
                            status = "invalid_ip"
                        });
                    }

                    da.Bans.Add(userModel.last_ip, userModel.user_id, ban.reason, int.Parse(ban.end_date), userModel.client_id);

                    api.RequestUserDisconnect(userModel.user_id);

                    api.SendBanMail(userModel.username, userModel.email, uint.Parse(ban.end_date));

                    return ApiResponse.Json(HttpStatusCode.OK, new
                    {
                        status = "success"
                    });
                }
                else if (ban.ban_type == "user")
                {
                    if (userModel.is_banned)
                    {
                        return ApiResponse.Json(HttpStatusCode.NotFound, new
                        {
                            status = "already_banned"
                        });
                    }

                    da.Users.UpdateBanned(userModel.user_id, true);

                    api.RequestUserDisconnect(userModel.user_id);

                    api.SendBanMail(userModel.username, userModel.email, uint.Parse(ban.end_date));

                    return ApiResponse.Json(HttpStatusCode.OK, new
                    {
                        status = "success"
                    });
                }

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "invalid_ban_type"
                });
            }
        }

        //List users
        [HttpGet]
        public IActionResult Get(int limit, int offset, string order)
        {
            if (limit == 0) limit = 20;
            if (order == null) order = "register_date";
            var api = Api.INSTANCE;
            api.DemandModerator(Request);
            using (var da = api.DAFactory.Get())
            {

                if (limit > 100)
                {
                    limit = 100;
                }

                var result = da.Users.All((int)offset, (int)limit);
                return ApiResponse.PagedList<User>(Request, HttpStatusCode.OK, result);
            }
        }

        //Create a new user
        [HttpPost]
        public IActionResult Post(UserCreateModel user)
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

    public class MailCreateModel
    {
        public string target_id { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public string sender_name { get; set; }
    }
}
