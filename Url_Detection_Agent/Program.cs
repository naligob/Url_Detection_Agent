using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Url_Detection_Agent;
using Url_Detection_Agent.Interfaces;
using Url_Detection_Agent.Services;


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
        .ConfigureAppConfiguration((context,app) =>
        {
            app.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json");
        })
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<AgentApp>();
            services.AddSingleton<IProxyServerService, ProxyServerService>();
            services.AddSingleton<IUrlMemoryCache, UrlMemoryCache>();
            services.AddSingleton<IAPIService, APIService>();
            services.AddSingleton<IHtmlHelperService, HtmlHelperService>();
        });
}