using FSO.Server;
using FSO.Server.Api.Core.Services;
using FSO.Server.Common;
using FSO.Server.Servers.UserApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;

namespace FSO.Server.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            Content.Model.AbstractTextureRef.ImageFetchFunction = CoreImageLoader.SoftImageFetch;
            UserApi.CustomStartup = StartWebApi;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1252 = Encoding.GetEncoding(1252);

            FSO.Server.Program.Main(args);
        }

        public static IAPILifetime StartWebApi(UserApi api, string url)
        {
            var config = api.Config;
            var userApiConfig = config.Services.UserApi;
            var settings = new NameValueCollection();
            settings.Add("maintainance", userApiConfig.Maintainance.ToString());
            settings.Add("authTicketDuration", userApiConfig.AuthTicketDuration.ToString());
            settings.Add("regkey", userApiConfig.Regkey);
            settings.Add("secret", config.Secret);
            settings.Add("updateUrl", userApiConfig.UpdateUrl);
            settings.Add("cdnUrl", userApiConfig.CDNUrl);
            settings.Add("connectionString", config.Database.ConnectionString);
            settings.Add("NFSdir", config.SimNFS);
            settings.Add("smtpHost", userApiConfig.SmtpHost);
            settings.Add("smtpUser", userApiConfig.SmtpUser);
            settings.Add("smtpPassword", userApiConfig.SmtpPassword);
            settings.Add("smtpPort", userApiConfig.SmtpPort.ToString());
            settings.Add("useProxy", userApiConfig.UseProxy.ToString());
            settings.Add("updateID", config.UpdateID?.ToString() ?? "");
            settings.Add("branchName", config.UpdateBranch);

            var api2 = new FSO.Server.Api.Core.Api();
            api2.Init(settings);
            if (userApiConfig.AwsConfig != null) api2.UpdateUploader = new AWSUpdateUploader(userApiConfig.AwsConfig);
            api.SetupInstance(api2);
            api2.HostPool = api.GetGluonHostPool();
            
            return FSO.Server.Api.Core.Program.RunAsync(new string[] { url });
        }
    }
}
