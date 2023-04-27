using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Services;
using Url_Detection_Agent.Utils;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore;
using Serilog;
using Serilog.Extensions.Logging;

namespace Url_Detection_Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;

            // general run of the app

            try
            {

                services.GetRequiredService<AgentApp>().Run(args);
            }
            catch (Exception ex)
            {
                Log.CloseAndFlush();
                throw;
            }
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, app) =>
                {
                    app.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<AgentApp>();
                    services.AddSingleton<IProxyServerService, ProxyServerService>();
                    services.AddSingleton<IUrlMemoryCache, UrlMemoryCache>();
                    services.AddSingleton<IAPIService, APIService>();
                    services.AddSingleton<IHtmlHelperService, HtmlHelperService>();
                    services.AddSingleton<IUserVerification, UserVerification>();
                    services.AddScoped<IAppInstallerHelperService, AppInstallerHelperService>();
                })
                .ConfigureLogging((_,service) =>
                {
                    service.Services.RemoveAll<ILoggerProvider>();

                    Serilog.Debugging.SelfLog.Enable(Console.Error); 

                    Serilog.ILogger logger = new LoggerConfiguration()
                        .Enrich.FromLogContext() 
                        .MinimumLevel.Verbose()
                        .WriteTo.File(@"AppLogs\Logs.txt")
                        .CreateLogger();

                    service.AddProvider(new SerilogLoggerProvider
                             (logger)); 
                });
        }
    }
}


