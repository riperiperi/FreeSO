using FSO.Server.Api.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Domain;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Servers.Api.JsonWebToken;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace FSO.Server.Api
{
    public class Api
    {
        public static Api INSTANCE;

        public IDAFactory DAFactory;
        public ApiConfig Config;
        public JWTFactory JWT;
        public Shards Shards;
        public IGluonHostPool HostPool;

        public event APIRequestShutdownDelegate OnRequestShutdown;
        public event APIBroadcastMessageDelegate OnBroadcastMessage;
        public event APIRequestUserDisconnectDelegate OnRequestUserDisconnect;
        public event APIRequestMailNotifyDelegate OnRequestMailNotify;

        public delegate void APIRequestShutdownDelegate(uint time, ShutdownType type);
        public delegate void APIBroadcastMessageDelegate(string sender, string title, string message);
        public delegate void APIRequestUserDisconnectDelegate(uint user_id);
        public delegate void APIRequestMailNotifyDelegate(int message_id, string subject, string body, uint target_id);

        public Api()
        {
            INSTANCE = this;
        }

        public void Init(NameValueCollection appSettings)
        {
            Config = new ApiConfig();
            Config.Maintainance = bool.Parse(appSettings["maintainance"]);
            Config.AuthTicketDuration = int.Parse(appSettings["authTicketDuration"]);
            Config.Regkey = appSettings["regkey"];
            Config.Secret = appSettings["secret"];
            Config.UpdateUrl = appSettings["updateUrl"];
            Config.NFSdir = appSettings["nfsdir"];

            if(appSettings["mailerHost"]!=null&&
                appSettings["mailerUser"]!=null&&
                appSettings["mailerPassword"]!=null&&
                appSettings["mailerPort"]!=null)
            {
                Config.MailerEnabled = true;
                Config.MailerHost = appSettings["mailerHost"];
                Config.MailerUser = appSettings["mailerUser"];
                Config.MailerPassword = appSettings["mailerPassword"];
                Config.MailerPort = int.Parse(appSettings["mailerPort"]);
            }

            Config.EmailConfirmation = appSettings["emailConfirmation"]!=null&& appSettings["emailConfirmation"] == "true";

            JWT = new JWTFactory(new JWTConfiguration()
            {
                Key = System.Text.UTF8Encoding.UTF8.GetBytes(Config.Secret)
            });

            DAFactory = new MySqlDAFactory(new Database.DatabaseConfiguration()
            {
                ConnectionString = appSettings["connectionString"]
            });


            Shards = new Shards(DAFactory);
            Shards.AutoUpdate();
        }

        public JWTUser RequireAuthentication(HttpRequestMessage request)
        {
            /*var http = HttpContext.Current;
            if (http == null)
            {
                throw new SecurityException("Unable to get http context");
            }*/
            JWTUser result;
            if (request.Headers.Authorization != null)
            {
                result = JWT.DecodeToken(request.Headers.Authorization.Parameter);
            }
            else
            {
                var cookies = request.Headers.GetCookies().FirstOrDefault();
                if (cookies == null)
                    throw new SecurityException("Unable to find cookie");


                var cookie = cookies["fso"];
                if (cookie == null)
                {
                    throw new SecurityException("Unable to find cookie");
                }
                result = JWT.DecodeToken(cookie.Value);
            }
            if (result == null)
            {
                throw new SecurityException("Invalid token");
            }

            return result;
        }

        public void SendBanMail(string username, string email, uint end_date)
        {
            ApiMail banMail = new ApiMail("MailBan");

            var date = end_date == 0 ? "Permanent ban" : Epoch.ToDate(end_date).ToString();

            banMail.AddString("username", username);
            banMail.AddString("end", date);

            banMail.Send(email, "Banned from ingame");
        }

        public void SendEmailConfirmationOKMail(string username, string email)
        {
            ApiMail confirmOKMail = new ApiMail("MailRegistrationOK");

            confirmOKMail.AddString("username", username);

            confirmOKMail.Send(email, "Welcome to FreeSO, " + username + "!");
        }

        public bool SendEmailConfirmationMail(string email, string token, string confirmation_url, uint expires)
        {
            ApiMail confirmMail = new ApiMail("MailRegistrationToken");

            confirmation_url = confirmation_url.Replace("%token%", token);
            confirmMail.AddString("token", token);
            confirmMail.AddString("expires", Epoch.HMSRemaining(expires));
            confirmMail.AddString("confirmation_url", confirmation_url);

            return confirmMail.Send(email, "Verify your FreeSO account");
        }

        public void DemandModerator(JWTUser user)
        {
            if (!user.Claims.Contains("moderator")) throw new Exception("Requires Moderator level status");
        }

        public void DemandAdmin(JWTUser user)
        {
            if (!user.Claims.Contains("admin")) throw new Exception("Requires Admin level status");
        }

        public void DemandModerator(HttpRequestMessage request)
        {
            DemandModerator(RequireAuthentication(request));
        }

        public void DemandAdmin(HttpRequestMessage request)
        {
            DemandAdmin(RequireAuthentication(request));
        }

        public void RequestShutdown(uint time, ShutdownType type)
        {
            OnRequestShutdown?.Invoke(time, type);
        }

        public void RequestUserDisconnect(uint user_id)
        {
            OnRequestUserDisconnect?.Invoke(user_id);
        }

        public void RequestMailNotify(int message_id, string subject, string body, uint target_id)
        {
            OnRequestMailNotify(message_id, subject, body, target_id);
        }

        public void BroadcastMessage(string sender, string title, string message)
        {
            OnBroadcastMessage?.Invoke(sender, title, message);
        }

        public Database.DA.Users.User CreateUser(string username, string email, string password, string ip)
        {
            using (var da = DAFactory.Get())
            {
                var userModel = new Database.DA.Users.User();
                userModel.username = username;
                userModel.email = email;
                userModel.is_admin = false;
                userModel.is_moderator = false;
                userModel.user_state = Database.DA.Users.UserState.valid;
                userModel.register_date = Epoch.Now;
                userModel.is_banned = false;
                userModel.register_ip = ip;
                userModel.last_ip = ip;

                var passhash = PasswordHasher.Hash(password);
                var authSettings = new Database.DA.Users.UserAuthenticate();
                authSettings.scheme_class = passhash.scheme;
                authSettings.data = passhash.data;

                try
                {
                    var userId = da.Users.Create(userModel);
                    authSettings.user_id = userId;
                    da.Users.CreateAuth(authSettings);

                    userModel = da.Users.GetById(userId);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return userModel;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}