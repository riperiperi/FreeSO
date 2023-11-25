using FSO.Server.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace FSO.Server.Api.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            host.Run();
        }

        public static IAPILifetime RunAsync(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            var lifetime = new APIControl((IApplicationLifetime)host.Services.GetService(typeof(IApplicationLifetime)));
            host.Start();
            return lifetime;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(args[0])
                .ConfigureLogging(x =>
                {
                    x.SetMinimumLevel(LogLevel.None);
                })
                .UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = 500000000;
                })
                .SuppressStatusMessages(true)
                .UseStartup<Startup>();
    }

    public class APIControl : IAPILifetime
    {
        private IApplicationLifetime Lifetime;
        
        public APIControl(IApplicationLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public void Stop()
        {
            Lifetime.StopApplication();
        }
    }
}
