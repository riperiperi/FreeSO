using FSO.Server.Api.Utils;
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

        public void SendBanMail(string username, string email, int end_date)
        {
            ApiMail banMail = new ApiMail("MailBan");

            var date = end_date == 0 ? "Permanent ban" : (new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(end_date).ToLocalTime()).ToString();

            banMail.AddString("username", username);
            banMail.AddString("end", date);

            banMail.Send(email, "Banned from ingame");
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
    }
}