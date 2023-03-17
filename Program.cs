using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Url_Detection_Agent;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Services;


//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Debug()
//    .WriteTo.File("Log/Logs.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

using IHost host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;
// general run of the app
try
{
    services.GetRequiredService<AgentApp>().Run(args);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    throw;
}

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<AgentApp>();
            services.AddSingleton<IProxyServerService, ProxyServerService>();
            services.AddSingleton<IUrlMemoryCache, UrlMemoryCache>();
            services.AddSingleton<IAPIService, APIService>();
            //services.AddLogging(loggingBuilder =>
            //loggingBuilder.AddSerilog(dispose: true));
        });
}